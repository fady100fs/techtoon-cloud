import streamlit as st
import database as db
import pandas as pd
from datetime import datetime

def show():
    st.title("💰 المدفوعات")
    search = st.text_input("🔍 بحث", placeholder="اسم الجهة أو المرجع")
    query = "SELECT p.id, p.date, p.payment_type, COALESCE(pt.name,''), COALESCE(a.name,''), p.amount, p.payment_method, p.reference_number FROM payments p LEFT JOIN parties pt ON p.party_id=pt.id LEFT JOIN accounts a ON p.account_id=a.id"
    params = []
    if search:
        query += " WHERE pt.name ILIKE %s OR a.name ILIKE %s OR p.reference_number ILIKE %s"
        params.extend([f"%{search}%", f"%{search}%", f"%{search}%"])
    query += " ORDER BY p.date DESC LIMIT 100"
    try:
        cols, rows = db.execute_query(query, tuple(params), fetch=True)
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "التاريخ", "النوع", "الجهة", "الحساب", "المبلغ", "الطريقة", "المرجع"])
            df["الجهة/الحساب"] = df["الجهة"].fillna('') + df["الحساب"].fillna('')
            df = df.drop(columns=["الجهة", "الحساب"])
            st.dataframe(df, use_container_width=True, hide_index=True)
            selected_id = st.selectbox("اختر", [r[0] for r in rows], format_func=lambda x: f"#{x} - {[r[3] or r[4] for r in rows if r[0]==x][0]} - {[r[5] for r in rows if r[0]==x][0]}")
            if st.button("🗑️ حذف", type="secondary"):
                db.execute_query("DELETE FROM payments WHERE id=%s", (selected_id,))
                st.success("✅ تم الحذف"); st.rerun()
        else: st.info("لا توجد مدفوعات")
    except Exception as e: st.error(f"خطأ: {e}")

    st.markdown("---")
    with st.form("new_payment"):
        st.markdown("### ➕ سند جديد")
        col1, col2 = st.columns(2)
        with col1:
            ptype = st.selectbox("النوع", ["receipt", "payment"], format_func=lambda x: "سند قبض" if x=="receipt" else "سند صرف")
            target_type = st.radio("نوع الجهة", ["عميل/مورد", "حساب (مصاريف بيت/إلخ)"])
            if target_type == "عميل/مورد":
                _, parties = db.execute_query("SELECT id, name FROM parties WHERE is_active=TRUE ORDER BY name", fetch=True)
                opts = {p[1]: p[0] for p in parties} if parties else {}
                target_name = st.selectbox("الجهة *", list(opts.keys()) if opts else ["لا يوجد"])
                account_id = None
            else:
                _, accounts = db.execute_query("SELECT id, name FROM accounts ORDER BY name", fetch=True)
                opts = {a[1]: a[0] for a in accounts} if accounts else {}
                target_name = st.selectbox("الحساب *", list(opts.keys()) if opts else ["لا يوجد"])
            amount = st.number_input("المبلغ *", 0.01, 999999.0, 0.01)
        with col2:
            pdate = st.date_input("التاريخ", datetime.now())
            method = st.selectbox("طريقة الدفع", ["نقدي", "بطاقة", "تحويل"])
            reference = st.text_input("المرجع")
        notes = st.text_area("ملاحظات")
        if st.form_submit_button("💾 حفظ"):
            party_id = opts.get(target_name) if target_type == "عميل/مورد" else None
            acc_id = opts.get(target_name) if target_type == "حساب (مصاريف بيت/إلخ)" else None
            _, cb = db.execute_query("SELECT id FROM cash_boxes WHERE is_active=TRUE LIMIT 1", fetch=True)
            cb_id = cb[0][0] if cb else 1
            db.execute_query("INSERT INTO payments (date, party_id, account_id, amount, payment_type, payment_method, reference_number, notes, currency_id, cash_box_id) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, 1, %s)", (pdate, party_id, acc_id, amount, ptype, method, reference or None, notes or None, cb_id))
            st.success("✅ تم الحفظ"); st.rerun()