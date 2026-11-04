# -*- coding: utf-8 -*-
"""
================================================================
  Techtoon Accounting — سكربت تطبيق الإصلاحات
  apply_techtoon_fix.py
================================================================

يقوم هذا السكربت بتعديل ملفاتك الأصلية مباشرةً (in-place) لتطبيق
الإصلاحين:
  1) خطأ Streamlit: st.session_state.itm_page_num cannot be modified
     after the widget with key itm_page_num is instantiated
       (pages/2_📦_الاصناف.py  +  pages/32_📝_قيود_اليومية.py)

  2) أصناف الدمج (kit) لا تخصم من المكوّنات المُدمجة عند البيع/الشراء
       (services.py -> create_invoice)

كيفية التشغيل:
  1) ضع هذا الملف في جذر المشروع (بجانب app.py و services.py)
  2) افتح VS Code Terminal داخل المجلد
  3) شغّل:
        python apply_techtoon_fix.py
     أو لتحديد مجلد المشروع يدوياً:
        python apply_techtoon_fix.py "D:\\PROG\\Accounting Prog\\Techtoon Accounting"

السكربت:
  • يأخذ نسخة احتياطية قبل التعديل (backup_*.py.bak)
  • لا يحذف أي شيء خارج البلوكات المستهدفة
  • Idempotent: لو شغّلته مرتين لا يكسر شيئاً
  • يطبع تقريراً واضحاً بالعربية لكل ملف
================================================================
"""

import io
import os
import sys
import shutil
import datetime
import unicodedata


# ====================================================================
#  محتوى البلوكات قبل وبعد التعديل
# ====================================================================

# ---------- الإصلاح 1أ: صفحة الأصناف ----------
FIX_1A_OLD = """        if total_pages > 1:
            col_prev, col_next = st.columns(2)
            with col_prev:
                if st.button("⬅️ السابق", disabled=(page_num <= 1), key=f"{PFX}items_prev"):
                    st.session_state[f"{PFX}page_num"] = page_num - 1
                    st.rerun()
            with col_next:
                if st.button("التالي ➡️", disabled=(page_num >= total_pages), key=f"{PFX}items_next"):
                    st.session_state[f"{PFX}page_num"] = page_num + 1
                    st.rerun()
"""

FIX_1A_NEW = """        if total_pages > 1:
            # ⭐ إصلاح StreamlitWidgetAlreadyInstantiatedError:
            #    لا يجوز تعديل st.session_state[f"{PFX}page_num"] بعد إنشاء عنصر
            #    number_input بنفس المفتاح. الحل الرسمي: استخدام on_click callbacks
            #    (الِ callbacks تُنفّذ قبل إعادة رسم الواجهة فيمكنها تعديل المفتاح بأمان).
            _page_key = f"{PFX}page_num"

            def _go_prev_page():
                cur = st.session_state.get(_page_key, 1)
                if cur and cur > 1:
                    st.session_state[_page_key] = cur - 1

            def _go_next_page():
                cur = st.session_state.get(_page_key, 1)
                if cur and cur < total_pages:
                    st.session_state[_page_key] = cur + 1

            col_prev, col_next = st.columns(2)
            with col_prev:
                st.button("⬅️ السابق", disabled=(page_num <= 1),
                          key=f"{PFX}items_prev", on_click=_go_prev_page)
            with col_next:
                st.button("التالي ➡️", disabled=(page_num >= total_pages),
                          key=f"{PFX}items_next", on_click=_go_next_page)
"""

# ---------- الإصلاح 1ب: صفحة قيود اليومية (نفس النمط) ----------
FIX_1B_OLD = """        # أزرار التنقل
        if total_pages > 1:
            col_prev, col_next = st.columns(2)
            with col_prev:
                if st.button("⬅️ السابق", disabled=(page_num <= 1),
                             key=f"{PFX}prev"):
                    st.session_state[f"{PFX}page_num"] = page_num - 1
                    st.rerun()
            with col_next:
                if st.button("التالي ➡️", disabled=(page_num >= total_pages),
                             key=f"{PFX}next"):
                    st.session_state[f"{PFX}page_num"] = page_num + 1
                    st.rerun()
"""

