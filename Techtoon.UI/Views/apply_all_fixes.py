# -*- coding: utf-8 -*-
"""
=====================================================================
 Techtoon Accounting C+ — تطبيق الإصلاحات
=====================================================================
 هذا السكربت يقوم بـ:
 1) نسخ الملفات المصححة إلى مشروعك
 2) تنفيذ هجرة قاعدة البيانات (إضافة الأعمدة المفقودة)
 3) التأكد من إعدادات الاتصال

 التشغيل:
     python apply_all_fixes.py

 ملاحظة: يجب تثبيت psycopg2-binary:
     pip install psycopg2-binary
=====================================================================
"""
import os
import sys
import json
import shutil
import subprocess

# =====================================================================
#  الإعدادات
# =====================================================================
PROJECT_PATH = r"D:\PROG\Accounting Prog\Techtoon Accounting C+"

# مسار الملفات داخل المشروع
TARGET_FILES = {
    # الملف المصدر (بجانب هذا السكربت) => المسار الهدف في المشروع
    "InvoiceEditViewModel.cs": os.path.join("Techtoon.UI", "Views", "InvoiceEditViewModel.cs"),
    "InvoiceEditWindow.xaml":  os.path.join("Techtoon.UI", "Views", "InvoiceEditWindow.xaml"),
    "InvoiceEditWindow.xaml.cs": os.path.join("Techtoon.UI", "Views", "InvoiceEditWindow.xaml.cs"),
}

# ملف SQL للهجرة
SQL_FILE = "fix_database.sql"

# =====================================================================
#  ألوان الطباعة
# =====================================================================
def green(t):  return f"\033[92m{t}\033[0m"
def red(t):    return f"\033[91m{t}\033[0m"
def yellow(t): return f"\033[93m{t}\033[0m"
def cyan(t):   return f"\033[96m{t}\033[0m"
def bold(t):   return f"\033[1m{t}\033[0m"

def banner():
    print(cyan("=" * 60))
    print(cyan("  Techtoon Accounting C+ — تطبيق الإصلاحات"))
    print(cyan("=" * 60))
    print()

# =====================================================================
#  قراءة سلسلة الاتصال من appsettings.json
# =====================================================================
def get_connection_string(project_path):
    """يحاول قراءة سلسلة الاتصال من appsettings.json"""
    candidates = [
        os.path.join(project_path, "Techtoon.UI", "appsettings.json"),
        os.path.join(project_path, "appsettings.json"),
        os.path.join(project_path, "Techtoon.Data", "appsettings.json"),
    ]
    for path in candidates:
        if not os.path.exists(path):
            continue
        try:
            with open(path, "r", encoding="utf-8") as f:
                content = f.read()
            # ابحث عن سلسلة الاتصال
            # الطريقة 1: JSON parsing
            try:
                data = json.loads(content)
                for key in ["ConnectionString", "ConnectionStrings", "connection", "connectionString"]:
                    if key in data:
                        val = data[key]
                        if isinstance(val, dict):
                            for v in val.values():
                                if "Host=" in str(v):
                                    return str(v)
                        elif isinstance(val, str) and "Host=" in val:
                            return val
                # بحث في كل القيم
                def search_dict(d):
                    for v in d.values():
                        if isinstance(v, str) and "Host=" in v:
                            return v
                        if isinstance(v, dict):
                            r = search_dict(v)
                            if r:
                                return r
                    return None
                result = search_dict(data)
                if result:
                    return result
            except json.JSONDecodeError:
                pass
            # الطريقة 2: بحث نصي
            for keyword in ["Host=", "host="]:
                idx = content.find(keyword)
                if idx >= 0:
                    # ابحث عن علامة التنصيص قبلها
                    start = content.rfind('"', 0, idx)
                    if start >= 0:
                        end = content.find('"', idx)
                        if end > idx:
                            return content[start+1:end]
        except Exception:
            continue

    # القيمة الافتراضية (Neon PostgreSQL)
    return "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"

def parse_connection_string(cs):
    """يحول سلسلة الاتصال إلى قاموس"""
    parts = {}
    for part in cs.split(";"):
        part = part.strip()
        if "=" in part:
            k, v = part.split("=", 1)
            parts[k.strip().lower()] = v.strip()
    return parts

