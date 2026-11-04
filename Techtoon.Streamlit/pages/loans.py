import streamlit as st
import database as db
import pandas as pd
from datetime import datetime

def show():
    st.title("💰 القروض والتسهيلات")
    
    col1, col2 = st.columns([3, 1])
    with col2:
        if st.button("➕ قرض جديد"):
            st.session_state.show_new_loan = True
    
    if st.session_state.get("show_new_loan"):
        new_loan_form()
    
    try:
        _, rows = db.execute_query(
            "SELECT l.id, l.loan_name, l.loan_type, l.principal_amount, l.interest_rate, l.status "
            "FROM loans ORDER BY l.id DESC",
            fetch=True
        )
        if rows:
            df = pd.DataFrame(rows, columns=["ID", "الاسم", "النوع", "المبلغ", "الفائدة", "الحالة"])
            st.dataframe(df, use_container_width=True, hide_index=True)
            
            selected_id = st.selectbox("اختر قرضاً", [r[0] for r in rows], 
                                       format_func=lambda x: f"#{x} - {[r[1] for r in rows if r[0]==x][0]}")
            
            if st.button("👁️ عرض التفاصيل"):
                show_details(selected_id)
        else:
            st.info("لا توجد قروض")
    except Exception as e:
        st.error(f"خطأ: {e}")

def new_loan_form():
    st.markdown("### ➕ قرض جديد")
    with st.form("new_loan"):
        col1, col2 = st.columns(2)
        with col1:
            name = st.text_input("اسم القرض *")
            ltype = st.selectbox("النوع", ["installment - أقساط", "overdraft - جاري مدين"])
            amount = st.number_input("المبلغ *", 0.0, 999999999.0, 0.0)
        with col2:
            rate = st.number_input("سعر الفائدة (%) *", 0.0, 100.0, 0.0)
            disburse = st.date_input("تاريخ الصرف", datetime.now())
            first_pay = st.date_input("تاريخ أول قسط", datetime.now())
        
        num = st.number_input("عدد الأقساط", 0, 360, 0)
        notes = st.text_area("ملاحظات")
        
        if st.form_submit_button("💾 حفظ"):
            if name and amount > 0 and rate > 0:
                try:
                    lt = ltype.split(" - ")[0]
                    db.execute_query(
                        "INSERT INTO loans (loan_name, loan_type, principal_amount, interest_rate, disbursement_date, first_payment_date, num_installments, status, notes, created_by, created_at) "
                        "VALUES (%s, %s, %s, %s, %s, %s, %s, 'active', %s, 1, NOW())",
                        (name, lt, amount, rate, disburse, first_pay, num, notes or None)
                    )
                    st.success("✅ تم حفظ القرض")
                    st.session_state.show_new_loan = False
                    st.rerun()
                except Exception as e:
                    st.error(f"خطأ: {e}")
            else:
                st.error("❌ البيانات غير مكتملة")

def show_details(loan_id):
    st.markdown("### 📋 تفاصيل القرض")
    try:
        _, loan = db.execute_query("SELECT loan_name, loan_type, principal_amount, interest_rate, status FROM loans WHERE id=%s", (loan_id,), fetch=True)
        if loan:
            r = loan[0]
            st.write(f"**الاسم:** {r[0]} | **النوع:** {r[1]} | **المبلغ:** {r[2]} | **الفائدة:** {r[3]}% | **الحالة:** {r[4]}")
            
            st.markdown("#### جدول الأقساط")
            _, insts = db.execute_query("SELECT installment_number, due_date, principal_amount, interest_amount, total_amount, paid_amount, status FROM loan_installments WHERE loan_id=%s ORDER BY installment_number", (loan_id,), fetch=True)
            if insts:
                df = pd.DataFrame(insts, columns=["م", "الاستحقاق", "الأصل", "الفائدة", "الإجمالي", "المدفوع", "الحالة"])
                st.dataframe(df, use_container_width=True, hide_index=True)
    except Exception as e:
        st.error(f"خطأ: {e}")