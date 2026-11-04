# confirm_dialog.py
"""نوافذ تأكيد للعمليات الخطرة (حذف، إلغاء، تصفير)."""
import streamlit as st


def confirm_action(action_label, message, confirm_text="تأكيد", cancel_text="إلغاء", danger=True):
    """
    عرض نافذة تأكيد.
    
    Returns:
        True لو أكّد المستخدم
        False لو ألغى
        None لو لم يفتح بعد
    """
    # مفتاح فريد لكل عملية
    key = f"confirm_{hash(action_label + message)}"
    
    # لو التأكيد مفعّل بالفعل
    if st.session_state.get(key, False):
        return True
    
    # اعرض النافذة
    color = "#ef4444" if danger else "#3b82f6"
    bg = "rgba(239,68,68,0.1)" if danger else "rgba(59,130,246,0.1)"
    
    st.markdown(f"""
    <div style="background:{bg};border:1px solid {color};border-radius:8px;padding:12px 16px;margin:8px 0;">
        <div style="color:{color};font-weight:bold;margin-bottom:8px;">⚠️ {action_label}</div>
        <div style="color:#cbd5e1;font-size:13px;">{message}</div>
    </div>
    """, unsafe_allow_html=True)
    
    col1, col2, col3 = st.columns([1, 1, 2])
    with col1:
        if st.button(f"✅ {confirm_text}", key=f"{key}_yes", type="primary"):
            st.session_state[key] = True
            return True
    with col2:
        if st.button(f"❌ {cancel_text}", key=f"{key}_no"):
            st.session_state[key] = False
            return False
    return None


def reset_confirmation(action_label):
    """إعادة ضبط حالة التأكيد."""
    key = f"confirm_{hash(action_label)}"
    st.session_state.pop(key, None)
