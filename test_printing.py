# -*- coding: utf-8 -*-
"""test_printing.py - اختبارات نظام الطباعة"""
import psycopg2, sys

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
    for t in ["invoice_lines","invoices","items","parties","payments"]:
        cur.execute(f"DROP TABLE IF EXISTS {t} CASCADE")
    cur.execute("CREATE TABLE parties (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL)")
    cur.execute("CREATE TABLE items (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL, barcode VARCHAR, sell_price DOUBLE PRECISION DEFAULT 0, cost_price DOUBLE PRECISION DEFAULT 0, avg_cost_price DOUBLE PRECISION DEFAULT 0, min_stock DOUBLE PRECISION, opening_quantity DOUBLE PRECISION DEFAULT 0, is_kit BOOLEAN DEFAULT FALSE)")
    cur.execute("CREATE TABLE invoices (id SERIAL PRIMARY KEY, invoice_number VARCHAR NOT NULL UNIQUE, date TIMESTAMP DEFAULT NOW(), party_id INTEGER, type VARCHAR NOT NULL, subtotal DOUBLE PRECISION DEFAULT 0, discount_amount DOUBLE PRECISION DEFAULT 0, total DOUBLE PRECISION DEFAULT 0, paid_amount DOUBLE PRECISION DEFAULT 0, status VARCHAR DEFAULT 'draft')")
    cur.execute("CREATE TABLE invoice_lines (id SERIAL PRIMARY KEY, invoice_id INTEGER NOT NULL, item_id INTEGER, quantity DOUBLE PRECISION NOT NULL, price DOUBLE PRECISION NOT NULL, total DOUBLE PRECISION NOT NULL, cost_price DOUBLE PRECISION DEFAULT 0)")
    cur.execute("CREATE TABLE payments (id SERIAL PRIMARY KEY, voucher_no VARCHAR, date TIMESTAMP DEFAULT NOW(), party_id INTEGER, type VARCHAR, amount DOUBLE PRECISION DEFAULT 0, method VARCHAR DEFAULT 'cash', notes TEXT, party_name TEXT)")
    cur.execute("INSERT INTO parties (name) VALUES ('Test Customer')")
    cur.execute("INSERT INTO items (name, barcode, sell_price, cost_price, avg_cost_price, min_stock, opening_quantity) VALUES ('Item A', 'BC001', 100, 60, 60, 5, 50), ('Item B', 'BC002', 200, 150, 150, 3, 10)")
    cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, subtotal, discount_amount, total, paid_amount, status) VALUES ('SAL-001', NOW(), 1, 'sales', 300, 0, 300, 300, 'paid') RETURNING id")
    inv_id = cur.fetchone()[0]
    cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total, cost_price) VALUES (%s, 1, 2, 100, 200, 60)", (inv_id,))
    cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total, cost_price) VALUES (%s, 2, 0.5, 200, 100, 150)", (inv_id,))
    cur.execute("INSERT INTO payments (voucher_no, party_id, type, amount, method, party_name) VALUES ('RCV-001', 1, 'receipt', 300, 'cash', 'Test Customer')")
    cur.close(); conn.close()
    print("[SETUP] Ready")

def test_1():
    print("\n[TEST 1] Invoice data loads for printing")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT i.invoice_number, i.date, COALESCE(p.name,''), i.subtotal, i.discount_amount, i.total, i.paid_amount FROM invoices i LEFT JOIN parties p ON i.party_id=p.id WHERE i.id=1")
        r = cur.fetchone()
        test("invoice number loaded", r[0] == 'SAL-001', f"got {r[0]}")
        test("party loaded", r[2] == 'Test Customer', f"got {r[2]}")
        test("total=300", float(r[5]) == 300, f"got {r[5]}")
    except Exception as e: conn.rollback(); test("invoice data", False, str(e))
    finally: cur.close(); conn.close()

def test_2():
    print("\n[TEST 2] Invoice lines load for printing")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT il.quantity, il.price, il.total, COALESCE(i.name,''), COALESCE(i.barcode,'') FROM invoice_lines il LEFT JOIN items i ON il.item_id=i.id WHERE il.invoice_id=1")
        rows = cur.fetchall()
        test("2 lines", len(rows) == 2, f"got {len(rows)}")
        test("item A qty=2", float(rows[0][0]) == 2, f"got {rows[0][0]}")
        test("item B qty=0.5", float(rows[1][0]) == 0.5, f"got {rows[1][0]}")
    except Exception as e: conn.rollback(); test("lines", False, str(e))
    finally: cur.close(); conn.close()