FIX_1B_NEW = """        # أزرار التنقل
        if total_pages > 1:
            # ⭐ إصلاح StreamlitWidgetAlreadyInstantiatedError (نفس إصلاح صفحة الأصناف):
            #    استخدام on_click callbacks بدلاً من تعديل المفتاح بعد إنشاء الويدجت.
            _page_key = f"{PFX}page_num"

            def _go_prev_page():
                cur = st.session_state.get(_page_key, 1)
                if cur and cur > 1:
                    st.session_state[_page_key] = cur - 1

            def _go_next_page():
                cur = st.session_state.get(_page_key, 1)
                if cur and cur < total_pages:
                    st.session_state[_page_key] = cur + 1

            col_prev, col_next = st.columns(2)
            with col_prev:
                st.button("⬅️ السابق", disabled=(page_num <= 1),
                          key=f"{PFX}prev", on_click=_go_prev_page)
            with col_next:
                st.button("التالي ➡️", disabled=(page_num >= total_pages),
                          key=f"{PFX}next", on_click=_go_next_page)
"""

# ---------- الإصلاح 2: services.py — خصم مكوّنات أصناف الدمج ----------
FIX_2_OLD = """            if not item.is_kit:
                movement_type = 'in' if invoice_type == 'purchase' else 'out'
                movement = models.InventoryMovement(
                    item_id=item.id, date=invoice_date, type=movement_type,
                    quantity=item_data['quantity'], reference_id=invoice.id
                )
                db.add(movement)

                if invoice_type == 'purchase':
                    purchase_price = item_data['price']
                    total_cost = item_data['quantity'] * purchase_price
                    cost_record = CostHistory(
                        item_id=item.id, invoice_id=invoice.id,
                        purchase_price=purchase_price, quantity=item_data['quantity'],
                        total_cost=total_cost, purchase_date=invoice_date, supplier_id=party.id
                    )
                    db.add(cost_record)
                    # ⭐ الإصلاح: flush حتى يرى تحديث الأسطر سجل التكلفة الجديد
                    #    (الجلسة autoflush=False وكان السجل المعلق غير مرئي للاستعلام)
                    db.flush()
                    # ⭐ نمرر نفس db لتفادي جلسة متداخلة منفصلة
                    update_item_prices(item.id, db=db)
"""

FIX_2_NEW = """            # ⭐ إصلاح أصناف الدمج (kit): عند بيع/شراء صنف مدمج، يجب خصم/إضافة
            #    كميات مكوّناته من مخزون كل صنف مكوّن (وليس من مخزون الصنف المدمج نفسه
            #    الذي لا يحتفظ بمخزون مستقل). الحل: إنشاء InventoryMovement لكل مكوّن
            #    بمقدار (component.quantity × كمية الصنف المدمج في الفاتورة).
            #    الصنف العادي يحافظ على سلوكه القديم بدون أي تعديل.
            if item.is_kit:
                kit_qty = float(item_data['quantity'] or 0)
                kit_components = db.query(models.KitComponent).filter(
                    models.KitComponent.kit_item_id == item.id
                ).all()
                movement_type = 'in' if invoice_type == 'purchase' else 'out'
                for comp in kit_components:
                    comp_qty = float(comp.quantity or 0) * kit_qty
                    if comp_qty <= 0:
                        continue
                    db.add(models.InventoryMovement(
                        item_id=comp.component_item_id, date=invoice_date,
                        type=movement_type, quantity=comp_qty, reference_id=invoice.id
                    ))
            else:
                movement_type = 'in' if invoice_type == 'purchase' else 'out'
                movement = models.InventoryMovement(
                    item_id=item.id, date=invoice_date, type=movement_type,
                    quantity=item_data['quantity'], reference_id=invoice.id
                )
                db.add(movement)

                if invoice_type == 'purchase':
                    purchase_price = item_data['price']
                    total_cost = item_data['quantity'] * purchase_price
                    cost_record = CostHistory(
                        item_id=item.id, invoice_id=invoice.id,
                        purchase_price=purchase_price, quantity=item_data['quantity'],
                        total_cost=total_cost, purchase_date=invoice_date, supplier_id=party.id
                    )
                    db.add(cost_record)
                    # ⭐ الإصلاح: flush حتى يرى تحديث الأسطر سجل التكلفة الجديد
                    #    (الجلسة autoflush=False وكان السجل المعلق غير مرئي للاستعلام)
                    db.flush()
                    # ⭐ نمرر نفس db لتفادي جلسة متداخلة منفصلة
                    update_item_prices(item.id, db=db)
"""


