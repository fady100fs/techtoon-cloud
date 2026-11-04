# sync_manager.py
# -*- coding: utf-8 -*-
"""
مدير المزامنة بين Neon (المصدر) و SQLite (النسخة المحلية).
- مزامنة أولية عند التشغيل
- مزامنة دورية في الخلفية (كل 15-30 دقيقة)
- مزامنة يدوية عند الطلب
"""
import os
import time
import threading
import logging
from datetime import datetime

from sqlalchemy import inspect as sa_inspect
from sqlalchemy.orm import Session

logger = logging.getLogger(__name__)


# ⭐ تسجيل نتائج/أخطاء المزامنة في ملف مرئي بدل ابتلاعها صامتاً
def _ensure_file_logging():
    try:
        if getattr(logger, "_tt_file_handler", False):
            return
        _dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "data")
        os.makedirs(_dir, exist_ok=True)
        _fh = logging.FileHandler(os.path.join(_dir, "sync.log"), encoding="utf-8")
        _fh.setFormatter(logging.Formatter("%(asctime)s | %(levelname)s | %(message)s"))
        logger.addHandler(_fh)
        if logger.level == logging.NOTSET:
            logger.setLevel(logging.INFO)
        logger._tt_file_handler = True
    except Exception:
        pass

_ensure_file_logging()

# ═══════════════════════════════════════════════════
#  إعدادات المزامنة
# ═══════════════════════════════════════════════════
# يمكن التحكم بها من متغير البيئة SYNC_INTERVAL_SECONDS
SYNC_INTERVAL = int(os.getenv("SYNC_INTERVAL_SECONDS", "1800"))   # 30 دقيقة افتراضياً

# حالة المزامنة (Thread-safe تقريباً — قراءة فقط من الخارج)
_sync_state = {
    "last_sync":       None,      # datetime آخر مزامنة ناجحة
    "last_attempt":    None,      # datetime آخر محاولة
    "ok":              False,     # هل نجحت آخر مزامنة؟
    "in_progress":     False,     # هل هناك مزامنة جارية؟
    "rows_synced":     0,         # إجمالي الصفوف المُزامَنة
    "error":           None,      # آخر خطأ
}
_state_lock = threading.Lock()
_sync_lock = threading.Lock()      # يمنع مزامنتين متزامنتين


# ═══════════════════════════════════════════════════
#  ترتيب الجداول (يجب أن يكون الأب قبل الابن)
# ═══════════════════════════════════════════════════
def _get_sync_tables_in_order():
    """
    إرجاع الجداول بترتيب يُحترم فيه Foreign Keys.
    الجداول المستقلة أولاً ثم التابعة.
    """
    import models

    # الجداول بترتيب آمن للإدراج
    ordered = []

    # 1️⃣ الجداول المستقلة (بدون FK أو FK على نفسها)
    independent = [
        "Currency", "Account", "User", "AccountingPeriod",
        "Category", "Warehouse", "ExpenseCategory",
        "CostCenter", "CashBox", "AppSetting",
    ]
    # 2️⃣ الجداول التابعة (FK على المستقل)
    dependent = [
        "Party", "Item", "Employee",
        "KitComponent", "InventoryMovement",
        "Invoice", "InvoiceLine",
        "RecurringInvoiceTemplate", "RecurringInvoiceLine", "CostHistory",
        "JournalEntry", "JournalLine",
        "Payment", "CashTransfer",
        "Expense",
        "StockLevel", "WarehouseTransfer", "StockCount",
        "FixedAsset", "DepreciationRecord",
        "Loan", "LoanInstallment",
        "SalaryRecord",
        "Budget", "CostAllocation", "CostCenterTransaction",
    ]

    for name in independent + dependent:
        model = getattr(models, name, None)
        if model is not None:
            ordered.append(model)

    return ordered


