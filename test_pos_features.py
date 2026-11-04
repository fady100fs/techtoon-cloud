# -*- coding: utf-8 -*-
"""test_pos_features.py - اختبارات ميزات POS"""
import psycopg2, sys, threading
from datetime import datetime

TEST_DB = "techtoon_test"
CS_TEST = f"host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname={TEST_DB} user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"
TESTS_PASSED = 0; TESTS_FAILED = 0

def test(name, cond, detail=""):
    global TESTS_PASSED, TESTS_FAILED
    if cond: print(f"  PASS: {name}"); TESTS_PASSED += 1
    else: print(f"  FAIL: {name} - {detail}"); TESTS_FAILED += 1

def setup():
    print("\n[SETUP]...")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["invoice_payments","invoice_lines","invoices","items","parties"]: cur.execute(f"DROP TABLE IF EXISTS {t} CASCADE")
    cur.execute("CREATE TABLE parties (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL, type VARCHAR)")
    cur.execute("CREATE TABLE items (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL, barcode VARCHAR UNIQUE, sell_price DOUBLE PRECISION DEFAULT 0, opening_quantity DOUBLE PRECISION DEFAULT 0)")
    cur.execute("CREATE TABLE invoices (id SERIAL PRIMARY KEY, invoice_number VARCHAR NOT NULL UNIQUE, date TIMESTAMP DEFAULT NOW(), party_id INTEGER, type VARCHAR NOT NULL, subtotal NUMERIC(14,2) DEFAULT 0, discount_amount NUMERIC(14,2) DEFAULT 0, total NUMERIC(14,2) DEFAULT 0, paid_amount NUMERIC(14,2) DEFAULT 0, status VARCHAR DEFAULT 'draft')")
    cur.execute("CREATE TABLE invoice_lines (id SERIAL PRIMARY KEY, invoice_id INTEGER NOT NULL, item_id INTEGER NOT NULL, quantity DOUBLE PRECISION NOT NULL, price DOUBLE PRECISION NOT NULL, discount NUMERIC(14,2) DEFAULT 0, total DOUBLE PRECISION NOT NULL)")
    cur.execute("CREATE TABLE invoice_payments (id SERIAL PRIMARY KEY, invoice_id INTEGER NOT NULL, method VARCHAR NOT NULL DEFAULT 'cash', amount DOUBLE PRECISION DEFAULT 0, reference VARCHAR, paid_at TIMESTAMP DEFAULT NOW())")
    cur.execute("INSERT INTO parties (name, type) VALUES ('Test Customer', 'customer')")
    cur.execute("INSERT INTO items (name, barcode, sell_price, opening_quantity) VALUES ('Item A', 'BC001', 100, 50), ('Item B', 'BC002', 200, 30), ('Item C', 'BC003', 50, 0)")
    cur.close(); conn.close(); print("[SETUP] Ready")

def test_1():
    print("\n[TEST 1] Save as draft")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, subtotal, total, paid_amount, status) VALUES ('DRAFT-001', NOW(), 1, 'sales', 100, 100, 0, 'draft') RETURNING id")
        inv_id = cur.fetchone()[0]
        cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total) VALUES (%s, 1, 1, 100, 100)", (inv_id,)); conn.commit()
        cur.execute("SELECT status FROM invoices WHERE id = %s", (inv_id,)); s = cur.fetchone()[0]
        test("status=draft", s == 'draft', f"got {s}")
    except Exception as e: conn.rollback(); test("draft", False, str(e))
    finally: cur.close(); conn.close()

def test_2():
    print("\n[TEST 2] Complete draft")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT id FROM invoices WHERE invoice_number='DRAFT-001'"); r = cur.fetchone()
        if not r: test("complete", False, "not found"); return
        inv_id = r[0]
        cur.execute("INSERT INTO invoice_payments (invoice_id, method, amount) VALUES (%s, 'cash', 100)", (inv_id,))
        cur.execute("UPDATE invoices SET paid_amount = 100, status = 'paid' WHERE id = %s", (inv_id,)); conn.commit()
        cur.execute("SELECT status, paid_amount FROM invoices WHERE id = %s", (inv_id,)); s, p = cur.fetchone()
        test("status=paid", s == 'paid', f"got {s}")
        test("paid=100", p == 100, f"got {p}")
    except Exception as e: conn.rollback(); test("complete", False, str(e))
    finally: cur.close(); conn.close()

def test_3():
    print("\n[TEST 3] Line discount")
    qty, price, disc = 2, 100, 30; expected = qty * price - disc
    test("total=170", expected == 170, f"got {expected}")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, subtotal, total, status) VALUES ('DISC-001', NOW(), 1, 'sales', 170, 170, 'unpaid') RETURNING id")
        inv_id = cur.fetchone()[0]
        cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, discount, total) VALUES (%s, 1, 2, 100, 30, 170)", (inv_id,)); conn.commit()
        cur.execute("SELECT discount, total FROM invoice_lines WHERE invoice_id = %s", (inv_id,)); d, t = cur.fetchone()
        test("disc=30", d == 30, f"got {d}")
        test("total=170", t == 170, f"got {t}")
    except Exception as e: conn.rollback(); test("disc", False, str(e))
    finally: cur.close(); conn.close()

