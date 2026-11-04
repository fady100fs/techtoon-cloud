# invoice_page_common.py
"""
وحدة مشتركة لصفحات الفواتير (بيع / شراء).
- تاريخ ووقت قابلان للتعديل
- رقم فاتورة قابل للتغيير
"""
import re
import sys
import traceback
from datetime import datetime, date, time as dtime
from pathlib import Path

import pandas as pd
import streamlit as st

ROOT = Path(__file__).resolve().parent
if str(ROOT) not in sys.path:
    sys.path.append(str(ROOT))

from sidebar import render_sidebar, queue_state_updates
from sqlalchemy import Float, Integer, Numeric
from database import SessionLocal
import models
from services import create_invoice, create_payment
from period_guard import check_period_open
from auth_required import require_login, get_current_user_id, get_current_user_name
from code_search import CodeSearch, FormState
from form_manager import clear_form, show_clear_hint
from input_helpers import price_input, qty_input
from cache_helpers import get_stock_map   # ⭐ لتلميح الكمية المتاحة تحت الصنف


# ═══════════════════════════════════════════════════════════
#  ثوابت
# ═══════════════════════════════════════════════════════════
CODE_FIELDS = [f for f in ("code", "item_code", "barcode", "sku") if hasattr(models.Item, f)]

PAYMENT_METHODS = {
    "cash": "نقدي",
    "bank_transfer": "تحويل بنكي",
    "check": "شيك",
}


# ═══════════════════════════════════════════════════════════
#  أدوات مساعدة
# ═══════════════════════════════════════════════════════════
def zero_null_numbers(obj):
    changed = False
    for col in obj.__table__.columns:
        if col.primary_key or col.foreign_keys:
            continue
        if isinstance(col.type, (Integer, Float, Numeric)) and getattr(obj, col.key, 0) is None:
            setattr(obj, col.key, 0)
            changed = True
    return changed


def used_invoice_numbers(db, invoice_type=None):
    used = {}
    if hasattr(models.Invoice, "invoice_number"):
        q = db.query(models.Invoice)
        if invoice_type:
            q = q.filter(models.Invoice.type == invoice_type)
        for (num,) in q.with_entities(models.Invoice.invoice_number).all():
            m = re.search(r"(\d+)$", str(num or ""))
            if m:
                used.setdefault(int(m.group(1)), str(num))
    return used


def format_invoice_number(n, invoice_type="sale"):
    prefix = "SAL" if invoice_type == "sale" else "PUR"
    return f"{prefix}-{int(n):06d}"


def _line_info(db, ln):
    name = None
    item_obj = getattr(ln, "item", None)
    if item_obj is not None:
        name = getattr(item_obj, "name", None)
    if name is None and getattr(ln, "item_id", None) is not None:
        it = db.query(models.Item).filter(models.Item.id == ln.item_id).first()
        name = it.name if it else None
    if name is None:
        name = getattr(ln, "item_name", None) or getattr(ln, "description", None) or "—"
    qty = getattr(ln, "quantity", None)
    price = next((getattr(ln, a) for a in ("unit_price", "price", "sell_price", "cost_price")
                  if getattr(ln, a, None) is not None), None)
    total = next((getattr(ln, a) for a in ("total", "total_price", "line_total")
                  if getattr(ln, a, None) is not None), None)
    if total is None and qty is not None and price is not None:
        total = qty * price
    return {"الصنف": name, "الكمية": qty, "السعر": price, "الإجمالي": total}


def invoice_lines(db, invoice):
    for rel in models.Invoice.__mapper__.relationships:
        if rel.uselist and hasattr(rel.mapper.class_, "quantity"):
            return [_line_info(db, ln) for ln in getattr(invoice, rel.key)]
    return None


def _go_new(key, value):
    st.session_state[key] = value


def _clear_edit_state():
    for k in ("_editing_invoice_id", "_editing_invoice_no", "_editing_invoice_type"):
        st.session_state.pop(k, None)


def _delete_invoice_fully(db, invoice):
    if invoice.date:
        check_period_open(invoice.date, entity="حذف فاتورة")

    inv_id = invoice.id
    db.query(models.InvoiceLine).filter(
        models.InvoiceLine.invoice_id == inv_id
    ).delete(synchronize_session=False)
    db.query(models.InventoryMovement).filter(
        models.InventoryMovement.reference_id == inv_id
    ).delete(synchronize_session=False)
    db.query(models.CostHistory).filter(
        models.CostHistory.invoice_id == inv_id
    ).delete(synchronize_session=False)
    je_ids = [row[0] for row in db.query(models.JournalEntry.id).filter(
        models.JournalEntry.reference_id == inv_id,
        models.JournalEntry.reference_type.in_(["invoice", "cogs"]),
    ).all()]
    if je_ids:
        db.query(models.JournalLine).filter(
            models.JournalLine.entry_id.in_(je_ids)
        ).delete(synchronize_session=False)
        db.query(models.JournalEntry).filter(
            models.JournalEntry.id.in_(je_ids)
        ).delete(synchronize_session=False)
    try:
        db.query(models.Payment).filter(
            models.Payment.reference_number == invoice.invoice_number,
            models.Payment.party_id == invoice.party_id,
        ).delete(synchronize_session=False)
    except Exception:
        pass
    db.delete(invoice)
    db.commit()


