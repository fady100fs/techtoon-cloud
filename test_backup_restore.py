# -*- coding: utf-8 -*-
"""test_backup_restore.py - اختبارات النسخ الاحتياطي والاستعادة"""
import psycopg2, sys, os, shutil
from datetime import datetime

TEST_DB = "techtoon_test"
PROD_DB = "neondb"
CS_BASE = "host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"
CS_PROD = f"{CS_BASE} dbname={PROD_DB}"
CS_TEST = f"{CS_BASE} dbname={TEST_DB}"
TESTS_PASSED = 0; TESTS_FAILED = 0

def test(name, cond, detail=""):
    global TESTS_PASSED, TESTS_FAILED
    if cond: print(f"  PASS: {name}"); TESTS_PASSED += 1
    else: print(f"  FAIL: {name} - {detail}"); TESTS_FAILED += 1

def setup():
    print("\n[SETUP] Preparing test tables...")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["invoice_lines","invoices","items","parties"]: cur.execute(f"DROP TABLE IF EXISTS {t} CASCADE")
    cur.execute("CREATE TABLE parties (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL)")
    cur.execute("CREATE TABLE items (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL, sell_price DOUBLE PRECISION DEFAULT 0, opening_quantity DOUBLE PRECISION DEFAULT 0)")
    cur.execute("CREATE TABLE invoices (id SERIAL PRIMARY KEY, invoice_number VARCHAR NOT NULL UNIQUE, date TIMESTAMP DEFAULT NOW(), party_id INTEGER, type VARCHAR NOT NULL, total DOUBLE PRECISION DEFAULT 0, paid_amount DOUBLE PRECISION DEFAULT 0, status VARCHAR DEFAULT 'draft')")
    cur.execute("CREATE TABLE invoice_lines (id SERIAL PRIMARY KEY, invoice_id INTEGER NOT NULL, item_id INTEGER, quantity DOUBLE PRECISION NOT NULL, price DOUBLE PRECISION NOT NULL, total DOUBLE PRECISION NOT NULL)")
    cur.execute("INSERT INTO parties (name) VALUES ('Test Customer')")
    cur.execute("INSERT INTO items (name, sell_price, opening_quantity) VALUES ('Item A', 100, 50), ('Item B', 200, 30)")
    cur.execute("INSERT INTO invoices (invoice_number, party_id, type, total, paid_amount, status) VALUES ('SAL-001', 1, 'sales', 300, 300, 'paid')")
    cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total) VALUES (1, 1, 3, 100, 300)")
    cur.close(); conn.close()
    print("[SETUP] Ready")

def create_backup_sql():
    """محاكاة BackupService.CreateBackup على قاعدة الاختبار"""
    conn = psycopg2.connect(CS_TEST); cur = conn.cursor()
    sql_lines = ["-- Techtoon Backup", f"-- {datetime.now()}", ""]
    
    # احصل على الجداول
    cur.execute("SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE' ORDER BY table_name")
    tables = [r[0] for r in cur.fetchall()]
    
    for table in tables:
        sql_lines.append(f"-- Table: {table}")
        # CREATE TABLE
        cur.execute("SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns WHERE table_schema='public' AND table_name=%s ORDER BY ordinal_position", (table,))
        cols = cur.fetchall()
        sql_lines.append(f'CREATE TABLE IF NOT EXISTS "{table}" (')
        for col in cols:
            nullable = "NULL" if col[2] == "YES" else "NOT NULL"
            def_val = f"DEFAULT {col[3]}" if col[3] else ""
            sql_lines.append(f'  "{col[0]}" {col[1]} {nullable} {def_val},')
        sql_lines.append(");")
        
        # INSERT INTO
        col_names = ", ".join(f'"{c[0]}"' for c in cols)
        cur.execute(f'SELECT * FROM "{table}"')
        rows = cur.fetchall()
        for row in rows:
            values = []
            for i, val in enumerate(row):
                if val is None: values.append("NULL")
                elif isinstance(val, str): values.append(f"'{val.replace(chr(39), chr(39)+chr(39))}'")
                elif isinstance(val, (int, float)): values.append(str(val))
                else: values.append(f"'{str(val)}'")
            sql_lines.append(f'INSERT INTO "{table}" ({col_names}) VALUES ({", ".join(values)});')
        sql_lines.append("")
    
    cur.close(); conn.close()
    return "\n".join(sql_lines)

def test_1():
    print("\n[TEST 1] Create backup")
    backup_dir = os.path.join(os.path.dirname(__file__), "test_backups")
    os.makedirs(backup_dir, exist_ok=True)
    backup_file = os.path.join(backup_dir, f"test_backup_{datetime.now().strftime('%Y%m%d_%H%M%S')}.sql")
    
    sql_content = create_backup_sql()
    with open(backup_file, "w", encoding="utf-8") as f:
        f.write(sql_content)
    
    test("backup file created", os.path.exists(backup_file), "")
    test("file size > 100", os.path.getsize(backup_file) > 100, f"size={os.path.getsize(backup_file)}")
    test("contains CREATE TABLE", "CREATE TABLE" in sql_content, "")
    test("contains INSERT INTO", "INSERT INTO" in sql_content, "")

