# reset_admin_password.py
"""
سكريبت لإعادة تعيين كلمة مرور المدير
"""

from database import SessionLocal
from models import User
from auth import hash_password

def reset_admin_password(new_password="admin123"):
    """إعادة تعيين كلمة مرور admin"""
    db = SessionLocal()
    try:
        admin = db.query(User).filter(User.username == "admin").first()
        
        if not admin:
            print("❌ المستخدم admin غير موجود!")
            print("جاري إنشاء المستخدم admin...")
            
            admin = User(
                username="admin",
                password_hash=hash_password(new_password),
                full_name="المدير العام",
                email="admin@techtoon.com",
                role="مدير",
                is_active=True
            )
            db.add(admin)
            db.commit()
            print(f"✅ تم إنشاء المستخدم admin بكلمة مرور: {new_password}")
        else:
            admin.password_hash = hash_password(new_password)
            db.commit()
            print(f"✅ تم إعادة تعيين كلمة مرور admin إلى: {new_password}")
        
        print("\n" + "=" * 60)
        print("بيانات تسجيل الدخول:")
        print(f"اسم المستخدم: admin")
        print(f"كلمة المرور: {new_password}")
        print("=" * 60)
        
    except Exception as e:
        print(f"❌ خطأ: {e}")
    finally:
        db.close()

if __name__ == "__main__":
    print("🔄 جاري إعادة تعيين كلمة مرور admin...")
    reset_admin_password("admin123")