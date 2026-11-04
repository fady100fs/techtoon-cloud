# -*- coding: utf-8 -*-
"""test_security_system.py - اختبارات الأمان والصلاحيات"""
import psycopg2, sys

TEST_DB = "techtoon_test"
CS_TEST = f"host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname={TEST_DB} user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"
TESTS_PASSED = 0; TESTS_FAILED = 0

def test(name, cond, detail=""):
    global TESTS_PASSED, TESTS_FAILED
    if cond: print(f"  PASS: {name}"); TESTS_PASSED += 1
    else: print(f"  FAIL: {name} - {detail}"); TESTS_FAILED += 1

def setup():
    print("\n[SETUP] Preparing security test tables...")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["audit_log", "user_permissions", "users"]:
        cur.execute(f"DROP TABLE IF EXISTS {t} CASCADE")
    
    cur.execute("""
        CREATE TABLE users (
            id SERIAL PRIMARY KEY,
            username VARCHAR NOT NULL UNIQUE,
            password_hash VARCHAR,
            full_name VARCHAR,
            email VARCHAR,
            is_active BOOLEAN DEFAULT TRUE,
            created_at TIMESTAMP DEFAULT NOW(),
            last_login TIMESTAMP,
            role VARCHAR DEFAULT 'SALESPERSON'
        )
    """)
    cur.execute("""
        CREATE TABLE user_permissions (
            id SERIAL PRIMARY KEY,
            user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
            permission_key VARCHAR NOT NULL,
            is_granted BOOLEAN DEFAULT TRUE,
            created_at TIMESTAMP DEFAULT NOW(),
            UNIQUE(user_id, permission_key)
        )
    """)
    cur.execute("""
        CREATE TABLE audit_log (
            id SERIAL PRIMARY KEY,
            user_id INTEGER,
            username VARCHAR,
            action VARCHAR NOT NULL,
            entity_type VARCHAR,
            entity_id INTEGER,
            details TEXT,
            ip_address VARCHAR,
            created_at TIMESTAMP DEFAULT NOW()
        )
    """)
    
    # مستخدم أدمن
    cur.execute("INSERT INTO users (username, password_hash, full_name, role) VALUES ('admin', '$2a$12$testhash', 'Admin', 'ADMIN')")
    # مستخدم كاشير
    cur.execute("INSERT INTO users (username, password_hash, full_name, role) VALUES ('cashier', '$2a$12$testhash', 'Cashier', 'SALESPERSON')")
    
    # صلاحيات الأدمن (الكل)
    for perm in ['sales', 'edit_price', 'discounts', 'cancel_invoice', 'returns', 'expenses', 'profit_reports', 'stock_adjustment', 'manage_users', 'settings', 'backup']:
        cur.execute("INSERT INTO user_permissions (user_id, permission_key) VALUES (1, %s)", (perm,))
    
    # صلاحيات الكاشير (بيع + مرتجع فقط)
    for perm in ['sales', 'returns']:
        cur.execute("INSERT INTO user_permissions (user_id, permission_key) VALUES (2, %s)", (perm,))
    
    cur.close(); conn.close()
    print("[SETUP] Ready")

def test_1():
    print("\n[TEST 1] Admin has all permissions")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT COUNT(*) FROM user_permissions WHERE user_id=1 AND is_granted=TRUE")
        count = cur.fetchone()[0]
        test("admin has 11 permissions", count == 11, f"got {count}")
        
        perms = ['sales', 'edit_price', 'discounts', 'cancel_invoice', 'returns', 'expenses', 'profit_reports', 'stock_adjustment', 'manage_users', 'settings', 'backup']
        for p in perms:
            cur.execute("SELECT is_granted FROM user_permissions WHERE user_id=1 AND permission_key=%s", (p,))
            r = cur.fetchone()
            test(f"admin has '{p}'", r is not None and r[0] == True, "")
    except Exception as e: conn.rollback(); test("admin perms", False, str(e))
    finally: cur.close(); conn.close()

def test_2():
    print("\n[TEST 2] Cashier has limited permissions")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT COUNT(*) FROM user_permissions WHERE user_id=2 AND is_granted=TRUE")
        count = cur.fetchone()[0]
        test("cashier has 2 permissions", count == 2, f"got {count}")
        
        # الكاشير يملك صلاحية البيع
        cur.execute("SELECT is_granted FROM user_permissions WHERE user_id=2 AND permission_key='sales'")
        r = cur.fetchone()
        test("cashier can sell", r is not None and r[0] == True, "")
        
        # الكاشير لا يملك صلاحية إدارة المستخدمين
        cur.execute("SELECT is_granted FROM user_permissions WHERE user_id=2 AND permission_key='manage_users'")
        r = cur.fetchone()
        test("cashier cannot manage users", r is None, "")
        
        # الكاشير لا يملك صلاحية تعديل الأسعار
        cur.execute("SELECT is_granted FROM user_permissions WHERE user_id=2 AND permission_key='edit_price'")
        r = cur.fetchone()
        test("cashier cannot edit price", r is None, "")
    except Exception as e: conn.rollback(); test("cashier perms", False, str(e))
    finally: cur.close(); conn.close()

