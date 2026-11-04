import streamlit as st
import database as db
import pandas as pd

def show():
    st.title("👥 العملاء والموردين")
    search = st.text_input("🔍 بحث", placeholder="الاسم")
    query = "SELECT id, name, type, phone, credit_limit, opening_balance FROM parties WHERE is_active=TRUE"
    params = []
    if search:
        query += " AND name ILIKE %s"
        params.append(f"%{search}%")
    query += " ORDER BY name LIMIT 200"
    try:
        cols, rows = db.execute_query(query, tuple(params), fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "الاسم", "النوع", "الهاتف", "حد الائتمان", "الرصيد"])
            st.dataframe(df, use_container_width=True, hide_index=True)
            selected_id = st.selectbox("اختر", [r[0] for r in rows], format_func=lambda x: f"#{x} - {[r[1] for r in rows if r[0]==x][0]}")
            if st.button("🗑️ تعطيل", type="secondary"):
                db.execute_query("UPDATE parties SET is_active=FALSE WHERE id=%s", (selected_id,))
                st.success("✅ تم التعطيل"); st.rerun()
        else: st.info("لا يوجد عملاء")
    except Exception as e: st.error(f"خطأ: {e}")

    st.markdown("---")
    with st.form("new_party"):
        st.markdown("### ➕ عميل/مورد جديد")
        name = st.text_input("الاسم *")
        col1, col2 = st.columns(2)
        ptype = col1.selectbox("النوع", ["customer", "supplier", "both"])
        phone = col2.text_input("الهاتف")
        col1, col2 = st.columns(2)
        cl = col1.number_input("حد الائتمان", 0.0, 999999.0, 0.0)
        ob = col2.number_input("الرصيد الافتتاحي", -999999.0, 999999.0, 0.0)
        if st.form_submit_button("💾 حفظ"):
            if name:
                db.execute_query("INSERT INTO parties (name, type, phone, credit_limit, opening_balance, is_active) VALUES (%s, %s, %s, %s, %s, TRUE)", (name, ptype, phone, cl, ob))
                st.success("✅ تم الحفظ"); st.rerun()
            else: st.error("❌ الاسم مطلوب")