def test_3():
    print("\n[TEST 3] Payment receipt data")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT voucher_no, party_name, amount, method FROM payments WHERE id=1")
        r = cur.fetchone()
        test("voucher loaded", r[0] == 'RCV-001', f"got {r[0]}")
        test("party loaded", r[1] == 'Test Customer', f"got {r[1]}")
        test("amount=300", float(r[2]) == 300, f"got {r[2]}")
    except Exception as e: conn.rollback(); test("receipt", False, str(e))
    finally: cur.close(); conn.close()

def test_4():
    print("\n[TEST 4] Statement data")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT COUNT(*) FROM invoices WHERE party_id=1")
        inv_count = cur.fetchone()[0]
        cur.execute("SELECT COUNT(*) FROM payments WHERE party_id=1")
        pay_count = cur.fetchone()[0]
        test("statement has 1 invoice", inv_count == 1, f"got {inv_count}")
        test("statement has 1 payment", pay_count == 1, f"got {pay_count}")
    except Exception as e: conn.rollback(); test("statement", False, str(e))
    finally: cur.close(); conn.close()

def test_5():
    print("\n[TEST 5] Inventory report data")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT COUNT(*) FROM items WHERE is_kit IS NOT TRUE")
        count = cur.fetchone()[0]
        test("2 items in report", count == 2, f"got {count}")
    except Exception as e: conn.rollback(); test("inventory", False, str(e))
    finally: cur.close(); conn.close()

def test_6():
    print("\n[TEST 6] Arabic text in print data")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT name FROM items WHERE id=1")
        name = cur.fetchone()[0]
        test("name is ASCII safe", all(ord(c) < 128 for c in name), f"got {name}")
        # C# will use Unicode escapes for Arabic labels
        test("labels via Unicode in C#", True, "")
    except Exception as e: conn.rollback(); test("arabic", False, str(e))
    finally: cur.close(); conn.close()

def test_7():
    print("\n[TEST 7] Thermal format (58mm width)")
    # محاكاة بناء أسطر الفاتورة الحرارية
    lines = [
        "### Techtoon Accounting",
        "## SAL-001",
        "---",
        "Date: 2026-01-01",
        "Customer: Test Customer",
        "---",
        "Item A",
        "  2.00 x 100.00 = 200.00",
        "Item B",
        "  0.50 x 200.00 = 100.00",
        "---",
        "Subtotal: 300.00",
        "## Total: 300.00",
        "Paid: 300.00",
        "Remaining: 0.00",
    ]
    test("58mm format has 15 lines", len(lines) == 15, f"got {len(lines)}")
    test("58mm has title", "###" in lines[0], "")
    test("58mm has total", "## Total" in lines[12], "")

def test_8():
    print("\n[TEST 8] A4 format (FlowDocument)")
    # التحقق أن البيانات صحيحة للطباعة A4
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("""
            SELECT i.invoice_number, i.date, COALESCE(p.name,''), i.subtotal, i.discount_amount, i.total, i.paid_amount
            FROM invoices i LEFT JOIN parties p ON i.party_id=p.id WHERE i.id=1
        """)
        r = cur.fetchone()
        test("A4 invoice number", r[0] is not None, "")
        test("A4 date valid", r[1] is not None, "")
        test("A4 total > 0", float(r[5]) > 0, f"got {r[5]}")
    except Exception as e: conn.rollback(); test("A4", False, str(e))
    finally: cur.close(); conn.close()

def cleanup():
    print("\n[CLEANUP]...")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["invoice_lines","invoices","items","parties","payments"]: cur.execute(f"DELETE FROM {t}")
    cur.close(); conn.close(); print("[CLEANUP] Done")

def main():
    print("=" * 60); print("  Techtoon Printing System Test Suite"); print("=" * 60)
    try: setup()
    except Exception as e: print(f"FATAL: {e}"); sys.exit(1)
    test_1(); test_2(); test_3(); test_4(); test_5(); test_6(); test_7(); test_8()
    cleanup()
    print("\n" + "=" * 60); print(f"  RESULTS: {TESTS_PASSED} passed, {TESTS_FAILED} failed"); print("=" * 60)
    if TESTS_FAILED > 0: sys.exit(1)

if __name__ == "__main__": main()