# ---------- الإصلاح 3: cache_helpers.py — مفاتيح ناقصة في get_payments_index ----------
# الخطأ: KeyError: 'payment_type'  في  pages/8_💰_المدفوعات.py  السطر 226
# السبب: الدالة get_payments_index تُرجِع المفاتيح: id/number/date/party_id/amount/method/type
#        لكن صفحة المدفوعات تتوقع: payment_type/payment_method/reference_number/
#        party_name/currency_symbol/cash_box_name
# الحل: إضافة كل المفاتيح الناقصة مع الحفاظ على المفاتيح الأصلية (توافقية).
FIX_3_OLD = """@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_payments_index(limit: int = 500):
    import models
    db = _get_read_session()
    try:
        rows = db.query(models.Payment).order_by(models.Payment.id.desc()).limit(limit).all()
        return [{
            "id": r.id,
            "number": getattr(r, "reference_number", None),
            "date": getattr(r, "date", None),
            "party_id": getattr(r, "party_id", None),
            "amount": float(getattr(r, "amount", 0) or 0),
            "method": getattr(r, "payment_method", None),
            "type": getattr(r, "payment_type", None),
        } for r in rows]
    finally:
        db.close()
"""

FIX_3_NEW = """@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_payments_index(limit: int = 500):
    import models
    db = _get_read_session()
    try:
        rows = db.query(models.Payment).order_by(models.Payment.id.desc()).limit(limit).all()
        out = []
        for r in rows:
            # العلاقات قد تكون None لو حُذف الأصل — نتعامل بأمان
            party = getattr(r, "party", None)
            currency = getattr(r, "currency", None)
            cash_box = getattr(r, "cash_box", None)
            party_name = getattr(party, "name", None) or "-"
            currency_symbol = getattr(currency, "symbol", None) or "ج.م"
            cash_box_name = getattr(cash_box, "name", None) or "-"
            payment_type = getattr(r, "payment_type", None)
            payment_method = getattr(r, "payment_method", None)
            reference_number = getattr(r, "reference_number", None)
            out.append({
                # المفاتيح الأصلية (محفوظة للتوافقية)
                "id": r.id,
                "number": reference_number,
                "date": getattr(r, "date", None),
                "party_id": getattr(r, "party_id", None),
                "amount": float(getattr(r, "amount", 0) or 0),
                "method": payment_method,
                "type": payment_type,
                # ⭐ المفاتيح الإضافية التي تتوقعها صفحة المدفوعات
                #    (كانت ناقصة فتسبّب KeyError على payment_type و party_name
                #     و payment_method و reference_number و currency_symbol
                #     و cash_box_name)
                "payment_type": payment_type,
                "payment_method": payment_method,
                "reference_number": reference_number,
                "party_name": party_name,
                "currency_symbol": currency_symbol,
                "cash_box_name": cash_box_name,
                "notes": getattr(r, "notes", None),
                "currency_id": getattr(r, "currency_id", None),
                "cash_box_id": getattr(r, "cash_box_id", None),
            })
        return out
    finally:
        db.close()
"""