def _load_invoice_data(db, invoice):
    inv_id = invoice.id
    inv_no_str = invoice.invoice_number
    inv_type = invoice.type or "sale"
    party_id = invoice.party_id
    discount = float(invoice.discount_percentage or 0.0)
    tax = float(invoice.tax_rate or 0.0)
    inv_date = invoice.date
    lines = db.query(models.InvoiceLine).filter(
        models.InvoiceLine.invoice_id == inv_id
    ).all()
    new_items = []
    for ln in lines:
        item = db.query(models.Item).filter(models.Item.id == ln.item_id).first()
        if item:
            new_items.append({
                "item_name": item.name,
                "quantity": float(ln.quantity or 0),
                "price": float(ln.price or 0),
            })
    return {
        "id": inv_id,
        "no_str": inv_no_str,
        "type": inv_type,
        "party_id": party_id,
        "discount": discount,
        "tax": tax,
        "date": inv_date,
        "items": new_items,
    }


def _prepare_invoice_for_edit(data, fs, no_key, dt_date_key, dt_time_key):
    """تحميل بيانات الفاتورة لوضع التعديل — تاريخ ووقت ورقم."""
    st.session_state["_editing_invoice_id"] = data["id"]
    st.session_state["_editing_invoice_no"] = data["no_str"]
    st.session_state["_editing_invoice_type"] = data["type"]

    m = re.search(r"(\d+)$", str(data["no_str"] or ""))
    inv_num = int(m.group(1)) if m else 1

    st.session_state["invoice_items"] = list(data.get("items", []))
    st.session_state.pop("inv_items_editor", None)

    original_dt = data.get("date") or datetime.now()
    set_vals = {
        no_key: inv_num,
        dt_date_key: original_dt.date() if hasattr(original_dt, "date") else original_dt,
        dt_time_key: original_dt.time() if hasattr(original_dt, "time") else dtime(0, 0),
        fs.key("discount"): data["discount"],
        fs.key("tax"): data["tax"],
    }
    if "party_id" in data and data["party_id"] is not None:
        set_vals[fs.key(f"party_{data['type']}")] = data["party_id"]

    queue_state_updates(set_values=set_vals)


def _get_invoice_paid_amount(db, invoice):
    try:
        return sum(float(p.amount or 0) for p in db.query(models.Payment).filter(
            models.Payment.reference_number == invoice.invoice_number,
            models.Payment.party_id == invoice.party_id,
        ).all())
    except Exception:
        return 0.0


def _get_invoice_payments(db, invoice):
    try:
        return db.query(models.Payment).filter(
            models.Payment.reference_number == invoice.invoice_number,
            models.Payment.party_id == invoice.party_id,
        ).order_by(models.Payment.date.desc()).all()
    except Exception:
        return []


def _update_invoice_status(db, invoice):
    net = float(invoice.net_amount or 0)
    paid = _get_invoice_paid_amount(db, invoice)
    if paid <= 0:
        new_status = "pending"
    elif paid + 0.01 >= net:
        new_status = "paid"
    else:
        new_status = "partial"
    if invoice.status != new_status:
        invoice.status = new_status
        db.commit()


def _status_badge(status):
    s = (status or "").lower()
    if s == "paid":
        return "🟢 مدفوعة", "#11998e"
    if s == "partial":
        return "🟡 مدفوعة جزئيًا", "#f59e0b"
    if s == "cancelled":
        return "⚫ ملغاة", "#555"
    return "🔴 غير مدفوعة", "#ef4444"


def _delete_payment(db, payment_id):
    try:
        p = db.query(models.Payment).filter(models.Payment.id == payment_id).first()
        if p:
            if p.date:
                check_period_open(p.date, entity="حذف دفعة")
            db.delete(p)
            db.commit()
            return True
    except Exception:
        db.rollback()
    return False


# ═══════════════════════════════════════════════════════════
#  قسم الدفع للفاتورة الجديدة
# ═══════════════════════════════════════════════════════════
def render_payment_section_for_new(db, net_amount, key_prefix="newinv"):
    st.markdown("---")
    st.markdown("### 💵 دفع الفاتورة (اختياري)")
    st.caption(
        "لو عايز تسجّل دفعة مع حفظ الفاتورة فورًا، فعّل الخيار ده. "
        "أو اتركه فاضي وأضف الدفعات لاحقًا من صفحة الفاتورة."
    )

    enable_payment = st.checkbox(
        "✅ تسجيل دفعة الآن مع حفظ الفاتورة",
        value=False,
        key=f"{key_prefix}_enable",
    )

    if not enable_payment:
        return None

    cash_boxes = db.query(models.CashBox).filter(
        models.CashBox.is_active == True
    ).all()

    if not cash_boxes:
        st.error("⚠️ لا توجد خزائن! أضف خزينة أولاً.")
        return None

    box_opts = {b.id: f"{b.name} ({b.code})" for b in cash_boxes}

    col_a, col_b = st.columns(2)
    with col_a:
        amount = st.number_input(
            "المبلغ المدفوع (ج.م):",
            min_value=0.0,
            max_value=float(max(net_amount, 0.01)),
            value=float(net_amount),
            step=1.0,
            format="%.2f",
            key=f"{key_prefix}_amount",
        )
    with col_b:
        method_label = st.selectbox(
            "طريقة الدفع:",
            options=list(PAYMENT_METHODS.values()),
            key=f"{key_prefix}_method",
        )
        method_key = next(
            (k for k, v in PAYMENT_METHODS.items() if v == method_label),
            "cash",
        )

    col_c, col_d = st.columns(2)
    with col_c:
        box_id = st.selectbox(
            "الخزينة:",
            options=list(box_opts.keys()),
            format_func=lambda x: box_opts[x],
            key=f"{key_prefix}_box",
        )
    with col_d:
        ref_no = st.text_input(
            "رقم المرجع (اختياري):",
            key=f"{key_prefix}_ref",
        )

    notes = st.text_area(
        "ملاحظات (اختياري):",
        key=f"{key_prefix}_notes",
        height=60,
    )

    return {
        "amount": float(amount),
        "method": method_key,
        "cash_box_id": int(box_id),
        "reference": ref_no or None,
        "notes": notes or None,
    }