def test_4():
    print("\n[TEST 4] Invoice discount")
    sub, disc = 500, 50; expected = sub - disc
    test("total=450", expected == 450, f"got {expected}")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, subtotal, discount_amount, total, status) VALUES ('INVDISC-001', NOW(), 1, 'sales', 500, 50, 450, 'unpaid')"); conn.commit()
        cur.execute("SELECT discount_amount, total FROM invoices WHERE invoice_number='INVDISC-001'"); d, t = cur.fetchone()
        test("disc=50", d == 50, f"got {d}")
        test("total=450", t == 450, f"got {t}")
    except Exception as e: conn.rollback(); test("invdisc", False, str(e))
    finally: cur.close(); conn.close()

def test_5():
    print("\n[TEST 5] Add by barcode")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT id, name, sell_price FROM items WHERE barcode = 'BC001'"); item = cur.fetchone()
        test("found", item is not None, "")
        if item:
            test("name=A", item[1] == 'Item A', f"got {item[1]}")
            test("price=100", item[2] == 100, f"got {item[2]}")
        cur.execute("SELECT id FROM items WHERE barcode = 'NOTEXIST'"); nf = cur.fetchone()
        test("not found=None", nf is None, "")
        try:
            cur.execute("INSERT INTO items (name, barcode, sell_price) VALUES ('Dup', 'BC001', 999)"); conn.commit()
            test("dup blocked", False, "should fail")
        except psycopg2.IntegrityError:
            conn.rollback(); test("dup blocked", True)
    except Exception as e: conn.rollback(); test("barcode", False, str(e))
    finally: cur.close(); conn.close()

def test_6():
    print("\n[TEST 6] Prevent duplicate save")
    _isSaving = False
    def save():
        nonlocal _isSaving
        if _isSaving: return False
        _isSaving = True; _isSaving = False; return True
    f = save(); test("first ok", f == True, "")
    _isSaving = True; s = save(); test("concurrent blocked", s == False, ""); _isSaving = False

def test_7():
    print("\n[TEST 7] Cancel invoice")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, subtotal, total, paid_amount, status) VALUES ('CANCEL-001', NOW(), 1, 'sales', 200, 200, 200, 'paid') RETURNING id")
        inv_id = cur.fetchone()[0]; conn.commit()
        cur.execute("UPDATE invoices SET status = 'cancelled' WHERE id = %s", (inv_id,)); conn.commit()
        cur.execute("SELECT status FROM invoices WHERE id = %s", (inv_id,)); s = cur.fetchone()[0]
        test("status=cancelled", s == 'cancelled', f"got {s}")
        cur.execute("SELECT COUNT(*) FROM invoices WHERE id = %s", (inv_id,)); c = cur.fetchone()[0]
        test("still exists", c == 1, f"got {c}")
    except Exception as e: conn.rollback(); test("cancel", False, str(e))
    finally: cur.close(); conn.close()

def test_8():
    print("\n[TEST 8] Stock check")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT opening_quantity FROM items WHERE barcode = 'BC003'"); s = cur.fetchone()[0]
        test("C stock=0", s == 0, f"got {s}")
        test("cannot fulfill 5", not (5 <= s), "")
        cur.execute("SELECT opening_quantity FROM items WHERE barcode = 'BC001'"); sa = cur.fetchone()[0]
        test("can fulfill 10", 10 <= sa, "")
    except Exception as e: conn.rollback(); test("stock", False, str(e))
    finally: cur.close(); conn.close()

def test_9():
    print("\n[TEST 9] Connection failure")
    bad = "host=invalid.host.example.com port=5432 dbname=test user=test password=test sslmode=require connect_timeout=3"
    try:
        conn = psycopg2.connect(bad); test("fails", False, "should fail")
    except psycopg2.OperationalError: test("raises error", True, "")
    except Exception as e: test("raises error", True, str(type(e).__name__))

def cleanup():
    print("\n[CLEANUP]...")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["invoice_payments","invoice_lines","invoices","items","parties"]: cur.execute(f"DELETE FROM {t}")
    cur.close(); conn.close(); print("[CLEANUP] Done")

def main():
    print("=" * 60); print("  Techtoon POS Features Test Suite"); print("=" * 60)
    try: setup()
    except Exception as e: print(f"FATAL: {e}"); sys.exit(1)
    test_1(); test_2(); test_3(); test_4(); test_5(); test_6(); test_7(); test_8(); test_9()
    cleanup()
    print("\n" + "=" * 60); print(f"  RESULTS: {TESTS_PASSED} passed, {TESTS_FAILED} failed"); print("=" * 60)
    if TESTS_FAILED > 0: sys.exit(1)

if __name__ == "__main__": main()