# ═══════════════════════════════════════════════════
#  الدالة الأساسية: سحب من Neon إلى SQLite
# ═══════════════════════════════════════════════════
def _pull_all_from_neon(verbose: bool = False) -> dict:
    """
    سحب كل الجداول من Neon وإعادة كتابتها في SQLite.
    الاستراتيجية: TRUNCATE محلي ثم INSERT — بسيط وآمن.
    """
    from database import SessionLocal           # Neon
    from local_mirror import get_local_session  # SQLite

    remote: Session = SessionLocal()
    local: Session = get_local_session()
    result = {"ok": False, "tables": 0, "rows": 0, "errors": []}

    try:
        tables = _get_sync_tables_in_order()

        # ⭐ إيقاف FK مؤقتاً + مسح كل الجداول
        from sqlalchemy import text as _text_fk_off
        local.execute(_text_fk_off("PRAGMA foreign_keys=OFF"))

        for model in reversed(tables):     # احذف من الابن إلى الأب
            try:
                local.query(model).delete(synchronize_session=False)
            except Exception as e:
                logger.debug(f"حذف {model.__name__}: {e}")
        local.commit()

        # ⭐ نسخ البيانات من Neon
        for model in tables:
            try:
                rows = remote.query(model).all()
                if not rows:
                    continue

                # تحويل كل صف إلى dict
                mapper = sa_inspect(model)
                col_names = [c.key for c in mapper.columns]

                records = []
                for r in rows:
                    d = {c: getattr(r, c, None) for c in col_names}
                    records.append(d)

                # Bulk insert (سريع جداً)
                if records:
                    local.bulk_insert_mappings(model, records)

                result["tables"] += 1
                result["rows"] += len(records)

                if verbose:
                    logger.info(f"  ⬇️ {model.__name__}: {len(records)} صف")
            except Exception as e:
                err = f"{model.__name__}: {e}"
                result["errors"].append(err)
                logger.warning(f"⚠️ فشل نسخ {err}")

        local.commit()
        from sqlalchemy import text as _text_fk_on
        local.execute(_text_fk_on("PRAGMA foreign_keys=ON"))
        result["ok"] = len(result["errors"]) == 0

    except Exception as e:
        logger.error(f"❌ فشل المزامنة: {e}")
        local.rollback()
        result["errors"].append(str(e))
    finally:
        remote.close()
        local.close()

    return result


# ═══════════════════════════════════════════════════
#  الواجهة العامة
# ═══════════════════════════════════════════════════
def full_sync(verbose: bool = False) -> bool:
    """مزامنة كاملة (Thread-safe) — يدوية أو دورية."""
    if not _sync_lock.acquire(blocking=False):
        logger.info("⏳ مزامنة جارية بالفعل — تم التجاهل")
        return False

    try:
        with _state_lock:
            _sync_state["in_progress"] = True
            _sync_state["last_attempt"] = datetime.now()

        start = time.time()
        res = _pull_all_from_neon(verbose=verbose)
        # ⭐ بعد نجاح المزامنة نمسح الكاش حتى ترى الصفحات البيانات
        #    المُزامنة فوراً (كانت تبقى قديمة حتى انتهاء مدة الكاش)
        try:
            import streamlit as _st_clear
            _st_clear.cache_data.clear()
        except Exception:
            pass
        duration = time.time() - start

        with _state_lock:
            _sync_state["in_progress"] = False
            _sync_state["ok"] = res["ok"]
            _sync_state["rows_synced"] = res["rows"]
            _sync_state["error"] = res["errors"][0] if res["errors"] else None
            if res["ok"]:
                _sync_state["last_sync"] = datetime.now()

        logger.info(
            f"✅ مزامنة مكتملة في {duration:.1f}s — "
            f"{res['tables']} جدول، {res['rows']} صف"
            + (f" — أخطاء: {len(res['errors'])}" if res["errors"] else "")
        )
        return res["ok"]

    finally:
        _sync_lock.release()