# ═══════════════════════════════════════════════════════════
#  قسم الدفع للفاتورة المحفوظة
# ═══════════════════════════════════════════════════════════
def render_payment_section(db, invoice):
    net = float(invoice.net_amount or 0)
    current_user_id = get_current_user_id()
    paid = _get_invoice_paid_amount(db, invoice)
    remaining = max(0.0, net - paid)

    st.markdown("---")
    st.markdown("### 💰 سداد الفاتورة")

    col1, col2, col3 = st.columns(3)
    with col1:
        st.metric("إجمالي الفاتورة", f"{net:,.2f} ج.م")
    with col2:
        st.metric("المدفوع", f"{paid:,.2f} ج.م")
    with col3:
        if remaining > 0:
            st.metric("المتبقي", f"{remaining:,.2f} ج.م",
                      delta=f"-{remaining:,.2f}", delta_color="inverse")
        else:
            st.metric("المتبقي", "0.00 ج.م", delta="مسددة", delta_color="off")

    if remaining <= 0:
        st.success("✅ هذه الفاتورة مسددة بالكامل.")
    else:
        st.markdown("#### 💵 تسجيل دفعة جديدة")
        st.caption(f"**المتبقي:** {remaining:,.2f} ج.م — يمكنك دفع المبلغ كاملًا أو على أجزاء.")

        cash_boxes = db.query(models.CashBox).filter(
            models.CashBox.is_active == True
        ).all()

        if not cash_boxes:
            st.error("⚠️ لا توجد خزائن مفعّلة!")
        else:
            box_opts = {b.id: f"{b.name} ({b.code})" for b in cash_boxes}

            with st.form(f"inv_payment_form_{invoice.id}", clear_on_submit=False):
                col_a, col_b = st.columns(2)
                with col_a:
                    amount = st.number_input(
                        "المبلغ المدفوع (ج.م):",
                        min_value=0.01,
                        max_value=float(remaining),
                        value=float(remaining),
                        step=1.0,
                        format="%.2f",
                        key=f"pay_amount_{invoice.id}",
                    )
                with col_b:
                    method_label = st.selectbox(
                        "طريقة الدفع:",
                        options=list(PAYMENT_METHODS.values()),
                        key=f"pay_method_{invoice.id}",
                    )
                    method_key = next(
                        (k for k, v in PAYMENT_METHODS.items() if v == method_label),
                        "cash",
                    )
                col_c, col_d = st.columns(2)
                with col_c:
                    box_id = st.selectbox(
                        "الخزينة:",
                        options=list(box_opts.keys()),
                        format_func=lambda x: box_opts[x],
                        key=f"pay_box_{invoice.id}",
                    )
                with col_d:
                    ref_no = st.text_input(
                        "رقم المرجع (اختياري):",
                        key=f"pay_ref_{invoice.id}",
                    )
                notes = st.text_area(
                    "ملاحظات (اختياري):",
                    key=f"pay_notes_{invoice.id}",
                    height=60,
                )
                submitted = st.form_submit_button(
                    "✅ تسجيل الدفعة",
                    type="primary",
                    use_container_width=True,
                )

            if submitted:
                try:
                    if amount <= 0:
                        st.error("❌ المبلغ يجب أن يكون أكبر من صفر.")
                    elif amount > remaining + 0.01:
                        st.error(f"❌ المبلغ أكبر من المتبقي ({remaining:,.2f}).")
                    else:
                        payment_type = "receipt" if (invoice.type or "sale") == "sale" else "payment"
                        create_payment(
                            party_id=invoice.party_id,
                            amount=float(amount),
                            payment_type=payment_type,
                            payment_method=method_key,
                            reference_number=invoice.invoice_number,
                            notes=notes or None,
                            created_by=current_user_id,
                            cash_box_id=int(box_id),
                        )
                        db.expire_all()
                        inv_fresh = db.query(models.Invoice).filter(
                            models.Invoice.id == invoice.id
                        ).first()
                        if inv_fresh:
                            _update_invoice_status(db, inv_fresh)
                        st.success(f"✅ تم تسجيل دفعة {amount:,.2f} ج.م بنجاح!")
                        st.balloons()
                        clear_form(f"pay_", extra_keys=(
                            f"pay_amount_{invoice.id}",
                            f"pay_method_{invoice.id}",
                            f"pay_box_{invoice.id}",
                            f"pay_ref_{invoice.id}",
                            f"pay_notes_{invoice.id}",
                        ))
                        st.rerun()
                except Exception as e:
                    db.rollback()
                    st.error(f"❌ خطأ: {e}")
                    st.code(traceback.format_exc())

    payments = _get_invoice_payments(db, invoice)
    if payments:
        st.markdown("#### 📋 مدفوعات هذه الفاتورة")
        for p in payments:
            method_ar = PAYMENT_METHODS.get(p.payment_method, p.payment_method or "—")
            col_info, col_del = st.columns([5, 1])
            with col_info:
                st.markdown(
                    f"**{float(p.amount or 0):,.2f} ج.م** — {method_ar} — "
                    f"{p.date.strftime('%Y-%m-%d %H:%M') if p.date else '—'}"
                    + (f" — مرجع: {p.reference_number}" if p.reference_number else "")
                    + (f" — {p.notes}" if p.notes else "")
                )
            with col_del:
                if st.button("🗑 حذف", key=f"del_pay_{p.id}", use_container_width=True):
                    if _delete_payment(db, p.id):
                        db.expire_all()
                        inv_fresh = db.query(models.Invoice).filter(
                            models.Invoice.id == invoice.id
                        ).first()
                        if inv_fresh:
                            _update_invoice_status(db, inv_fresh)
                        st.success("✅ تم حذف الدفعة.")
                        st.rerun()
                    else:
                        st.error("❌ فشل حذف الدفعة.")


