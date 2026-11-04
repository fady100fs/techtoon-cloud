import streamlit as st
import database as db
import pandas as pd

def show():
    st.title("📦 الأصناف")
    search = st.text_input("🔍 بحث", placeholder="اسم الصنف أو الباركود")
    query = "SELECT id, name, barcode, cost_price, sell_price, opening_quantity FROM items WHERE is_active=TRUE"
    params = []
    if search:
        query += " AND (name ILIKE %s OR barcode ILIKE %s)"
        params.extend([f"%{search}%", f"%{search}%"])
    query += " ORDER BY name LIMIT 200"
    try:
        cols, rows = db.execute_query(query, tuple(params), fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "الاسم", "الباركود", "التكلفة", "سعر البيع", "الكمية"])
            st.dataframe(df, use_container_width=True, hide_index=True)
            selected_id = st.selectbox("اختر صنف", [r[0] for r in rows], format_func=lambda x: f"#{x} - {[r[1] for r in rows if r[0]==x][0]}")
            if st.button("🗑️ تعطيل الصنف", type="secondary"):
                db.execute_query("UPDATE items SET is_active=FALSE WHERE id=%s", (selected_id,))
                st.success("✅ تم التعطيل"); st.rerun()
        else: st.info("لا توجد أصناف")
    except Exception as e: st.error(f"خطأ: {e}")

    st.markdown("---")
    with st.form("new_item"):
        st.markdown("### ➕ صنف جديد")
        name = st.text_input("اسم الصنف *")
        barcode = st.text_input("الباركود")
        col1, col2, col3 = st.columns(3)
        cost = col1.number_input("التكلفة", 0.0, 999999.0, 0.0)
        sell = col2.number_input("سعر البيع", 0.0, 999999.0, 0.0)
        qty = col3.number_input("الكمية الافتتاحية", 0.0, 999999.0, 0.0)
        if st.form_submit_button("💾 حفظ"):
            if name:
                db.execute_query("INSERT INTO items (name, barcode, cost_price, sell_price, opening_quantity, is_active, is_kit) VALUES (%s, %s, %s, %s, %s, TRUE, FALSE)", (name, barcode or None, cost, sell, qty))
                st.success("✅ تم الحفظ"); st.rerun()
            else: st.error("❌ الاسم مطلوب")