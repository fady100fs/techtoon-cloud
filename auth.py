# auth.py
"""
نظام المصادقة والصلاحيات
يستخدم bcrypt مباشرة
"""

import bcrypt
from database import SessionLocal
import models
from models import User, UserRole
from datetime import datetime
import streamlit as st
import os
from pathlib import Path

def hash_password(password: str) -> str:
    """تشفير كلمة المرور باستخدام bcrypt"""
    password_bytes = password.encode('utf-8')
    salt = bcrypt.gensalt()
    hashed = bcrypt.hashpw(password_bytes, salt)
    return hashed.decode('utf-8')

def verify_password(password: str, password_hash: str) -> bool:
    """التحقق من كلمة المرور"""
    password_bytes = password.encode('utf-8')
    hash_bytes = password_hash.encode('utf-8')
    return bcrypt.checkpw(password_bytes, hash_bytes)

def create_user(username: str, password: str, full_name: str, role: UserRole, email: str = None):
    """إنشاء مستخدم جديد"""
    db = SessionLocal()
    try:
        existing = db.query(User).filter(User.username == username).first()
        if existing:
            raise ValueError("اسم المستخدم موجود مسبقاً!")
        
        user = User(
            username=username,
            password_hash=hash_password(password),
            full_name=full_name,
            email=email,
            role=role,
            is_active=True
        )
        db.add(user)
        db.commit()
        print(f"✅ تم إنشاء المستخدم '{username}' بنجاح!")
        return user
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()

def authenticate_user(username: str, password: str):
    """
    تسجيل الدخول - يُرجع قاموساً يحتوي بيانات المستخدم (ليس كائن SQLAlchemy)
    لتجنب مشكلة DetachedInstanceError
    """
    db = SessionLocal()
    try:
        user = db.query(User).filter(User.username == username).first()
        
        if not user:
            return None, "اسم المستخدم غير صحيح"
        
        if not user.is_active:
            return None, "الحساب معطل"
        
        if not verify_password(password, user.password_hash):
            return None, "كلمة المرور غير صحيحة"
        
        # تحديث آخر تسجيل دخول
        user.last_login = datetime.now()
        db.commit()
        
        # إنشاء قاموس مستقل ببيانات المستخدم (الحل الأساسي للمشكلة)
        user_data = {
            'id': user.id,
            'username': user.username,
            'full_name': user.full_name,
            'email': user.email,
            'role': user.role,
            'is_active': user.is_active,
            'created_at': user.created_at,
            'last_login': user.last_login
        }
        
        return user_data, None
    finally:
        db.close()

def get_user_permissions(role: UserRole):
    """الحصول على صلاحيات الدور"""
    permissions = {
        UserRole.ADMIN: {
            'can_create': True,
            'can_edit': True,
            'can_delete': True,
            'can_view_reports': True,
            'can_manage_users': True,
            'can_backup': True,
            'can_import': True
        },
        UserRole.ACCOUNTANT: {
            'can_create': True,
            'can_edit': True,
            'can_delete': False,
            'can_view_reports': True,
            'can_manage_users': False,
            'can_backup': True,
            'can_import': True
        },
        UserRole.SALESPERSON: {
            'can_create': True,
            'can_edit': True,
            'can_delete': False,
            'can_view_reports': False,
            'can_manage_users': False,
            'can_backup': False,
            'can_import': False
        },
        UserRole.VIEWER: {
            'can_create': False,
            'can_edit': False,
            'can_delete': False,
            'can_view_reports': True,
            'can_manage_users': False,
            'can_backup': False,
            'can_import': False
        }
    }
    return permissions.get(role, permissions[UserRole.VIEWER])

def check_permission(permission: str) -> bool:
    """التحقق من صلاحية المستخدم الحالي"""
    if 'current_user' not in st.session_state:
        return False
    
    user = st.session_state.current_user
    permissions = get_user_permissions(user['role'])
    return permissions.get(permission, False)

def get_current_user_id() -> int:
    """الحصول على ID المستخدم الحالي"""
    if 'current_user' in st.session_state:
        return st.session_state.current_user['id']
    return None

def get_last_backup_time() -> str:
    """الحصول على وقت آخر نسخة احتياطية"""
    backup_folder = Path(__file__).parent / "backups"
    
    if not backup_folder.exists():
        return "لم يتم إنشاء نسخة احتياطية بعد"
    
    backup_files = [f for f in backup_folder.iterdir() if f.suffix == '.db']
    
    if not backup_files:
        return "لم يتم إنشاء نسخة احتياطية بعد"
    
    # الحصول على أحدث ملف
    latest_backup = max(backup_files, key=lambda f: f.stat().st_mtime)
    backup_time = datetime.fromtimestamp(latest_backup.stat().st_mtime)
    
    return f"آخر نسخة: {backup_time.strftime('%Y-%m-%d %H:%M:%S')}"

def create_default_admin():
    """إنشاء مدير افتراضي إذا لم يكن موجوداً"""
    db = SessionLocal()
    try:
        admin = db.query(User).filter(User.username == "admin").first()
        if not admin:
            admin = User(
                username="admin",
                password_hash=hash_password("admin123"),
                full_name="المدير العام",
                email="admin@techtoon.com",
                role=UserRole.ADMIN,
                is_active=True
            )
            db.add(admin)
            db.commit()
            print("✅ تم إنشاء المستخدم الافتراضي: admin / admin123")
    except Exception as e:
        print(f"خطأ في إنشاء المستخدم الافتراضي: {e}")
    finally:
        db.close()