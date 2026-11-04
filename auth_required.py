# auth_required.py
"""
ملف مشترك للتحقق من تسجيل الدخول في جميع الصفحات
يتم استدعاؤه في بداية كل صفحة
"""

import streamlit as st

def require_login():
    """
    التحقق من تسجيل الدخول - إذا لم يكن المستخدم مسجلاً، يتم إيقاف الصفحة
    """
    if 'current_user' not in st.session_state:
        st.error("⛔ يجب تسجيل الدخول أولاً للوصول إلى هذه الصفحة!")
        st.warning(" انتقل إلى صفحة **تسجيل الدخول** من القائمة الجانبية")
        st.markdown("---")
        st.info("💡 إذا لم يكن لديك حساب، تواصل مع مدير النظام")
        st.stop()  # إيقاف تنفيذ الصفحة
    
    return st.session_state.current_user

def get_current_user():
    """
    الحصول على بيانات المستخدم الحالي (بعد التأكد من تسجيل الدخول)
    """
    user = require_login()
    return user

def get_current_user_id():
    """
    الحصول على ID المستخدم الحالي
    """
    user = require_login()
    return user['id']

def get_current_user_name():
    """
    الحصول على اسم المستخدم الحالي
    """
    user = require_login()
    return user['full_name']

def get_current_user_role():
    """
    الحصول على دور المستخدم الحالي
    """
    user = require_login()
    return user['role']

def check_permission(permission: str) -> bool:
    """
    التحقق من صلاحية معينة للمستخدم الحالي
    """
    from auth import get_user_permissions
    
    user = require_login()
    permissions = get_user_permissions(user['role'])
    return permissions.get(permission, False)

def require_permission(permission: str):
    """
    التحقق من الصلاحية - إذا لم تكن متوفرة، يتم إيقاف الصفحة
    """
    if not check_permission(permission):
        user = get_current_user()
        st.error(f"⛔ ليس لديك صلاحية '{permission}'!")
        st.warning(f"👤 دورك الحالي: **{user['role'].value}**")
        st.info("💡 تواصل مع مدير النظام للحصول على الصلاحية المطلوبة")
        st.stop()