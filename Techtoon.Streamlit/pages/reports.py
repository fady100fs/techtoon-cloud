import streamlit as st
import database as db
import pandas as pd
import plotly.express as px
from datetime import datetime, timedelta

def show():
    st.title("📈 التقارير")
    rpt = st.selectbox("نوع التقرير", ["تقرير المبيعات", "تقرير الجرد", "كشف حساب", "تقرير المصروفات"])
    col1, col2 = st.columns(2)
    start = col1.date_input("من", datetime.now() - timedelta(days=30))
    end = col2.date_input("إلى", datetime.now())
    if st.button("📊 توليد"):
        if rpt == "تقرير المبيعات":
            try:
                _, rows = db.execute_query("SELECT i.invoice_number, i.date, COALESCE(p.name,''), i.total, i.paid_amount, i.status FROM invoices i LEFT JOIN parties p ON i.party_id=p.id WHERE i.type='sales' AND i.date BETWEEN %s AND %s AND i.status<>'cancelled' ORDER BY i.date", (start, end + timedelta(days=1)), fetch=True)
                if rows:
                    df = pd.DataFrame(rows, columns=["رقم", "التاريخ", "العميل", "الإجمالي", "المدفوع", "الحالة"])
                    st.metric("الإجمالي", f"{df['الإجمالي'].sum():,.2f}")
                    st.dataframe(df, use_container_width=True, hide_index=True)
                else: st.info("لا توجد مبيعات")
            except Exception as e: st.error(f"خطأ: {e}")
        elif rpt == "تقرير الجرد":
            try:
                _, rows = db.execute_query("SELECT i.name, i.barcode, i.opening_quantity, COALESCE(i.avg_cost_price, i.cost_price, 0), i.sell_price FROM items i WHERE i.is_active=TRUE AND i.is_kit IS NOT TRUE ORDER BY i.name", fetch=True)
                if rows:
                    df = pd.DataFrame(rows, columns=["الصنف", "الباركود", "الكمية", "التكلفة", "سعر البيع"])
                    df["القيمة"] = df["الكمية"] * df["التكلفة"]
                    st.metric("إجمالي القيمة", f"{df['القيمة'].sum():,.2f}")
                    st.dataframe(df, use_container_width=True, hide_index=True)
                else: st.info("لا توجد أصناف")
            except Exception as e: st.error(f"خطأ: {e}")
        elif rpt == "كشف حساب":
            try:
                _, ps = db.execute_query("SELECT id, name FROM parties WHERE is_active=TRUE ORDER BY name", fetch=True)
                if ps:
                    p_opts = {p[1]: p[0] for p in ps}
                    p_name = st.selectbox("اختر الجهة", list(p_opts.keys()))
                    pid = p_opts[p_name]
                    _, invs = db.execute_query("SELECT date, invoice_number, total, paid_amount FROM invoices WHERE party_id=%s ORDER BY date", (pid,), fetch=True)
                    _, pays = db.execute_query("SELECT date, reference_number, amount FROM payments WHERE party_id=%s ORDER BY date", (pid,), fetch=True)
                    c1, c2 = st.columns(2)
                    with c1:
                        st.markdown("#### الفواتير")
                        if invs: st.dataframe(pd.DataFrame(invs, columns=["التاريخ", "رقم", "الإجمالي", "المدفوع"]), use_container_width=True, hide_index=True)
                    with c2:
                        st.markdown("#### المدفوعات")
                        if pays: st.dataframe(pd.DataFrame(pays, columns=["التاريخ", "مرجع", "مبلغ"]), use_container_width=True, hide_index=True)
            except Exception as e: st.error(f"خطأ: {e}")
        elif rpt == "تقرير المصروفات":
            try:
                _, rows = db.execute_query("SELECT e.date, ec.name, e.amount, e.notes FROM expenses e LEFT JOIN expense_categories ec ON e.category_id=ec.id WHERE e.date BETWEEN %s AND %s ORDER BY e.date", (start, end + timedelta(days=1)), fetch=True)
                if rows:
                    df = pd.DataFrame(rows, columns=["التاريخ", "الفئة", "المبلغ", "ملاحظات"])
                    st.metric("الإجمالي", f"{df['المبلغ'].sum():,.2f}")
                    st.dataframe(df, use_container_width=True, hide_index=True)
                else: st.info("لا توجد مصروفات")
            except Exception as e: st.error(f"خطأ: {e}")