# ═══════════════════════════════════════════════════════════
#  عرض الفاتورة المحفوظة
# ═══════════════════════════════════════════════════════════
def show_saved_invoice(db, inv_no, inv_text, next_no, no_key, fs, invoice_type, dt_date_key, dt_time_key, kb=None):
    try:
        invoice = (db.query(models.Invoice)
                   .filter(models.Invoice.invoice_number == inv_text).first())
        if invoice is None:
            st.warning("تعذّر العثور على الفاتورة.")
            return

        status_txt, status_color = _status_badge(invoice.status)
        col_t, col_s = st.columns([3, 1])
        with col_t:
            st.subheader(f"📄 فاتورة رقم {inv_no}  ({inv_text})")
        with col_s:
            st.markdown(
                f'<div style="text-align:center;padding:8px;border-radius:8px;'
                f'background:{status_color}22;border:1px solid {status_color};'
                f'color:{status_color};font-weight:700;">{status_txt}</div>',
                unsafe_allow_html=True,
            )

        facts = {}
        if invoice.date:
            facts["📅 التاريخ"] = invoice.date.strftime("%Y-%m-%d")
            facts["🕐 الوقت"] = invoice.date.strftime("%H:%M:%S")
        for attr, title in (("invoice_type", "النوع"), ("created_at", "تاريخ الإنشاء")):
            v = getattr(invoice, attr, None)
            if v is not None:
                facts[title] = str(getattr(v, "value", v))[:19]
        pid = getattr(invoice, "party_id", None)
        if pid is not None:
            p = db.query(models.Party).filter(models.Party.id == pid).first()
            if p:
                label = "العميل" if invoice_type == "sale" else "المورد"
                facts[label] = p.name
        for attr, title in (("total_amount", "الإجمالي"),
                            ("discount_amount", "الخصم"),
                            ("tax_amount", "الضريبة"),
                            ("net_amount", "الصافي")):
            v = getattr(invoice, attr, None)
            if v is not None:
                facts[title] = f"{float(v):,.2f}"

        if facts:
            cols = st.columns(min(len(facts), 4))
            for i, (k, v) in enumerate(facts.items()):
                cols[i % len(cols)].metric(k, v)

        lines = invoice_lines(db, invoice)
        if lines is None:
            st.warning("تعذّر التعرف على أصناف هذه الفاتورة.")
        elif not lines:
            st.info("لا توجد أصناف في هذه الفاتورة.")
        else:
            st.markdown("**أصناف الفاتورة:**")
            st.dataframe(pd.DataFrame(lines), use_container_width=True, hide_index=True)

        render_payment_section(db, invoice)

        st.markdown("---")
        st.markdown("### ⚙️ إدارة الفاتورة")
        col_edit, col_del = st.columns(2)

        with col_edit:
            if st.button("✏️ تحميل للتعديل", type="primary", use_container_width=True,
                         key=f"edit_inv_{invoice.id}"):
                try:
                    if invoice.date:
                        check_period_open(invoice.date, entity="تعديل فاتورة")
                    data = _load_invoice_data(db, invoice)
                    _prepare_invoice_for_edit(data, fs, no_key, dt_date_key, dt_time_key)
                    st.session_state["_inv_edit_flash"] = (
                        f"✏️ وضع التعديل: فاتورة {invoice.invoice_number}."
                    )
                    st.rerun()
                except Exception as e:
                    db.rollback()
                    st.error(f"❌ تعذّر تحميل الفاتورة للتعديل: {e}")
                    st.code(traceback.format_exc())

        with col_del:
            confirm_key = f"confirm_del_inv_{invoice.id}"
            confirm = st.checkbox("تأكيد الحذف", key=confirm_key)
            if st.button("🗑 حذف الفاتورة بالكامل", type="secondary",
                         use_container_width=True, disabled=not confirm,
                         key=f"del_inv_{invoice.id}"):
                try:
                    deleted_no = invoice.invoice_number
                    _delete_invoice_fully(db, invoice)
                    st.session_state["_inv_flash"] = f"✅ تم حذف الفاتورة {deleted_no} بالكامل."
                    queue_state_updates(
                        delete_keys=("inv_items_editor",),
                        set_values={no_key: next_no, "invoice_items": []},
                    )
                    clear_form("inv_")
                    st.rerun()
                except Exception as e:
                    db.rollback()
                    st.error(f"❌ تعذّر حذف الفاتورة: {e}")
                    st.code(traceback.format_exc())

        st.markdown("---")
        st.button("➕ فاتورة جديدة", on_click=_go_new, args=(no_key, next_no))

    except Exception as e:
        st.error(f"❌ خطأ في عرض الفاتورة: {e}")
        st.code(traceback.format_exc())


