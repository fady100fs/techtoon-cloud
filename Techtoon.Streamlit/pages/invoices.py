import streamlit as st
import database as db
import pandas as pd

def show():
    st.title("🛒 الفواتير")
    search = st.text_input("🔍 بحث", placeholder="رقم الفاتورة أو اسم العميل")
    query = "SELECT i.id, i.invoice_number, i.date, i.type, COALESCE(p.name,'') as party, i.total, i.paid_amount, i.status FROM invoices i LEFT JOIN parties p ON i.party_id=p.id"
    params = []
    if search:
        query += " WHERE i.invoice_number ILIKE %s OR p.name ILIKE %s"
        params.extend([f"%{search}%", f"%{search}%"])
    query += " ORDER BY i.date DESC LIMIT 100"
    try:
        cols, rows = db.execute_query(query, tuple(params), fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "رقم الفاتورة", "التاريخ", "النوع", "العميل", "الإجمالي", "المدفوع", "الحالة"])
            st.dataframe(df, use_container_width=True, hide_index=True)
            selected_id = st.selectbox("اختر فاتورة للإجراءات", [r[0] for r in rows], format_func=lambda x: f"#{x} - {[r[1] for r in rows if r[0]==x][0]}")
            col1, col2 = st.columns(2)
            with col1:
                if st.button("👁️ عرض التفاصيل"):
                    _, det = db.execute_query("SELECT i.invoice_number, i.date, i.type, COALESCE(p.name,''), i.total, i.paid_amount, i.status, i.notes FROM invoices i LEFT JOIN parties p ON i.party_id=p.id WHERE i.id=%s", (selected_id,), fetch=True)
                    if det:
                        r = det[0]
                        st.markdown(f"### الفاتورة: {r[0]}")
                        st.write(f"**التاريخ:** {r[1]} | **النوع:** {r[2]} | **العميل:** {r[3]} | **الإجمالي:** {r[4]:.2f} | **المدفوع:** {r[5]:.2f} | **الحالة:** {r[6]}")
                        st.markdown("#### الأصناف")
                        _, lines = db.execute_query("SELECT i.name, il.quantity, il.price, il.total FROM invoice_lines il JOIN items i ON il.item_id=i.id WHERE il.invoice_id=%s", (selected_id,), fetch=True)
                        if lines: st.dataframe(pd.DataFrame(lines, columns=["الصنف", "الكمية", "السعر", "الإجمالي"]), use_container_width=True, hide_index=True)
            with col2:
                if st.button("🗑️ حذف", type="secondary"):
                    db.execute_query("DELETE FROM invoice_lines WHERE invoice_id=%s", (selected_id,))
                    db.execute_query("DELETE FROM invoice_payments WHERE invoice_id=%s", (selected_id,))
                    db.execute_query("DELETE FROM payments WHERE invoice_id=%s", (selected_id,))
                    db.execute_query("DELETE FROM inventory_movements WHERE reference_id=%s AND reference_type='invoice'", (selected_id,))
                    db.execute_query("DELETE FROM invoices WHERE id=%s", (selected_id,))
                    st.success("✅ تم الحذف"); st.rerun()
        else: st.info("لا توجد فواتير")
    except Exception as e: st.error(f"خطأ: {e}")