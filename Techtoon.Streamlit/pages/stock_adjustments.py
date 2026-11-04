import streamlit as st
import database as db
import pandas as pd
from datetime import datetime

def show():
    st.title("⚖️ تسويات المخزون")
    try:
        _, rows = db.execute_query("SELECT a.id, a.document_number, a.adjustment_date, i.name, w.name, a.old_quantity, a.new_quantity, a.difference, sr.name, a.status FROM stock_adjustments a LEFT JOIN items i ON a.item_id=i.id LEFT JOIN warehouses w ON a.warehouse_id=w.id LEFT JOIN stock_reasons sr ON a.reason=sr.code ORDER BY a.id DESC", fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "رقم", "التاريخ", "الصنف", "المخزن", "قبل", "بعد", "الفرق", "السبب", "الحالة"])
            st.dataframe(df, use_container_width=True, hide_index=True)
        else: st.info("لا توجد تسويات")
    except Exception as e: st.error(f"خطأ: {e}")

    st.markdown("---")
    with st.form("new_adj"):
        st.markdown("### ➕ تسوية جديدة")
        _, items = db.execute_query("SELECT id, name FROM items WHERE is_active=TRUE ORDER BY name", fetch=True)
        i_opts = {i[1]: i[0] for i in items} if items else {}
        _, whs = db.execute_query("SELECT id, name FROM warehouses WHERE is_active=TRUE ORDER BY name", fetch=True)
        w_opts = {w[1]: w[0] for w in whs} if whs else {}
        _, rsns = db.execute_query("SELECT code, name FROM stock_reasons WHERE reason_type='adjustment' AND is_active=TRUE ORDER BY name", fetch=True)
        r_opts = {r[1]: r[0] for r in rsns} if rsns else {}
        col1, col2 = st.columns(2)
        i_name = col1.selectbox("الصنف *", list(i_opts.keys()) if i_opts else [])
        r_name = col2.selectbox("السبب *", list(r_opts.keys()) if r_opts else [])
        col1, col2 = st.columns(2)
        w_name = col1.selectbox("المخزن", list(w_opts.keys()) if w_opts else [])
        nq = col2.number_input("الرصيد الجديد *", 0.0, 999999.0, 0.0)
        notes = st.text_area("ملاحظات")
        if st.form_submit_button("💾 حفظ"):
            if i_opts and r_opts:
                doc = f"ADJ-{datetime.now().strftime('%Y%m%d%H%M%S')}"
                db.execute_query("INSERT INTO stock_adjustments (document_number, item_id, warehouse_id, adjustment_date, old_quantity, new_quantity, difference, reason, notes, created_by, created_at, status, reference_document_type) VALUES (%s, %s, %s, NOW(), 0, %s, %s, %s, %s, 1, NOW(), 'draft', 'manual')", (doc, i_opts[i_name], w_opts.get(w_name, 1), nq, nq, r_opts[r_name], notes or None))
                st.success(f"✅ تم الحفظ: {doc}"); st.rerun()