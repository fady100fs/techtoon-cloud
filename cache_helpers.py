# cache_helpers.py
import os
import streamlit as st
from sqlalchemy.orm import joinedload


class TTL:
    VERY_SHORT = 30
    SHORT = 120
    MEDIUM = 300
    LONG = 900
    VERY_LONG = 3600


def _get_ttl():
    mode = os.getenv("DEPLOY_MODE", "cloud").lower()
    if mode == "local":
        return int(os.getenv("LOCAL_CACHE_TTL", "120"))
    return int(os.getenv("CLOUD_CACHE_TTL", "3600"))


CACHE_TTL = _get_ttl()


def _get_read_session():
    from database import get_read_session
    return get_read_session()


def _get_fresh_session():
    """⭐ جلسة قراءة من قاعدة البيانات المصدر مباشرة (نفس مصدر الكتابة).

    تُستخدم لأرقام المخزون الحرجة (تلميح الفواتير، قائمة الأصناف، لوحة
    التحكم) حتى تظهر الكميات فوراً بعد حفظ فواتير البيع/الشراء دون
    انتظار المزامنة — كان الاعتماد على المرآة يعرض رصيداً صفراً أو قديماً.
    """
    from database import SessionLocal
    return SessionLocal()




@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_invoices_index(limit: int = 1000):
    """فهرس الفواتير الكامل — يُرجع كل الحقول التي تحتاجها الصفحات."""
    import models
    from sqlalchemy import func
    db = _get_read_session()
    try:
        rows = (
            db.query(models.Invoice)
            .options(joinedload(models.Invoice.party), joinedload(models.Invoice.currency))
            .order_by(models.Invoice.id.desc())
            .limit(limit)
            .all()
        )
        if not rows:
            return []

        # خريطة الأطراف
        party_ids = list({r.party_id for r in rows if r.party_id})
        party_map = {}
        if party_ids:
            party_map = {p.id: (p.name or "") for p in db.query(models.Party).filter(
                models.Party.id.in_(party_ids)
            ).all()}

        # خريطة المستخدمين
        user_ids = list({r.created_by for r in rows if r.created_by})
        user_map = {}
        if user_ids:
            try:
                user_map = {u.id: (u.full_name or u.username or "") for u in db.query(models.User).filter(
                    models.User.id.in_(user_ids)
                ).all()}
            except Exception:
                pass

        # مجاميع المدفوعات لكل فاتورة
        payment_sums = {}
        try:
            rows_pay = db.query(
                models.Payment.reference_number,
                func.sum(models.Payment.amount).label("total"),
            ).group_by(models.Payment.reference_number).all()
            payment_sums = {r.reference_number: float(r.total or 0) for r in rows_pay}
        except Exception:
            pass

        result = []
        for r in rows:
            num = getattr(r, "invoice_number", None) or getattr(r, "number", None)
            net = getattr(r, "net_amount", None)
            if net is None:
                net = getattr(r, "total_amount", None)
            net = float(net or 0)

            paid = payment_sums.get(num, 0.0)
            remaining = max(0.0, net - paid)

            result.append({
                "id": r.id,
                "invoice_number": num,
                "number": num,
                "date": getattr(r, "date", None),
                "party_id": getattr(r, "party_id", None),
                "party_name": party_map.get(getattr(r, "party_id", None), "—"),
                "created_by": getattr(r, "created_by", None),
                "created_by_name": user_map.get(getattr(r, "created_by", None), "النظام"),
                "total": net,
                "total_amount": net,
                "net_amount": net,
                "paid_amount": paid,
                "remaining": remaining,
                "type": getattr(r, "type", None),
                "status": getattr(r, "status", None),
                "currency_id": getattr(r, "currency_id", None),
            })
        return result
    finally:
        db.close()



@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
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