# ---------- الإصلاح 4: services.py — _after_write (أداء المزامنة) ----------
# المشكلة: بعد كل حفظ، _after_write() تفعل:
#   1) st.cache_data.clear() — تمسح كل الكاش → الصفحة التالية تعيد تحميل كل
#      البيانات من Neon (بطيء جداً)
#   2) تشغيل full_sync() في الخلفية بعد 2 ثانية فقط — full_sync تعمل
#      TRUNCATE + INSERT لكل ~30 جدول من Neon، تأخذ 10-60 ثانية وتستنزف
#      اتصال Neon طوال هذه المدة. لا يوجد throttle، فالحفظ المتتالي يُشغّل
#      مزامنات متداخلة.
# الحل:
#   1) استبدال st.cache_data.clear() بـ مسح كاش مستهدف (فقط الدوال المتأثرة)
#   2) إضافة throttle: لا تُزامن أكثر من مرة كل 60 ثانية
#   3) زيادة debounce من 2 إلى 10 ثوانٍ لتجميع أكبر للكتابات المتتالية
FIX_4_OLD = """def _after_write():
    \"\"\"
    بعد أي كتابة ناجحة:
    1) امسح كاش Streamlit (لتحديث العرض)
    2) ابدأ مزامنة خلفية (Debounced) محلياً فقط
    \"\"\"
    # 1) مسح الكاش
    try:
        import streamlit as st
        st.cache_data.clear()
    except Exception:
        pass

    # 2) مزامنة خلفية مُجمّعة
    try:
        if not is_local_mode():
            return
        if _sync_pending.is_set():
            return  # مزامنة قيد الانتظار بالفعل
        _sync_pending.set()

        def _do_sync():
            time.sleep(2)  # نافذة تجميع لعدة كتابات سريعة
            try:
                from sync_manager import full_sync
                full_sync(verbose=False)
            except Exception:
                pass
            finally:
                _sync_pending.clear()

        threading.Thread(target=_do_sync, daemon=True).start()
    except Exception:
        pass
"""

FIX_4_NEW = """def _after_write():
    \"\"\"
    بعد أي كتابة ناجحة:
    1) امسح كاش Streamlit بشكل مستهدف (فقط الدوال المتأثرة) — وليس الكل
       (كان st.cache_data.clear() يمسح كل شىء فيُجبر الصفحة التالية على
        إعادة تحميل كل البيانات من Neon — بطيء جداً)
    2) ابدأ مزامنة خلفية (Debounced 10s + Throttled 60s) محلياً فقط
       (كانت المزامنة تُشغّل بعد 2 ثانية من كل حفظ بلا throttle، فتتداخل
        المزامنات وتستنزف اتصال Neon)
    \"\"\"
    # 1) مسح الكاش المستهدف — أسرع بكثير من st.cache_data.clear()
    try:
        from cache_helpers import (
            get_invoices_index, get_payments_index,
            get_dashboard_kpis, get_inventory_summary,
            get_items_with_stock, get_stock_map,
        )
        # نمسح فقط الدوال الأكثر تأثراً بأي كتابة (الفواتير/المدفوعات/المخزون/اللوحة)
        # الدوال النادرة التغيّر (التصنيفات، العملات) تبقى محفوظة في الكاش
        get_dashboard_kpis.clear()
        get_invoices_index.clear()
        get_payments_index.clear()
        get_inventory_summary.clear()
        get_items_with_stock.clear()
        get_stock_map.clear()
    except Exception:
        # fallback: لو cache_helpers غير متاح في سياق ما، نمسح الكل
        try:
            import streamlit as st
            st.cache_data.clear()
        except Exception:
            pass

    # 2) مزامنة خلفية مُجمّعة + Throttle
    try:
        if not is_local_mode():
            return
        # ⭐ Throttle: لا تُزامن أكثر من مرة كل 60 ثانية
        now = time.time()
        last_sync = getattr(_after_write, '_last_sync_time', 0)
        if now - last_sync < 60:
            return  # مزامنة قريبة جداً — تخطّي لتقليل الحمل على Neon
        if _sync_pending.is_set():
            return  # مزامنة قيد الانتظار بالفعل
        _sync_pending.set()

        def _do_sync():
            time.sleep(10)  # ⭐ نافذة تجميع أكبر (كانت 2 ثانية)
            try:
                from sync_manager import full_sync
                full_sync(verbose=False)
                _after_write._last_sync_time = time.time()
            except Exception:
                pass
            finally:
                _sync_pending.clear()

        threading.Thread(target=_do_sync, daemon=True).start()
    except Exception:
        pass
"""


