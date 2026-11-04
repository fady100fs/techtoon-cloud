import streamlit as st
import database as db
import pandas as pd
from datetime import datetime

def show():
    st.title("🗑️ طلبات الشطب")
    try:
        _, rows = db.execute_query("SELECT w.id, w.document_number, w.date, i.name, w.quantity, w.total_value, w.proposed_action, w.status FROM writeoff_requests w LEFT JOIN items i ON w.item_id=i.id ORDER BY w.id DESC", fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "رقم", "التاريخ", "الصنف", "الكمية", "القيمة", "الإجراء", "الحالة"])
            st.dataframe(df, use_container_width=True, hide_index=True)
        else: st.info("لا توجد طلبات")
    except Exception as e: st.error(f"خطأ: {e}")

    st.markdown("---")
    with st.form("new_wo"):
        st.markdown("### ➕ طلب شطب")
        _, items = db.execute_query("SELECT id, name FROM items WHERE is_active=TRUE ORDER BY name", fetch=True)
        i_opts = {i[1]: i[0] for i in items} if items else {}
        _, whs = db.execute_query("SELECT id, name FROM warehouses WHERE is_active=TRUE ORDER BY name", fetch=True)
        w_opts = {w[1]: w[0] for w in whs} if whs else {}
        _, rsns = db.execute_query("SELECT id, name FROM stock_reasons WHERE reason_type='writeoff' AND is_active=TRUE ORDER BY name", fetch=True)
        r_opts = {r[1]: r[0] for r in rsns} if rsns else {}
        col1, col2 = st.columns(2)
        i_name = col1.selectbox("الصنف *", list(i_opts.keys()) if i_opts else [])
        r_name = col2.selectbox("السبب *", list(r_opts.keys()) if r_opts else [])
        col1, col2 = st.columns(2)
        w_name = col1.selectbox("المخزن *", list(w_opts.keys()) if w_opts else [])
        qty = col2.number_input("الكمية *", 0.01, 999999.0, 0.01)
        action = st.selectbox("الإجراء", ["writeoff - شطب", "sell_as_is - بيع", "return_supplier - إرجاع"])
        notes = st.text_area("ملاحظات")
        if st.form_submit_button("💾 حفظ"):
            if i_opts and w_opts and r_opts:
                doc = f"WO-{datetime.now().strftime('%Y%m%d%H%M%S')}"
                db.execute_query("INSERT INTO writeoff_requests (document_number, date, item_id, warehouse_id, quantity, writeoff_reason_id, proposed_action, notes, status, requested_by, requested_at) VALUES (%s, NOW(), %s, %s, %s, %s, %s, %s, 'draft', 1, NOW())", (doc, i_opts[i_name], w_opts[w_name], qty, r_opts[r_name], action.split(" - ")[0], notes or None))
                st.success(f"✅ تم الحفظ: {doc}"); st.rerun()