@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_dashboard_kpis():
    import models
    from sqlalchemy import func
    from datetime import datetime
    db = _get_read_session()
    try:
        kpis = {}

        # المفاتيح القديمة (توافقية مع أي استخدام سابق)
        kpis["invoices_count"] = db.query(func.count(models.Invoice.id)).scalar() or 0
        kpis["parties_count"] = db.query(func.count(models.Party.id)).scalar() or 0
        kpis["items_count"] = db.query(func.count(models.Item.id)).scalar() or 0
        total = db.query(func.coalesce(func.sum(models.Invoice.net_amount), 0)).scalar()
        kpis["invoices_total"] = float(total or 0)
        total_pay = db.query(func.coalesce(func.sum(models.Payment.amount), 0)).scalar()
        kpis["payments_total"] = float(total_pay or 0)

        # ⭐ المفاتيح التي تتوقعها لوحة التحكم (كانت ناقصة وتسبب KeyError)
        total_sales = db.query(func.coalesce(func.sum(models.Invoice.net_amount), 0)).filter(
            models.Invoice.type == "sale",
            models.Invoice.status != "cancelled",
        ).scalar()
        kpis["total_sales"] = float(total_sales or 0)

        month_start = datetime.now().replace(day=1, hour=0, minute=0, second=0, microsecond=0)
        monthly_sales = db.query(func.coalesce(func.sum(models.Invoice.net_amount), 0)).filter(
            models.Invoice.type == "sale",
            models.Invoice.status != "cancelled",
            models.Invoice.date >= month_start,
        ).scalar()
        kpis["monthly_sales"] = float(monthly_sales or 0)

        total_expenses = db.query(func.coalesce(func.sum(models.Expense.amount), 0)).scalar()
        kpis["total_expenses"] = float(total_expenses or 0)

        kpis["total_invoices"] = int(kpis["invoices_count"])
        kpis["total_customers"] = db.query(func.count(models.Party.id)).filter(
            models.Party.type == "customer"
        ).scalar() or 0

        return kpis
    finally:
        db.close()


@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_inventory_summary():
    """⭐ ملخص حالة المخزون — كانت الدالة مفقودة وتسبب ImportError في لوحة التحكم."""
    import models
    from sqlalchemy import func, case
    db = _get_fresh_session()
    try:
        stock_map = {}
        try:
            try:
                from sqlalchemy import text as _text
                _r = db.execute(_text("SELECT item_id, balance FROM v_stock_balance")).fetchall()
                stock_map = {r[0]: float(r[1] or 0) for r in _r}
            except Exception:
                rows = db.query(
                    models.InventoryMovement.item_id,
                    func.sum(case(
                        (models.InventoryMovement.type == "in", models.InventoryMovement.quantity),
                        else_=-models.InventoryMovement.quantity
                    )).label("balance")
                ).group_by(models.InventoryMovement.item_id).all()
                stock_map = {iid: float(b or 0) for iid, b in rows}
        except Exception:
            try:
                rows2 = db.query(
                    models.StockLevel.item_id,
                    func.coalesce(func.sum(models.StockLevel.quantity), 0)
                ).group_by(models.StockLevel.item_id).all()
                stock_map = {iid: float(q or 0) for iid, q in rows2}
            except Exception:
                stock_map = {}

        total_items = 0
        good_stock_count = 0
        low_stock_count = 0
        total_value = 0.0

        items = db.query(models.Item).all()
        for it in items:
            if getattr(it, "is_kit", False):
                continue  # الأصناف المدمجة لا تُحسب من حركات المخزون
            total_items += 1
            qty = stock_map.get(it.id, 0.0)
            min_stock = float(getattr(it, "min_stock", 0) or 0)
            if qty <= min_stock:
                low_stock_count += 1
            else:
                good_stock_count += 1
            cost = float(getattr(it, "cost_price", 0) or 0)
            total_value += max(qty, 0.0) * cost

        return {
            "total_items": total_items,
            "good_stock_count": good_stock_count,
            "low_stock_count": low_stock_count,
            "total_value": round(total_value, 2),
        }
    finally:
        db.close()