# ---------- الإصلاح 5أ: sync_manager.py — إضافة quick_sync (مزامنة سريعة) ----------
# المشكلة: full_sync تنسخ كل ~30 جدول من Neon بعد كل حفظ — بطيء جداً (10-60 ثانية)
# الحل: إضافة quick_sync تنسخ فقط الجداول الحرجة (التي تتغير بالكتابات الشائعة)
#       وتترك الجداول المستقرة (العملات، الحسابات، المستخدمون) للمزامنة الدورية.
# هذا إضافة (INSERTION) — لا يحذف أي دالة موجودة.
FIX_5A_OLD = """def _background_loop(interval_seconds: int):
    \"\"\"حلقة المزامنة الدورية (تعمل في Thread daemon).\"\"\"
"""

FIX_5A_NEW = """def quick_sync(tables=None) -> bool:
    \"\"\"⭐ مزامنة سريعة لجداول محددة فقط — أسرع بكثير من full_sync.

    افتراضياً تُزامن فقط الجداول الحرجة التي تتغير بالكتابات الشائعة
    (فواتير، حركات مخزون، قيود، مدفوعات، تحويلات، أصناف، أطراف،
     مصروفات، أرصدة مخزون). الجداول المستقرة (العملات، الحسابات،
    المستخدمون، التصنيفات، المخازن...) تُزامن دورياً بواسطة
    full_sync كل 30 دقيقة — لا حاجة لمزامنتها بعد كل حفظ.
    \"\"\"
    if not _sync_lock.acquire(blocking=False):
        logger.info(\"⏳ مزامنة جارية بالفعل — تم التجاهل\")
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
            local.execute(_text(\"PRAGMA foreign_keys=OFF\"))

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
                    logger.debug(f\"⚠️ تخطّي {model.__name__}: {e}\")

            local.commit()
            local.execute(_text(\"PRAGMA foreign_keys=ON\"))
            logger.info(f\"⚡ quick_sync: {synced}/{len(tables)} جدول\")
            return True
        finally:
            remote.close()
            local.close()
    except Exception as e:
        logger.error(f\"❌ فشل quick_sync: {e}\")
        return False
    finally:
        _sync_lock.release()


def _background_loop(interval_seconds: int):
    \"\"\"حلقة المزامنة الدورية (تعمل في Thread daemon).\"\"\"
"""


# ---------- الإصلاح 5ب: services.py — استبدال full_sync بـ quick_sync في _after_write ----------
# الآن بعد أن أصبح لدينا quick_sync (أسرع بكثير)، نستخدمها بدلاً من full_sync
# في _after_write. المزامنة الدورية الكاملة (full_sync كل 30 دقيقة) تبقى كما هي.
FIX_5B_OLD = """                from sync_manager import full_sync
                full_sync(verbose=False)"""

FIX_5B_NEW = """                from sync_manager import quick_sync
                quick_sync()"""


# ====================================================================
#  أدوات مساعدة
# ====================================================================

def read_text(path):
    """قراءة الملف كـ UTF-8 مع الحفاظ على CRLF."""
    with open(path, "rb") as f:
        raw = f.read()
    for enc in ("utf-8-sig", "utf-8", "cp1256", "latin-1"):
        try:
            return raw.decode(enc)
        except UnicodeDecodeError:
            continue
    raise RuntimeError(f"تعذّر فك ترميز الملف: {path}")


def write_text(path, text, native_eol="\r\n"):
    """كتابة الملف بترميز UTF-8 مع الحفاظ على نمط نهاية السطر الأصلي."""
    # إذا كان الملف الأصلي CRLF والنص الجديد LF فقط، نحوّل
    if native_eol == "\r\n" and "\r\n" not in text and "\n" in text:
        text = text.replace("\n", "\r\n")
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)


def normalize_eol(text):
    """تطبيع نهايات الأسطر إلى LF للمقارنة."""
    return text.replace("\r\n", "\n").replace("\r", "\n")


def find_pages_file(pages_dir, ascii_prefix, arabic_substr):
    """يبحث عن ملف في pages/ يبدأ بالبادئة ويحتوي السلسلة العربية.

    ضروري لأن أسماء الملفات قد تُخزَّن بنمط Unicode مختلف (NFC/NFD)
    حسب نظام التشغيل."""
    target_nfc = unicodedata.normalize("NFC", arabic_substr)
    if not os.path.isdir(pages_dir):
        return None
    for name in os.listdir(pages_dir):
        if not name.endswith(".py"):
            continue
        if not name.startswith(ascii_prefix):
            continue
        name_nfc = unicodedata.normalize("NFC", name)
        if target_nfc in name_nfc:
            return os.path.join(pages_dir, name)
    return None


