"""
navigation_helper.py — موحّد لاستقبال التنقلات من شجرة الحسابات
يُستدعى في أعلى كل صفحة مستقبِلة.
"""

import streamlit as st


def consume_navigation_flags():
    """
    يقرأ ويُفرّغ كل مفاتيح التنقل من session_state.
    يُرجع dict بكل القيم المستلمة.
    """
    keys = [
        # أزرار الانتقال من شجرة الحسابات
        "open_expense_category_id",
        "open_expense_tab",
        "filter_expense_by_account",
        "journal_filter_account",
        "filter_by_account",
        "filter_party_id",
        "filter_account_id",
        # مفاتيح فتح سجل محدد
        "_open_expenses_id",
        "_open_cash_boxes_id",
        "_open_items_id",
        "_open_parties_id",
        "_open_invoices_id",
        "_open_payments_id",
        "_open_fixed_assets_id",
        "_open_loans_id",
        "_open_cost_centers_id",
        # علامات المصدر
        "_from_accounts_to_expenses",
        "_from_accounts_to_cash_boxes",
        "_from_accounts_to_items",
        "_from_accounts_to_parties",
        "_from_accounts_to_invoices",
        "_from_accounts_to_payments",
        "_from_accounts_to_fixed_assets",
        "_from_accounts_to_loans",
        "_from_accounts_to_cost_centers",
    ]

    received = {}
    for k in keys:
        if k in st.session_state:
            received[k] = st.session_state.pop(k)

    return received


def show_navigation_banner(received, page_name="هذه الصفحة"):
    """
    يعرض شريط إشعار إذا جاء المستخدم من شجرة الحسابات.
    """
    source_keys = [k for k in received.keys() if k.startswith("_from_accounts_")]
    if not source_keys:
        return False

    # تحقق من وجود بيانات فعلية
    has_data = any(
        received.get(k)
        for k in received.keys()
        if not k.startswith("_from_accounts_")
    )

    if has_data:
        with st.container(border=True):
            col_a, col_b = st.columns([5, 1])
            with col_a:
                st.info(
                    f"🎯 **تم استدعاؤك من شجرة الحسابات** إلى {page_name} — "
                    f"الفلاتر مطبّقة تلقائياً."
                )
            with col_b:
                if st.button("🗑️ مسح الفلتر", key=f"clear_nav_{page_name}", use_container_width=True):
                    for k in list(received.keys()):
                        st.session_state.pop(k, None)
                    st.rerun()
        return True

    return False


def get_filter(key, default=None):
    """اختصار لقراءة فلتر من session_state (بدون حذف)."""
    return st.session_state.get(key, default)


def clear_filter(key):
    """حذف فلتر معين."""
    st.session_state.pop(key, None)


def clear_all_filters():
    """حذف كل فلاتر التنقل."""
    for k in list(st.session_state.keys()):
        if k.startswith("_from_accounts_") or k.startswith("_open_") or \
           k.startswith("open_") or k.startswith("filter_"):
            st.session_state.pop(k, None)