def quick_sync(tables=None) -> bool:
    """⭐ مزامنة سريعة لجداول محددة فقط — أسرع بكثير من full_sync.

    افتراضياً تُزامن فقط الجداول الحرجة التي تتغير بالكتابات الشائعة
    (فواتير، حركات مخزون، قيود، مدفوعات، تحويلات، أصناف، أطراف،
     مصروفات، أرصدة مخزون). الجداول المستقرة (العملات، الحسابات،
    المستخدمون، التصنيفات، المخازن...) تُزامن دورياً بواسطة
    full_sync كل 30 دقيقة — لا حاجة لمزامنتها بعد كل حفظ.
    """
    if not _sync_lock.acquire(blocking=False):
        logger.info("⏳ مزامنة جارية بالفعل — تم التجاهل")
        return False

    try:
        from database import SessionLocal
        from local_mirror import get_local_session
        from sqlalchemy import inspect as sa_inspect, text as _text

        # الجداول الافتراضية للمزامنة السريعة (الحرجة فقط)
        if tables is None:
            import models
            tables = [
                models.Invoice, models.InvoiceLine, models.InventoryMovement,
                models.CostHistory, models.JournalEntry, models.JournalLine,
                models.Payment, models.CashTransfer,
                models.Item, models.KitComponent,
                models.Party, models.Expense,
                models.StockLevel, models.WarehouseTransfer, models.StockCount,
            ]

        remote = SessionLocal()
        local = get_local_session()
        synced = 0
        try:
            local.execute(_text("PRAGMA foreign_keys=OFF"))

            for model in tables:
                try:
                    # احذف المحلي
                    local.query(model).delete(synchronize_session=False)
                    # انسخ من Neon
                    rows = remote.query(model).all()
                    if rows:
                        mapper = sa_inspect(model)
                        col_names = [c.key for c in mapper.columns]
                        records = [{c: getattr(r, c, None) for c in col_names} for r in rows]
                        local.bulk_insert_mappings(model, records)
                    synced += 1
                except Exception as e:
                    logger.debug(f"⚠️ تخطّي {model.__name__}: {e}")

            local.commit()
            local.execute(_text("PRAGMA foreign_keys=ON"))
            logger.info(f"⚡ quick_sync: {synced}/{len(tables)} جدول")
            return True
        finally:
            remote.close()
            local.close()
    except Exception as e:
        logger.error(f"❌ فشل quick_sync: {e}")
        return False
    finally:
        _sync_lock.release()


def _background_loop(interval_seconds: int):
    """حلقة المزامنة الدورية (تعمل في Thread daemon)."""
    logger.info(f"🔁 بدء المزامنة الدورية كل {interval_seconds}s")
    while True:
        time.sleep(interval_seconds)
        try:
            full_sync(verbose=False)
        except Exception as e:
            logger.error(f"⚠️ خطأ في المزامنة الدورية: {e}")


def start_background_sync(interval_seconds: int = None) -> threading.Thread:
    """
    تشغيل المزامنة الخلفية:
    1. مزامنة أولية فورية (blocking)
    2. ثم Thread في الخلفية كل N ثانية
    """
    from local_mirror import init_local_schema

    interval = interval_seconds or SYNC_INTERVAL

    # 1️⃣ تأكد من وجود الجداول
    init_local_schema()

    # 2️⃣ مزامنة أولية
    logger.info("🚀 مزامنة أولية...")
    full_sync(verbose=True)

    # 3️⃣ Thread خلفي
    t = threading.Thread(
        target=_background_loop,
        args=(interval,),
        daemon=True,
        name="TechtoonSyncWorker",
    )
    t.start()
    return t


def get_sync_state() -> dict:
    """قراءة حالة المزامنة (للـ UI)."""
    with _state_lock:
        return dict(_sync_state)


def set_interval(seconds: int):
    """تغيير فترة المزامنة (يحتاج إعادة تشغيل)."""
    global SYNC_INTERVAL
    SYNC_INTERVAL = max(60, int(seconds))
    logger.info(f"⏱️ فترة المزامنة الجديدة: {SYNC_INTERVAL}s")