def backup_file(path):
    """نسخة احتياطية بتاريخ ووقت — لا يُكتب فوق نسخة سابقة."""
    ts = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
    bak = f"{path}.bak_{ts}"
    shutil.copy2(path, bak)
    return bak


def apply_patch(file_path, old_block, new_block, label,
                idempotency_marker):
    """تطبيق تعديل inplace. يرجع True إذا طُبّق، False إذا كان موجوداً مسبقاً."""
    if not os.path.isfile(file_path):
        print(f"  [{label}] ❌ الملف غير موجود: {file_path}")
        return False

    text = read_text(file_path)
    # detect native eol
    native_eol = "\r\n" if "\r\n" in text else "\n"

    # Idempotency check: if marker already present, skip
    if idempotency_marker in text:
        print(f"  [{label}] ✅ الإصلاح مُطّبّق مسبقاً — تُجاهل (لا شيء يكسر)")
        return True

    # محاولة المطابقة الحرفية أولاً
    if old_block in text:
        new_text = text.replace(old_block, new_block, 1)
    else:
        # محاولة بتطبيع نهايات الأسطر
        norm_text = normalize_eol(text)
        norm_old = normalize_eol(old_block)
        if norm_old in norm_text:
            # حقن new_block بنفس نمط نهاية السطر الأصلي
            native_new = new_block
            if native_eol == "\r\n":
                native_new = new_block.replace("\n", "\r\n")
            new_text = norm_text.replace(norm_old, native_new, 1)
        else:
            print(f"  [{label}] ⚠️ لم يُوجد البلوك المستهدف — ربما تم تعديله مسبقاً بشكل مختلف")
            return False

    # نسخة احتياطية
    bak = backup_file(file_path)
    print(f"  [{label}] ✔ نسخة احتياطية: {os.path.basename(bak)}")
    write_text(file_path, new_text, native_eol=native_eol)
    print(f"  [{label}] ✅ تم تطبيق الإصلاح")
    return True


# ====================================================================
#  main
# ====================================================================