@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_items_with_stock(limit: int = 5000):
    import models
    from sqlalchemy import func, case
    db = _get_fresh_session()
    try:
        items = db.query(models.Item).limit(limit).all()
        if not items:
            return []
        cat_map = {}
        try:
            cat_map = {c.id: (c.name or "") for c in db.query(models.Category).all()}
        except Exception:
            pass
        stock_map = {}
        try:
            try:
                from sqlalchemy import text as _text
                _r = db.execute(_text("SELECT item_id, balance FROM v_stock_balance")).fetchall()
                stock_map = {r[0]: float(r[1] or 0) for r in _r}
            except Exception:
                rows = db.query(
                    models.InventoryMovement.item_id,
                    func.sum(case(
                        (models.InventoryMovement.type == "in", models.InventoryMovement.quantity),
                        else_=-models.InventoryMovement.quantity
                    )).label("balance")
                ).group_by(models.InventoryMovement.item_id).all()
                stock_map = {iid: float(b or 0) for iid, b in rows}
        except Exception:
            try:
                rows2 = db.query(
                    models.StockLevel.item_id,
                    func.coalesce(func.sum(models.StockLevel.quantity), 0)
                ).group_by(models.StockLevel.item_id).all()
                stock_map = {iid: float(q or 0) for iid, q in rows2}
            except Exception:
                pass
        result = []
        for r in items:
            item_id = getattr(r, "id", None)
            cat_id = getattr(r, "category_id", None)
            result.append({
                "id": item_id,
                "name": getattr(r, "name", "") or "",
                "code": getattr(r, "code", "") or "",
                "barcode": getattr(r, "barcode", "") or "",
                "category_id": cat_id,
                "category_name": cat_map.get(cat_id, "N/A"),
                "cost_price": float(getattr(r, "cost_price", 0) or 0),
                "sell_price": float(getattr(r, "sell_price", 0) or 0),
                "avg_cost_price": float(getattr(r, "avg_cost_price", 0) or 0),
                "is_kit": bool(getattr(r, "is_kit", False)),
                "min_stock": float(getattr(r, "min_stock", 0) or 0),
                "current_stock": float(stock_map.get(item_id, 0.0)),
                "total_purchased_qty": float(getattr(r, "total_purchased_qty", 0) or 0),
                "price": float(getattr(r, "sell_price", 0) or 0),
                "unit": getattr(r, "unit", "") or "",
            })
        return result
    finally:
        db.close()


@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_active_categories():
    import models
    db = _get_read_session()
    try:
        rows = db.query(models.Category).order_by(models.Category.name).all()
        return [(r.id, (r.name or "")) for r in rows]
    finally:
        db.close()


@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_stock_map():
    import models
    from sqlalchemy import func, case
    db = _get_fresh_session()
    try:
        try:
            from sqlalchemy import text as _text
            _r = db.execute(_text("SELECT item_id, balance FROM v_stock_balance")).fetchall()
            return {r[0]: float(r[1] or 0) for r in _r}
        except Exception:
            rows = db.query(
                models.InventoryMovement.item_id,
                func.sum(case(
                    (models.InventoryMovement.type == "in", models.InventoryMovement.quantity),
                    else_=-models.InventoryMovement.quantity
                )).label("balance")
            ).group_by(models.InventoryMovement.item_id).all()
            return {iid: float(b or 0) for iid, b in rows}
    finally:
        db.close()


def invalidate_cache():
    st.cache_data.clear()


def invalidate_all():
    st.cache_data.clear()


def clear_cache():
    st.cache_data.clear()


def invalidate_and_sync():
    invalidate_cache()
    try:
        from database import is_local_mode
        try:
            from database import DATABASE_URL_IS_LOCAL as _db_local
        except Exception:
            _db_local = False
        if is_local_mode() and not _db_local:  # ⭐ تخطَّ مزامنة المرآة إذا كانت القاعدة نفسها محلية
            import threading
            from sync_manager import full_sync
            threading.Thread(target=full_sync, daemon=True).start()
    except Exception:
        pass


@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_parties_index(limit: int = 2000):
    import models
    db = _get_read_session()
    try:
        rows = db.query(models.Party).order_by(models.Party.id.desc()).limit(limit).all()
        return [{"id": r.id, "name": getattr(r,"name","") or "", "type": getattr(r,"type","") or "",
                 "phone": getattr(r,"phone","") or "", "credit_limit": float(getattr(r,"credit_limit",0) or 0)} for r in rows]
    finally: db.close()

@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_cash_boxes_index():
    import models
    db = _get_read_session()
    try:
        rows = db.query(models.CashBox).all()
        return [{"id": r.id, "name": getattr(r,"name","") or "", "code": getattr(r,"code","") or "",
                 "type": str(getattr(r,"type","")) or ""} for r in rows]
    finally: db.close()

@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_all_users():
    import models
    db = _get_read_session()
    try:
        rows = db.query(models.User).all()
        return [{"id": r.id, "username": getattr(r,"username","") or "",
                 "full_name": getattr(r,"full_name","") or "",
                 "role": str(getattr(r,"role","")) or "", "is_active": bool(getattr(r,"is_active",True))} for r in rows]
    finally: db.close()

@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_warehouses_index():
    import models
    db = _get_read_session()
    try:
        rows = db.query(models.Warehouse).filter(models.Warehouse.is_active == True).all()
        return [{"id": r.id, "name": getattr(r,"name","") or "", "code": getattr(r,"code","") or ""} for r in rows]
    finally: db.close()


def preload_cache():
    try:
        get_dashboard_kpis()
        get_items_with_stock()
        get_stock_map()
        get_active_categories()
    except Exception:
        pass
