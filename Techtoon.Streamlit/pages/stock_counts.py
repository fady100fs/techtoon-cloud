import streamlit as st
import database as db
import pandas as pd
from datetime import datetime

def show():
    st.title("📋 جلسات الجرد")
    try:
        _, rows = db.execute_query("SELECT s.id, s.document_number, s.session_date, w.name, s.count_type, s.status, COALESCE((SELECT COUNT(*) FROM stock_counts WHERE session_id=s.id),0), COALESCE((SELECT COUNT(*) FROM stock_counts WHERE session_id=s.id AND counted_quantity IS NOT NULL),0), COALESCE((SELECT SUM(difference_value) FROM stock_counts WHERE session_id=s.id),0) FROM stock_count_sessions s LEFT JOIN warehouses w ON s.warehouse_id=w.id ORDER BY s.id DESC", fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "رقم", "التاريخ", "المخزن", "النوع", "الحالة", "الأصناف", "العديد", "قيمة الفروق"])
            st.dataframe(df, use_container_width=True, hide_index=True)
        else: st.info("لا توجد جلسات")
    except Exception as e: st.error(f"خطأ: {e}")

    st.markdown("---")
    with st.form("new_count"):
        st.markdown("### ➕ جلسة جديدة")
        _, whs = db.execute_query("SELECT id, name FROM warehouses WHERE is_active=TRUE ORDER BY name", fetch=True)
        wh_opts = {w[1]: w[0] for w in whs} if whs else {}
        col1, col2 = st.columns(2)
        wh_name = col1.selectbox("المخزن *", list(wh_opts.keys()) if wh_opts else ["لا يوجد"])
        ct = col2.selectbox("النوع", ["full - شامل", "partial - جزئي", "surprise - مفاجئ"])
        hide = st.checkbox("إخفاء الرصيد الدفتري")
        notes = st.text_area("ملاحظات")
        if st.form_submit_button("💾 إنشاء"):
            if wh_opts:
                c_code = ct.split(" - ")[0]; w_id = wh_opts[wh_name]; doc = f"CNT-{datetime.now().strftime('%Y%m%d%H%M%S')}"
                db.execute_query("INSERT INTO stock_count_sessions (document_number, session_date, warehouse_id, count_type, status, book_balance_locked, notes, created_by, created_at) VALUES (%s, NOW(), %s, %s, 'draft', %s, %s, 1, NOW())", (doc, w_id, c_code, hide, notes or None))
                st.success(f"✅ تم إنشاء: {doc}"); st.rerun()