# ═══════════════════════════════════════════════════════════
#  الصفحة الرئيسية
# ═══════════════════════════════════════════════════════════




# st.fragment helper — إعادة رسم جزئية (أسرع 5-10x)
def _fragment(fn):
    try:
        return st.fragment(fn)
    except Exception:
        return fn

def _render_invoice_index(invoice_type):
    """فهرس فواتير حسب النوع — يقرأ من DB مباشرة بدون كاش."""
    import pandas as pd
    from database import SessionLocal
    from sqlalchemy import func
    
    db = SessionLocal()
    try:
        q = db.query(models.Invoice).filter(
            models.Invoice.type == invoice_type
        ).order_by(models.Invoice.id.desc())
        
        all_inv = q.limit(500).all()
        
        if not all_inv:
            st.info("لا توجد فواتير من هذا النوع.")
            return
        
        # خرائط
        party_ids = list({r.party_id for r in all_inv if r.party_id})
        party_map = {p.id: (p.name or "") for p in db.query(models.Party).filter(
            models.Party.id.in_(party_ids)
        ).all()} if party_ids else {}
        
        user_ids = list({r.created_by for r in all_inv if r.created_by})
        user_map = {u.id: (u.full_name or u.username or "") for u in db.query(models.User).filter(
            models.User.id.in_(user_ids)
        ).all()} if user_ids else {}
        
        inv_nums = [r.invoice_number for r in all_inv if r.invoice_number]
        pay_sums = {}
        if inv_nums:
            rows_pay = db.query(
                models.Payment.reference_number,
                func.sum(models.Payment.amount).label("t"),
            ).filter(
                models.Payment.reference_number.in_(inv_nums)
            ).group_by(models.Payment.reference_number).all()
            pay_sums = {r[0]: float(r[1] or 0) for r in rows_pay if r[0]}
        
        data = []
        for r in all_inv:
            net = float(r.net_amount or 0)
            paid = pay_sums.get(r.invoice_number, 0.0)
            data.append({
                "المسلسل": r.invoice_number,
                "ID": r.id,
                "التاريخ": r.date.strftime("%Y-%m-%d %H:%M") if r.date else "-",
                "العميل/المورد": party_map.get(r.party_id, "—"),
                "المبلغ": f"{net:,.2f}",
                "المدفوع": f"{paid:,.2f}",
                "المتبقي": f"{max(0, net-paid):,.2f}",
                "الحالة": r.status or "pending",
                "أنشأها": user_map.get(r.created_by, "النظام"),
            })
        
        df = pd.DataFrame(data)
        st.dataframe(df.drop(columns=["ID"]), use_container_width=True, hide_index=True)
        st.caption(f"إجمالي: {len(data)} فاتورة")
        
        st.markdown("---")
        st.subheader("عرض التفاصيل")
        
        inv_ids = [r.id for r in all_inv]
        inv_nums_map = {r.id: r.invoice_number for r in all_inv}
        
        sel_id = st.selectbox(
            "اختر فاتورة:",
            options=inv_ids,
            format_func=lambda x: inv_nums_map.get(x, str(x)),
            key=f"idx_{invoice_type}_sel"
        )
        
        if sel_id:
            inv = db.query(models.Invoice).filter(models.Invoice.id == sel_id).first()
            if inv is None:
                st.warning("الفاتورة غير موجودة")
                return
            
            party = db.query(models.Party).filter(models.Party.id == inv.party_id).first() if inv.party_id else None
            lines = db.query(models.InvoiceLine).filter(models.InvoiceLine.invoice_id == inv.id).all()
            
            col_a, col_b, col_c = st.columns(3)
            with col_a:
                st.write(f"**رقم:** {inv.invoice_number}")
                st.write(f"**التاريخ:** {inv.date.strftime('%Y-%m-%d %H:%M') if inv.date else '-'}")
            with col_b:
                st.write(f"**العميل:** {party.name if party else 'غير محدد'}")
                st.write(f"**الإجمالي:** {float(inv.net_amount or 0):,.2f}")
            with col_c:
                st.write(f"**الحالة:** {inv.status}")
                st.write(f"**النوع:** {'بيع' if inv.type == 'sale' else 'شراء'}")
            
            if lines:
                item_ids = [l.item_id for l in lines]
                items_map = {it.id: it.name for it in db.query(models.Item).filter(
                    models.Item.id.in_(item_ids)
                ).all()}
                
                st.markdown("**الأصناف:**")
                ld = [{"الصنف": items_map.get(l.item_id, "؟"),
                       "الكمية": l.quantity,
                       "السعر": f"{float(l.price or 0):,.2f}",
                       "الإجمالي": f"{float(l.total or 0):,.2f}"} for l in lines]
                st.dataframe(pd.DataFrame(ld), use_container_width=True, hide_index=True)
    finally:
        db.close()


