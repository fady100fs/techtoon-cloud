# test_db.py — اختبار الاتصال بـ Neon PostgreSQL
from database import engine
from sqlalchemy import text

print("🔄 جاري الاتصال بـ Neon...")

# ==========================================
# اختبار 1: هل الاتصال شغال؟
# ==========================================
try:
    with engine.connect() as conn:
        r = conn.execute(text("SELECT version()"))
        version = r.scalar()
        print(f"✅ الاتصال ناجح!")
        print(f"📊 نسخة PostgreSQL: {version[:50]}...")
except Exception as e:
    print(f"❌ فشل الاتصال: {type(e).__name__}")
    print(f"📝 التفاصيل: {e}")
    exit(1)

# ==========================================
# اختبار 2: هل دالة strftime موجودة؟
# ==========================================
try:
    with engine.connect() as conn:
        r = conn.execute(text(
            "SELECT proname, pg_get_function_arguments(oid) "
            "FROM pg_proc WHERE proname='strftime'"
        ))
        funcs = r.fetchall()
        if funcs:
            print(f"\n✅ دالة strftime موجودة ({len(funcs)} نسخة):")
            for name, args in funcs:
                print(f"    - {name}({args})")
        else:
            print(f"\n❌ دالة strftime مش موجودة في قاعدة البيانات!")
except Exception as e:
    print(f"\n⚠️ خطأ في فحص الدالة: {e}")

# ==========================================
# اختبار 3: تجربة استدعاء الدالة بأنواع مختلفة
# ==========================================
try:
    with engine.connect() as conn:
        # مع NOW() (timestamptz)
        r = conn.execute(text("SELECT strftime('%Y', NOW())"))
        result = r.scalar()
        print(f"\n✅ strftime('%Y', NOW()) = {result}")

        # مع timestamp صريح (بدون timezone)
        r = conn.execute(text("SELECT strftime('%Y-%m', '2026-10-04'::timestamp)"))
        result = r.scalar()
        print(f"✅ strftime('%Y-%m', timestamp) = {result}")
except Exception as e:
    print(f"\n❌ فشل الاستدعاء: {type(e).__name__}")
    print(f"📝 التفاصيل: {e}")

# ==========================================
# اختبار 4: عدد الجداول
# ==========================================
try:
    with engine.connect() as conn:
        r = conn.execute(text(
            "SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename"
        ))
        tables = [row[0] for row in r.fetchall()]
        print(f"\n📋 عدد الجداول: {len(tables)}")
        if tables:
            print(f"📋 أسماء الجداول: {tables[:10]}{'...' if len(tables) > 10 else ''}")
        else:
            print("📋 (مفيش جداول — طبيعي لقاعدة جديدة)")
except Exception as e:
    print(f"⚠️ خطأ في قراءة الجداول: {e}")

print("\n" + "=" * 50)
print("انتهى الاختبار")
print("=" * 50)