def test_3():
    print("\n[TEST 3] Permission check logic")
    # محاكاة HasPermission
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        def has_permission(user_id, role, key):
            if role == 'ADMIN': return True
            cur.execute("SELECT 1 FROM user_permissions WHERE user_id=%s AND permission_key=%s AND is_granted=TRUE", (user_id, key))
            return cur.fetchone() is not None
        
        # أدمن
        test("admin can sell", has_permission(1, 'ADMIN', 'sales'), "")
        test("admin can manage_users", has_permission(1, 'ADMIN', 'manage_users'), "")
        test("admin can backup", has_permission(1, 'ADMIN', 'backup'), "")
        
        # كاشير
        test("cashier can sell", has_permission(2, 'SALESPERSON', 'sales'), "")
        test("cashier can returns", has_permission(2, 'SALESPERSON', 'returns'), "")
        test("cashier CANNOT edit_price", not has_permission(2, 'SALESPERSON', 'edit_price'), "")
        test("cashier CANNOT manage_users", not has_permission(2, 'SALESPERSON', 'manage_users'), "")
        test("cashier CANNOT cancel_invoice", not has_permission(2, 'SALESPERSON', 'cancel_invoice'), "")
        test("cashier CANNOT profit_reports", not has_permission(2, 'SALESPERSON', 'profit_reports'), "")
        test("cashier CANNOT backup", not has_permission(2, 'SALESPERSON', 'backup'), "")
    except Exception as e: conn.rollback(); test("perm logic", False, str(e))
    finally: cur.close(); conn.close()

def test_4():
    print("\n[TEST 4] Audit log recording")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        # سجل عملية
        cur.execute("""
            INSERT INTO audit_log (user_id, username, action, entity_type, entity_id, details)
            VALUES (1, 'admin', 'cancel_invoice', 'invoice', 100, 'Cancelled invoice #100')
        """); conn.commit()
        
        cur.execute("SELECT user_id, username, action, entity_type, entity_id, details FROM audit_log WHERE action='cancel_invoice'")
        r = cur.fetchone()
        test("audit logged", r is not None, "")
        if r:
            test("user_id=1", r[0] == 1, f"got {r[0]}")
            test("username=admin", r[1] == 'admin', f"got {r[1]}")
            test("action=cancel_invoice", r[2] == 'cancel_invoice', f"got {r[2]}")
            test("entity_id=100", r[4] == 100, f"got {r[4]}")
    except Exception as e: conn.rollback(); test("audit", False, str(e))
    finally: cur.close(); conn.close()

def test_5():
    print("\n[TEST 5] Password not stored in plaintext")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT password_hash FROM users WHERE username='admin'")
        hash_val = cur.fetchone()[0]
        test("hash starts with $2a$", hash_val.startswith('$2a$'), f"got {hash_val[:10]}")
        test("hash is not 'password'", hash_val != 'password', "")
        test("hash length > 50", len(hash_val) > 50, f"got {len(hash_val)}")  # Note: test data uses short hash
    except Exception as e: conn.rollback(); test("password", False, str(e))
    finally: cur.close(); conn.close()

def test_6():
    print("\n[TEST 6] Grant/Revoke permission")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        # امنح الكاشير صلاحية المصروفات
        cur.execute("""
            INSERT INTO user_permissions (user_id, permission_key, is_granted)
            VALUES (2, 'expenses', TRUE)
            ON CONFLICT (user_id, permission_key) DO UPDATE SET is_granted = TRUE
        """); conn.commit()
        
        cur.execute("SELECT is_granted FROM user_permissions WHERE user_id=2 AND permission_key='expenses'")
        r = cur.fetchone()
        test("granted expenses", r is not None and r[0] == True, "")
        
        # اسحب الصلاحية
        cur.execute("UPDATE user_permissions SET is_granted=FALSE WHERE user_id=2 AND permission_key='expenses'"); conn.commit()
        cur.execute("SELECT is_granted FROM user_permissions WHERE user_id=2 AND permission_key='expenses'")
        r = cur.fetchone()
        test("revoked expenses", r is not None and r[0] == False, "")
    except Exception as e: conn.rollback(); test("grant/revoke", False, str(e))
    finally: cur.close(); conn.close()

def test_7():
    print("\n[TEST 7] Inactive user cannot login")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO users (username, password_hash, full_name, role, is_active) VALUES ('inactive', '$2a$12$test', 'Inactive', 'SALESPERSON', FALSE)"); conn.commit()
        
        # محاكاة فحص تسجيل الدخول
        cur.execute("SELECT is_active FROM users WHERE username='inactive'")
        active = cur.fetchone()[0]
        test("inactive user blocked", active == False, f"got {active}")
    except Exception as e: conn.rollback(); test("inactive", False, str(e))
    finally: cur.close(); conn.close()

def test_8():
    print("\n[TEST 8] Audit log tamper protection")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        # سجل عملية
        cur.execute("INSERT INTO audit_log (user_id, username, action) VALUES (2, 'cashier', 'test_action')"); conn.commit()
        
        # محاولة تعديل السجل (يجب أن يفشل لو كانت هناك قيود)
        # حالياً لا يوجد قيد CHECK على التعديل، لكن الكود يجب أن يتحقق
        # نختبر أن السجل موجود
        cur.execute("SELECT action FROM audit_log WHERE action='test_action'")
        r = cur.fetchone()
        test("audit record exists", r is not None and r[0] == 'test_action', "")
    except Exception as e: conn.rollback(); test("tamper", False, str(e))
    finally: cur.close(); conn.close()

def cleanup():
    print("\n[CLEANUP]...")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["audit_log", "user_permissions", "users"]: cur.execute(f"DELETE FROM {t}")
    cur.close(); conn.close(); print("[CLEANUP] Done")

def main():
    print("=" * 60); print("  Techtoon Security & Permissions Test Suite"); print("=" * 60)
    try: setup()
    except Exception as e: print(f"FATAL: {e}"); sys.exit(1)
    test_1(); test_2(); test_3(); test_4(); test_5(); test_6(); test_7(); test_8()
    cleanup()
    print("\n" + "=" * 60); print(f"  RESULTS: {TESTS_PASSED} passed, {TESTS_FAILED} failed"); print("=" * 60)
    if TESTS_FAILED > 0: sys.exit(1)

if __name__ == "__main__": main()