def test_2():
    print("\n[TEST 2] Verify backup integrity")
    backup_dir = os.path.join(os.path.dirname(__file__), "test_backups")
    files = [f for f in os.listdir(backup_dir) if f.endswith(".sql")] if os.path.exists(backup_dir) else []
    if not files: test("verify", False, "no backup files"); return
    
    latest = os.path.join(backup_dir, sorted(files)[-1])
    content = open(latest, "r", encoding="utf-8").read()
    test("has CREATE TABLE", "CREATE TABLE" in content, "")
    test("has INSERT INTO", "INSERT INTO" in content, "")
    test("has parties table", '"parties"' in content, "")
    test("has items table", '"items"' in content, "")
    test("has invoices table", '"invoices"' in content, "")

def test_3():
    print("\n[TEST 3] Restore to test database")
    backup_dir = os.path.join(os.path.dirname(__file__), "test_backups")
    files = [f for f in os.listdir(backup_dir) if f.endswith(".sql")] if os.path.exists(backup_dir) else []
    if not files: test("restore", False, "no backup"); return
    
    latest = os.path.join(backup_dir, sorted(files)[-1])
    sql_content = open(latest, "r", encoding="utf-8").read()
    
    # احذف بيانات الاختبار ثم استعد
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["invoice_lines","invoices","items","parties"]: cur.execute(f"DELETE FROM {t}")
    
    # نفذ SQL (بشكل مبسط - نفذ كل جملة INSERT)
    insert_lines = [l for l in sql_content.split("\n") if l.startswith("INSERT INTO")]
    for line in insert_lines:
        try: cur.execute(line)
        except: pass
    
    # تحقق
    cur.execute("SELECT COUNT(*) FROM parties"); p = cur.fetchone()[0]
    cur.execute("SELECT COUNT(*) FROM items"); i = cur.fetchone()[0]
    cur.execute("SELECT COUNT(*) FROM invoices"); inv = cur.fetchone()[0]
    test("parties restored", p > 0, f"got {p}")
    test("items restored", i > 0, f"got {i}")
    test("invoices restored", inv > 0, f"got {inv}")
    
    cur.close(); conn.close()

def test_4():
    print("\n[TEST 4] Backup does not contain passwords")
    backup_dir = os.path.join(os.path.dirname(__file__), "test_backups")
    files = [f for f in os.listdir(backup_dir) if f.endswith(".sql")] if os.path.exists(backup_dir) else []
    if not files: test("no passwords", False, "no file"); return
    
    latest = os.path.join(backup_dir, sorted(files)[-1])
    content = open(latest, "r", encoding="utf-8").read()
    test("no 'password' in file", "password" not in content.lower(), "")
    test("no 'npg_' in file", "npg_" not in content, "")
    test("no 'sslmode' in file", "sslmode" not in content.lower(), "")

def test_5():
    print("\n[TEST 5] Retention policy (clean old)")
    backup_dir = os.path.join(os.path.dirname(__file__), "test_backups")
    # أنشئ ملف قديم
    old_file = os.path.join(backup_dir, "techtoon_backup_20200101_000000.sql")
    with open(old_file, "w") as f: f.write("-- old backup")
    # اضبط تاريخ الإنشاء للماضي
    import time
    old_time = time.time() - (35 * 24 * 3600)  # 35 يوم
    os.utime(old_file, (old_time, old_time))
    
    # محاكاة CleanOldBackups(30 days)
    import datetime as dt
    cutoff = dt.datetime.now() - dt.timedelta(days=30)
    deleted = 0
    for f in os.listdir(backup_dir):
        fp = os.path.join(backup_dir, f)
        if f.startswith("techtoon_backup_") and f.endswith(".sql"):
            if dt.datetime.fromtimestamp(os.path.getctime(fp)) < cutoff:
                os.remove(fp); deleted += 1
    
    test("old file deleted", not os.path.exists(old_file), "")
    test("deleted count >= 1", deleted >= 1, f"deleted {deleted}")

def cleanup():
    print("\n[CLEANUP]...")
    backup_dir = os.path.join(os.path.dirname(__file__), "test_backups")
    if os.path.exists(backup_dir): shutil.rmtree(backup_dir)
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["invoice_lines","invoices","items","parties"]: cur.execute(f"DELETE FROM {t}")
    cur.close(); conn.close()
    print("[CLEANUP] Done")

def main():
    print("=" * 60); print("  Techtoon Backup & Restore Test Suite"); print("=" * 60)
    try: setup()
    except Exception as e: print(f"FATAL: {e}"); sys.exit(1)
    test_1(); test_2(); test_3(); test_4(); test_5()
    cleanup()
    print("\n" + "=" * 60); print(f"  RESULTS: {TESTS_PASSED} passed, {TESTS_FAILED} failed"); print("=" * 60)
    if TESTS_FAILED > 0: sys.exit(1)

if __name__ == "__main__": main()