def main():
    print("=" * 64)
    print("  Techtoon Accounting — سكربت تطبيق الإصلاحات")
    print("=" * 64)

    # 1) تحديد جذر المشروع
    project_root = None
    if len(sys.argv) >= 2:
        project_root = sys.argv[1]
    else:
        # افتراضياً نستخدم مجلد السكربت نفسه
        project_root = os.getcwd()
    project_root = os.path.abspath(project_root)
    print(f"\nجذر المشروع: {project_root}")

    # التحقق من وجود الملفات الأساسية للتمييز بين جذر المشروع وغيره
    services_py = os.path.join(project_root, "services.py")
    cache_helpers_py = os.path.join(project_root, "cache_helpers.py")
    sync_manager_py = os.path.join(project_root, "sync_manager.py")
    pages_dir = os.path.join(project_root, "pages")
    if not os.path.isfile(services_py) or not os.path.isdir(pages_dir):
        print(
            f"\n❌ تعذر العثي على 'services.py' و 'ضpages/' في هذا المجلد."
            f"\n   السبب: السكربت يجب أن يكون في جذر مشروع Techtoon (بجانب app.py و services.py)."
            f"\n   التصلح: شغّل:"
            f"\n       python apply_techtoon_fix.py \"D:\\\\PROG\\\\Accounting Prog\\\\Techtoon Accounting\""
        )
        sys.exit(1)

    # 2) البحث عن ملفات الصفحات بالبادئة + السلسلة العربية
    items_page = find_pages_file(pages_dir, "2_", "الاصناف")
    journal_page = find_pages_file(pages_dir, "32_", "قيود")

    print(f"\nملفات هدف:")
    print(f"  • services.py                       : {services_py}")
    print(f"  • cache_helpers.py                  : {cache_helpers_py}")
    print(f"  • sync_manager.py                   : {sync_manager_py}")
    print(f"  • صفحة الأصناف (2_...):       {items_page or 'غير موجود'}")
    print(f"  • صفحة قيود اليومية (32_...): {journal_page or 'غير موجود'}")

    # 3) تطبيق الإصلاحات
    print("\n" + "-" * 64)
    print("تطبيق الإصلاحات:")
    print("-" * 64)

    results = []

    if items_page:
        ok = apply_patch(
            items_page, FIX_1A_OLD, FIX_1A_NEW,
            label="1أ صفحة الأصناف",
            idempotency_marker="on_click=_go_next_page",
        )
        results.append(ok)
    else:
        print("  [1أ] ⚠️ لم يُوجد ملف صفحة الأصناف — تُجاهل")

    if journal_page:
        ok = apply_patch(
            journal_page, FIX_1B_OLD, FIX_1B_NEW,
            label="1ب صفحة قيود اليومية",
            idempotency_marker="on_click=_go_next_page",
        )
        results.append(ok)
    else:
        print("  [1ب] ⚠️ لم يُوجد ملف صفحة قيود اليومية — تُجاهل")

    ok = apply_patch(
        services_py, FIX_2_OLD, FIX_2_NEW,
        label="2   services.py (خصم أصناف الدمج)",
        idempotency_marker="kit_qty = float(item_data['quantity'] or 0)",
    )
    results.append(ok)

    ok = apply_patch(
        services_py, FIX_4_OLD, FIX_4_NEW,
        label="4   services.py (أداء _after_write)",
        idempotency_marker="_after_write._last_sync_time = time.time()",
    )
    results.append(ok)

    # ⭐ إصلاح 5أ: إضافة quick_sync لـ sync_manager.py (INSERTION — لا يحذف شيئاً)
    if os.path.isfile(sync_manager_py):
        ok = apply_patch(
            sync_manager_py, FIX_5A_OLD, FIX_5A_NEW,
            label="5أ  sync_manager.py (إضافة quick_sync)",
            idempotency_marker="def quick_sync(tables=None)",
        )
        results.append(ok)
    else:
        print("  [5أ] ⚠️ لم يُوجد ملف sync_manager.py — تُجاهل")

    # ⭐ إصلاح 5ب: استبدال full_sync بـ quick_sync في _after_write
    ok = apply_patch(
        services_py, FIX_5B_OLD, FIX_5B_NEW,
        label="5ب  services.py (استخدام quick_sync)",
        idempotency_marker="quick_sync()",
    )
    results.append(ok)

    if os.path.isfile(cache_helpers_py):
        ok = apply_patch(
            cache_helpers_py, FIX_3_OLD, FIX_3_NEW,
            label="3   cache_helpers.py (مفاتيح المدفوعات)",
            idempotency_marker='"party_name": party_name',
        )
        results.append(ok)
    else:
        print("  [3] ⚠️ لم يُوجد ملف cache_helpers.py — تُجاهل")

    # 4) تحقق صياغي (Syntax check) لكل ملف تم تعديله
    print("\n" + "-" * 64)
    print("تحقق صياغي (Python syntax):")
    print("-" * 64)
    import py_compile
    syntax_ok = True
    for p in filter(None, (items_page, journal_page, services_py, cache_helpers_py, sync_manager_py)):
        try:
            py_compile.compile(p, doraise=True)
            print(f"  ✔ {os.path.basename(p)}")
        except py_compile.PyCompileError as e:
            print(f"  ❌ {os.path.basename(p)}: {e}")
            syntax_ok = False

    print("\n" + "=" * 64)
    if all(results) and syntax_ok:
        print("✅ تم تطبيق جميع الإصلاحات بنجاح.")
        print("الخطوة التالية: أعد تشغيل التطبيق  streamlit run app.py")
    elif syntax_ok:
        print("⚠️ تم التعديل مع تخطي بعض الملفات (شاهد الأعلى).")
    else:
        print("❌ هناك خطأ صياغي — استعد النسخة الاحتياطية (.bak_*)")
    print("=" * 64)


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"\n❌ خطأ غير متوقع: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(2)