def render_invoice_page(invoice_type: str):
    """invoice_type: 'sale' or 'purchase'"""
    assert invoice_type in ("sale", "purchase")

    is_sale = invoice_type == "sale"
    type_ar = "بيع" if is_sale else "شراء"
    type_label = "🛒 فواتير البيع" if is_sale else "🛍️ فواتير الشراء"
    party_label = "العميل" if is_sale else "المورد"
    party_select_label = "اختر العميل:" if is_sale else "اختر المورد:"

    render_sidebar()

    try:
        from shortcuts import install_shortcuts, show_shortcuts_guide
        show_shortcuts_guide()
        kb = install_shortcuts()
    except Exception:
        kb = {"new": False, "save": False, "clear": False, "delete": False}

    current_user = require_login()
    current_user_id = get_current_user_id()
    current_user_name = get_current_user_name()

    PFX = f"inv_{invoice_type}_"

    fs = FormState(PFX, cart_keys=("invoice_items",))
    no_key = fs.key("inv_no")
    dt_date_key = fs.key("inv_date")
    dt_time_key = fs.key("inv_time")

    st.title(type_label)
    
    # ⭐ عداد أصناف الفاتورة
    try:
        from invoice_cart_badge import render_cart_badge
        render_cart_badge(invoice_type)
    except Exception:
        pass
    _mode = st.radio("", ["➕ فاتورة جديدة", "📋 فهرس الفواتير"], horizontal=True, label_visibility="collapsed", key=f"mode_{invoice_type}")
    st.markdown("---")
    if "فهرس" in _mode:
        _render_invoice_index(invoice_type)
        return
    show_clear_hint()
    st.info(f"👤 المستخدم: **{current_user_name}**")

    flash = st.session_state.pop("_inv_flash", None)
    if flash:
        st.success(flash)
        st.balloons()

    db = SessionLocal()

    parties = db.query(models.Party).filter(models.Party.type == ("customer" if is_sale else "supplier")).all()
    items = db.query(models.Item).all()

    if not parties:
        st.error(f"⚠️ لا يوجد {party_label}ون! أضف {party_label} أولاً.")
        db.close()
        st.stop()

    if not items:
        st.error("⚠️ لا توجد أصناف!")
        db.close()
        st.stop()

    used_numbers = used_invoice_numbers(db, invoice_type=invoice_type)
    next_no = (max(used_numbers) + 1) if used_numbers else 1

    editing_id = st.session_state.get("_editing_invoice_id")
    is_editing = editing_id is not None

    edit_flash = st.session_state.pop("_inv_edit_flash", None)
    if edit_flash:
        st.warning(edit_flash)

    if is_editing:
        editing_no = st.session_state.get("_editing_invoice_no", "?")
        col_banner, col_cancel = st.columns([4, 1])
        with col_banner:
            st.info(f"🔴 **وضع التعديل** — أنت تعدّل الفاتورة **{editing_no}**.")
        with col_cancel:
            if st.button("❌ إلغاء التعديل", use_container_width=True, key=f"cancel_edit_btn_{invoice_type}"):
                _clear_edit_state()
                queue_state_updates(
                    delete_keys=("inv_items_editor",),
                    set_values={no_key: next_no, "invoice_items": []},
                )
                clear_form(PFX)
                st.rerun()

    if kb["new"]:
        _clear_edit_state()
        queue_state_updates(
            delete_keys=("inv_items_editor",),
            set_values={no_key: next_no, "invoice_items": []},
        )
        clear_form(PFX)
        st.rerun()

    if kb["clear"]:
        queue_state_updates(
            delete_keys=("inv_items_editor",),
            set_values={"invoice_items": []},
        )
        clear_form(PFX)
        st.rerun()

    # ═══════════════════════════════════════════════════
    #  التاريخ + الوقت + رقم الفاتورة
    # ═══════════════════════════════════════════════════
    st.markdown("### 📅 بيانات الفاتورة")

    if dt_date_key not in st.session_state:
        st.session_state[dt_date_key] = date.today()
    if dt_time_key not in st.session_state:
        st.session_state[dt_time_key] = datetime.now().time().replace(second=0, microsecond=0)
    if no_key not in st.session_state:
        st.session_state[no_key] = next_no

    col_dt, col_tm, col_no, col_state = st.columns([1.2, 1, 1, 2])

    with col_dt:
        sel_date = st.date_input("📅 تاريخ الفاتورة:", key=dt_date_key)
    with col_tm:
        sel_time = st.time_input("🕐 وقت الفاتورة:", key=dt_time_key)
    with col_no:
        inv_no = int(st.number_input("🔢 رقم الفاتورة:", min_value=1, step=1, key=no_key))

    inv_datetime = datetime.combine(sel_date, sel_time)

    is_existing = inv_no in used_numbers
    with col_state:
        st.write("")
        if is_existing and not is_editing:
            st.info(f"📂 الفاتورة رقم {inv_no} محفوظة سابقاً.")
        elif is_editing:
            st.caption(f"✏️ في وضع التعديل — {inv_datetime.strftime('%Y-%m-%d %H:%M')}")
        else:
            st.caption(f"✅ الرقم {inv_no} متاح — {inv_datetime.strftime('%Y-%m-%d %H:%M')}")

    if used_numbers:
        st.caption(
            f"📊 الأرقام المستخدمة: **{min(used_numbers)}** → **{max(used_numbers)}** "
            f"({len(used_numbers)} فاتورة). المسلسل القادم المقترح: **{next_no}**."
        )

    if is_existing and not is_editing:
        show_saved_invoice(
            db, inv_no, used_numbers[inv_no], next_no,
            no_key, fs, invoice_type, dt_date_key, dt_time_key, kb=kb
        )
        db.close()
        st.stop()

    # ═══════════════════════════════════════════════════
    #  اختيار الطرف
    # ═══════════════════════════════════════════════════
    party_dict = {p.id: p.name for p in parties}

    selected_party_id = st.selectbox(
        party_select_label,
        options=list(party_dict.keys()),
        format_func=lambda x: party_dict[x],
        key=fs.key(f"party_{invoice_type}"),
    )

    if "invoice_items" not in st.session_state:
        st.session_state["invoice_items"] = []

    st.markdown("---")
    st.subheader("أصناف الفاتورة")

    cs = CodeSearch(
        items,
        prefix=fs.prefix(),
        code_field=CODE_FIELDS,
        name_field="name",
        price_field=(lambda it: it.sell_price or 0.0) if is_sale
                    else (lambda it: it.cost_price or 0.0),
        qty_key=fs.key("qty"),
    )

    PLACEHOLDER = "— اختر صنفاً —"

    if st.session_state.pop(f"{PFX}clear_item_pending", False):
        st.session_state[cs.query_key] = ""
        st.session_state[cs.item_key] = PLACEHOLDER
        st.session_state[cs.price_key] = 0.0
        st.session_state[fs.key("qty")] = 1

    cs.render_input()

    col1, col2, col3 = st.columns([2, 1, 1])
    with col1:
        options = [PLACEHOLDER] + list(cs.labels)
        sel_label = st.selectbox("الصنف:", options, key=cs.item_key)

        # ⭐ تلميح المخزون: سطر خافت اللون أسفل الصنف المختار يعرض الكمية المتاحة
        #    • يظهر عند اختيار الصنف
        #    • يختفي عند العودة إلى "— اختر صنفاً —" (بعد الضغط على الإضافة أو التفريغ)
        #    • وعند اختيار صنف آخر يُستبدل بتلميح الصنف الجديد
        if sel_label != PLACEHOLDER:
            _hint_item = cs.by_label(sel_label)
            _hint_html = None
            if _hint_item is not None:
                if getattr(_hint_item, "is_kit", False):
                    _hint_html = "🧩 صنف مدمج — لا يُخصم من المخزون مباشرة"
                else:
                    try:
                        _avail = float(get_stock_map().get(_hint_item.id, 0.0))
                        _avail_txt = f"{_avail:,.2f}".rstrip("0").rstrip(".")
                        _warn = " ⚠️ عند/تحت الحد الأدنى" if _avail <= float(getattr(_hint_item, "min_stock", 0) or 0) else ""
                        _hint_html = f"📦 المتاح بالمخزون: {_avail_txt} وحدة{_warn}"
                    except Exception:
                        _hint_html = None
            if _hint_html:
                st.markdown(
                    f'<div style="opacity:0.55;font-size:0.85em;'
                    f'margin-top:-8px;margin-bottom:6px;">{_hint_html}</div>',
                    unsafe_allow_html=True,
                )

    if sel_label == PLACEHOLDER:
        current_item = None
    else:
        cs.sync_price(sel_label, invoice_type)
        current_item = cs.by_label(sel_label)

    with col2:
        qty = qty_input("الكمية:", key=f"{PFX}qty_1")
    with col3:
        price = price_input("السعر:", key=f"{PFX}price_1")

    if st.button("➕ إضافة للفاتورة"):
        if sel_label == PLACEHOLDER:
            st.error("⚠️ يرجى اختيار صنف أولاً")
        elif current_item is not None and qty > 0 and price > 0:
            st.session_state["invoice_items"].append({
                "item_name": current_item.name,
                "quantity": qty,
                "price": price,
            })
            st.toast(f"✅ تمت إضافة {current_item.name}")
            st.session_state[f"{PFX}clear_item_pending"] = True
            st.rerun()
        else:
            st.error("يرجى اختيار صنف وإدخال كمية وسعر صحيحين")

    save_clicked = False

    if st.session_state["invoice_items"]:
        st.markdown("---")
        st.subheader("تفاصيل الفاتورة")

        df_edit = pd.DataFrame([
            {
                "الصنف": it["item_name"],
                "الكمية": float(it["quantity"]),
                "السعر": float(it["price"]),
                "الإجمالي": float(it["quantity"]) * float(it["price"]),
            }
            for it in st.session_state["invoice_items"]
        ])

        edited_df = st.data_editor(
            df_edit,
            num_rows="dynamic",
            use_container_width=True,
            key="inv_items_editor",
            column_config={
                "الصنف": st.column_config.TextColumn("الصنف", required=True, width="large"),
                "الكمية": st.column_config.NumberColumn("الكمية", min_value=0.0, step=1.0, format="%.2f"),
                "السعر": st.column_config.NumberColumn("السعر", min_value=0.0, step=0.1, format="%.2f"),
                "الإجمالي": st.column_config.NumberColumn("الإجمالي", disabled=True, format="%.2f"),
            },
        )

        new_items = []
        for _, row in edited_df.iterrows():
            name = str(row["الصنف"]).strip() if pd.notna(row["الصنف"]) else ""
            q = float(row["الكمية"]) if pd.notna(row["الكمية"]) else 0.0
            p = float(row["السعر"]) if pd.notna(row["السعر"]) else 0.0
            if name and q > 0:
                new_items.append({"item_name": name, "quantity": q, "price": p})

        st.session_state["invoice_items"] = new_items

        if not edited_df.empty:
            _q = edited_df["الكمية"].fillna(0).astype(float)
            _p = edited_df["السعر"].fillna(0).astype(float)
            subtotal = float((_q * _p).sum())
        else:
            subtotal = 0.0

        col_disc, col_tax = st.columns(2)
        with col_disc:
            discount_pct = st.number_input("خصم %:", min_value=0.0, max_value=100.0,
                                           value=0.0, step=1.0, key=fs.key("discount"))
        with col_tax:
            tax_pct = st.number_input("ضريبة %:", min_value=0.0, max_value=100.0,
                                      value=0.0, step=1.0, key=fs.key("tax"))

        discount_amount = subtotal * (discount_pct / 100)
        amount_after_discount = subtotal - discount_amount
        tax_amount = amount_after_discount * (tax_pct / 100)
        net_amount = amount_after_discount + tax_amount

        col1, col2, col3, col4 = st.columns(4)
        with col1:
            st.metric("المجموع", f"{subtotal:,.2f}")
        with col2:
            st.metric("الخصم", f"{discount_amount:,.2f}")
        with col3:
            st.metric("الضريبة", f"{tax_amount:,.2f}")
        with col4:
            st.metric("الصافي", f"{net_amount:,.2f}")

        payment_data = render_payment_section_for_new(db, net_amount, key_prefix=f"{PFX}new_{inv_no}")

        st.markdown("---")
        col_save, col_clear = st.columns([2, 1])
        with col_save:
            save_label = "💾 حفظ التعديلات" if is_editing else "💾 حفظ الفاتورة"
            if st.button(save_label, type="primary", use_container_width=True, key=f"{PFX}save_btn"):
                save_clicked = True
        with col_clear:
            if st.button("🗑 تفريغ السلة", use_container_width=True, key=f"{PFX}clear_btn"):
                queue_state_updates(
                    delete_keys=("inv_items_editor",),
                    set_values={"invoice_items": []},
                )
                clear_form(PFX, f"{PFX}new_")
                st.rerun()

        if kb["save"]:
            save_clicked = True

        if save_clicked:
            can_save = True
            current_num_str = format_invoice_number(inv_no, invoice_type)

            if is_editing:
                existing = db.query(models.Invoice).filter(
                    models.Invoice.invoice_number == current_num_str
                ).first()
                if existing is not None and existing.id != editing_id:
                    st.error(f"❌ الرقم {inv_no} مستخدم في فاتورة أخرى.")
                    can_save = False
            else:
                if inv_no in used_numbers:
                    st.error(f"❌ الرقم {inv_no} مستخدم بالفعل.")
                    can_save = False

            if not selected_party_id:
                st.error(f"يرجى اختيار {party_label}")
                can_save = False
            elif not st.session_state["invoice_items"]:
                st.error("يرجى إضافة أصناف للفاتورة")
                can_save = False

            if can_save:
                saved_msg = None
                try:
                    check_period_open(inv_datetime, entity="حفظ فاتورة")

                    names = {ln["item_name"] for ln in st.session_state["invoice_items"]}
                    changed = False
                    for it in db.query(models.Item).filter(models.Item.name.in_(names)).all():
                        changed = zero_null_numbers(it) or changed
                    party_obj = db.query(models.Party).filter(models.Party.id == selected_party_id).first()
                    if party_obj is not None:
                        changed = zero_null_numbers(party_obj) or changed
                    if changed:
                        db.commit()

                    if is_editing:
                        old = db.query(models.Invoice).filter(models.Invoice.id == editing_id).first()
                        if old is not None:
                            _delete_invoice_fully(db, old)

                    invoice = create_invoice(
                        party_id=selected_party_id,
                        invoice_type=invoice_type,
                        items=st.session_state["invoice_items"],
                        invoice_number=current_num_str,
                        invoice_date=inv_datetime,
                        discount_percentage=discount_pct,
                        tax_rate=tax_pct,
                        created_by=current_user_id,
                    )

                    if payment_data and payment_data.get("amount", 0) > 0:
                        try:
                            payment_type = "receipt" if is_sale else "payment"
                            create_payment(
                                party_id=selected_party_id,
                                amount=float(payment_data["amount"]),
                                payment_type=payment_type,
                                payment_method=payment_data["method"],
                                reference_number=current_num_str,
                                notes=payment_data.get("notes"),
                                created_by=current_user_id,
                                cash_box_id=int(payment_data["cash_box_id"]),
                            )
                            db.expire_all()
                            inv_fresh = db.query(models.Invoice).filter(
                                models.Invoice.id == invoice.id
                            ).first()
                            if inv_fresh:
                                _update_invoice_status(db, inv_fresh)
                        except Exception as pay_err:
                            st.warning(f"⚠️ تم حفظ الفاتورة، لكن فشل تسجيل الدفعة: {pay_err}")
                    else:
                        invoice.status = "pending"
                        db.commit()

                    action_word = "تعديل" if is_editing else "حفظ"
                    saved_msg = f"✅ تم {action_word} فاتورة {type_ar} رقم {current_num_str} بنجاح!"
                    _clear_edit_state()
                except Exception as e:
                    db.rollback()
                    st.error(f"❌ خطأ: {e}")
                    st.code(traceback.format_exc())

                if saved_msg:
                    st.session_state["_inv_flash"] = saved_msg
                    queue_state_updates(
                        delete_keys=("inv_items_editor",),
                        set_values={"invoice_items": []},
                    )
                    clear_form(PFX, f"{PFX}new_")
                    db.close()
                    fs.reset()
                    st.rerun()
    else:
        st.info("🛒 السلة فارغة.")

    db.close()