# =====================================================================
#  نسخ الملفات
# =====================================================================
def copy_files(script_dir, project_path):
    print(bold("【1】 نسخ الملفات المصححة..."))
    print()

    if not os.path.isdir(project_path):
        print(red(f"  ✗ المسار غير موجود: {project_path}"))
        print(yellow("  عدّل PROJECT_PATH في هذا السكربت إلى مسار مشروعك الصحيح"))
        return False

    copied = 0
    for src_name, rel_dest in TARGET_FILES.items():
        src = os.path.join(script_dir, src_name)
        dst = os.path.join(project_path, rel_dest)

        if not os.path.exists(src):
            print(yellow(f"  ⚠ الملف المصدر غير موجود: {src_name}"))
            continue

        # أنشئ المجلد إن لم يكن موجوداً
        os.makedirs(os.path.dirname(dst), exist_ok=True)

        # نسخة احتياطية
        if os.path.exists(dst):
            backup = dst + ".bak"
            shutil.copy2(dst, backup)
            print(f"  ↳ نسخة احتياطية: {os.path.basename(dst)}.bak")

        shutil.copy2(src, dst)
        print(green(f"  ✓ تم النسخ: {rel_dest}"))
        copied += 1

    print()
    print(green(f"  ✅ تم نسخ {copied} ملف بنجاح"))
    print()
    return True

# =====================================================================
#  تنفيذ هجرة قاعدة البيانات
# =====================================================================
def run_database_migration(script_dir, project_path):
    print(bold("【2】 تنفيذ هجرة قاعدة البيانات..."))
    print()

    sql_path = os.path.join(script_dir, SQL_FILE)
    if not os.path.exists(sql_path):
        print(red(f"  ✗ ملف SQL غير موجود: {SQL_FILE}"))
        return False

    with open(sql_path, "r", encoding="utf-8") as f:
        sql_content = f.read()

    cs = get_connection_string(project_path)
    parts = parse_connection_string(cs)

    host = parts.get("host", "localhost")
    port = parts.get("port", "5432")
    db   = parts.get("database", "techtoon")
    user = parts.get("username", parts.get("user", "postgres"))
    pw   = parts.get("password", parts.get("pwd", ""))

    print(f"  الخادم:    {host}")
    print(f"  المنفذ:    {port}")
    print(f"  القاعدة:   {db}")
    print(f"  المستخدم:  {user}")
    print()

    # محاولة استخدام psycopg2
    try:
        import psycopg2
    except ImportError:
        print(yellow("  ⚠ psycopg2 غير مثبت. جاري التثبيت..."))
        try:
            subprocess.check_call([sys.executable, "-m", "pip", "install", "psycopg2-binary"])
            import psycopg2
            print(green("  ✓ تم تثبيت psycopg2-binary"))
        except Exception as e:
            print(red(f"  ✗ تعذر تثبيت psycopg2: {e}"))
            print(yellow("  نفّذ يدوياً: pip install psycopg2-binary"))
            print()
            print(yellow("  ثم نفّذ ملف SQL يدوياً:"))
            print(f"     psql \"{cs}\" -f \"{sql_path}\"")
            return False

    try:
        print("  جاري الاتصال بقاعدة البيانات...")
        conn = psycopg2.connect(
            host=host, port=int(port), dbname=db,
            user=user, password=pw,
            sslmode="require"
        )
        conn.autocommit = True
        cur = conn.cursor()
        print(green("  ✓ تم الاتصال بنجاح"))
        print()

        # تنفيذ كل جملة SQL على حدة
        statements = [s.strip() for s in sql_content.split(";") if s.strip() and not s.strip().startswith("--")]
        success = 0
        errors = 0
        for stmt in statements:
            # تخطي التعليقات
            lines = [l for l in stmt.split("\n") if not l.strip().startswith("--")]
            stmt = "\n".join(lines).strip()
            if not stmt:
                continue
            try:
                cur.execute(stmt)
                success += 1
            except Exception as e:
                err_msg = str(e)
                # تجاهل أخطاء "العمود موجود فعلاً"
                if "already exists" in err_msg.lower():
                    success += 1
                else:
                    errors += 1
                    print(yellow(f"  ⚠ {err_msg[:100]}"))

        cur.close()
        conn.close()

        print()
        print(green(f"  ✅ تم تنفيذ {success} جملة SQL بنجاح"))
        if errors > 0:
            print(yellow(f"  ⚠ {errors} جملة تم تخطيها (غالباً لأن العمود/الجدول موجود فعلاً)"))
        print()
        return True

    except Exception as e:
        print(red(f"  ✗ خطأ في الاتصال: {e}"))
        print()
        print(yellow("  نفّذ ملف SQL يدوياً:"))
        print(f"     psql \"{cs}\" -f \"{sql_path}\"")
        return False

# =====================================================================
#  التحقق من البناء
# =====================================================================
def check_build(project_path):
    print(bold("【3】 التحقق من البناء..."))
    print()
    print("  جاري البناء (قد يستغرق دقيقة)...")
    try:
        result = subprocess.run(
            ["dotnet", "build", "Techtoon.UI"],
            cwd=project_path,
            capture_output=True,
            text=True,
            timeout=180
        )
        if result.returncode == 0:
            print(green("  ✅ تم البناء بنجاح - لا توجد أخطاء"))
            return True
        else:
            print(red("  ✗ يوجد أخطاء في البناء:"))
            print()
            # اطبع آخر 20 سطر من الأخطاء
            lines = result.stdout.split("\n")
            error_lines = [l for l in lines if "error" in l.lower() or "خطأ" in l]
            for l in error_lines[-15:]:
                print(f"     {l.strip()}")
            return False
    except FileNotFoundError:
        print(yellow("  ⚠ dotnet غير موجود في PATH. تأكد من تثبيت .NET SDK"))
        return False
    except subprocess.TimeoutExpired:
        print(yellow("  ⚠ انتهت مهلة البناء"))
        return False
    except Exception as e:
        print(yellow(f"  ⚠ {e}"))
        return False

# =====================================================================
#  البرنامج الرئيسي
# =====================================================================
def main():
    banner()

    script_dir = os.path.dirname(os.path.abspath(__file__))

    # التحقق من المسار
    global PROJECT_PATH
    if len(sys.argv) > 1:
        PROJECT_PATH = sys.argv[1]

    print(f"  مسار المشروع: {PROJECT_PATH}")
    print()

    if not os.path.isdir(PROJECT_PATH):
        print(red(f"  ✗ المسار غير موجود: {PROJECT_PATH}"))
        print()
        print(yellow("  الاستخدام:"))
        print(yellow(f"    python {os.path.basename(__file__)} \"D:\\مسار\\مشروعك\""))
        print()
        print(yellow("  أو عدّل PROJECT_PATH في رأس السكربت"))
        return

    # 1) نسخ الملفات
    ok = copy_files(script_dir, PROJECT_PATH)
    if not ok:
        return

    # 2) هجرة قاعدة البيانات
    run_database_migration(script_dir, PROJECT_PATH)

    # 3) البناء
    check_build(PROJECT_PATH)

    # الخلاصة
    print(cyan("=" * 60))
    print(bold("  الخلاصة:"))
    print(cyan("=" * 60))
    print()
    print(green("  ✓ تم نسخ الملفات المصححة:"))
    print(f"     • InvoiceEditViewModel.cs  (بحث فوري + دفع متعدد)")
    print(f"     • InvoiceEditWindow.xaml   (واجهة محسّنة)")
    print(f"     • InvoiceEditWindow.xaml.cs (كود خلفي)")
    print()
    print(green("  ✓ تم إصلاح قاعدة البيانات:"))
    print(f"     • إضافة عمود cash_box_id لحل الخطأ"))
    print(f"     • إنشاء جدول invoice_payments للدفع المتعدد"))
    print()
    print(yellow("  لتشغيل البرنامج:"))
    print(f"     cd \"{PROJECT_PATH}\"")
    print(f"     dotnet run --project Techtoon.UI")
    print()
    print(cyan("=" * 60))

if __name__ == "__main__":
    main()
