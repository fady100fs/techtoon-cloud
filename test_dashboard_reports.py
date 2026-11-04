# -*- coding: utf-8 -*-
"""test_dashboard_reports.py - اختبارات لوحة التحكم والتقارير"""
import psycopg2, sys
from datetime import datetime, timedelta

TEST_DB = "techtoon_test"
CS_TEST = f"host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname={TEST_DB} user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"
TESTS_PASSED = 0; TESTS_FAILED = 0

def test(name, cond, detail=""):
    global TESTS_PASSED, TESTS_FAILED
    if cond: print(f"  PASS: {name}"); TESTS_PASSED += 1
    else: print(f"  FAIL: {name} - {detail}"); TESTS_FAILED += 1

def setup():
    print("\n[SETUP] Preparing dashboard test tables...")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["invoice_lines","invoices","items","parties","payments","expenses","inventory_movements"]:
        cur.execute(f"DROP TABLE IF EXISTS {t} CASCADE")
    
    cur.execute("CREATE TABLE parties (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL, type VARCHAR)")
    cur.execute("CREATE TABLE items (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL, barcode VARCHAR, sell_price DOUBLE PRECISION DEFAULT 0, cost_price DOUBLE PRECISION DEFAULT 0, avg_cost_price DOUBLE PRECISION DEFAULT 0, min_stock DOUBLE PRECISION, opening_quantity DOUBLE PRECISION DEFAULT 0, is_kit BOOLEAN DEFAULT FALSE)")
    cur.execute("CREATE TABLE invoices (id SERIAL PRIMARY KEY, invoice_number VARCHAR NOT NULL UNIQUE, date TIMESTAMP DEFAULT NOW(), party_id INTEGER, type VARCHAR NOT NULL, total DOUBLE PRECISION DEFAULT 0, paid_amount DOUBLE PRECISION DEFAULT 0, status VARCHAR DEFAULT 'draft')")
    cur.execute("CREATE TABLE invoice_lines (id SERIAL PRIMARY KEY, invoice_id INTEGER NOT NULL, item_id INTEGER, quantity DOUBLE PRECISION NOT NULL, price DOUBLE PRECISION NOT NULL, total DOUBLE PRECISION NOT NULL, cost_price DOUBLE PRECISION DEFAULT 0)")
    cur.execute("CREATE TABLE payments (id SERIAL PRIMARY KEY, date TIMESTAMP DEFAULT NOW(), party_id INTEGER, type VARCHAR, amount DOUBLE PRECISION DEFAULT 0)")
    cur.execute("CREATE TABLE expenses (id SERIAL PRIMARY KEY, date TIMESTAMP DEFAULT NOW(), amount DOUBLE PRECISION NOT NULL)")
    cur.execute("CREATE TABLE inventory_movements (id SERIAL PRIMARY KEY, item_id INTEGER NOT NULL, type VARCHAR NOT NULL, quantity DOUBLE PRECISION NOT NULL)")
    
    cur.execute("INSERT INTO parties (name, type) VALUES ('Customer A', 'customer'), ('Supplier B', 'supplier')")
    cur.execute("INSERT INTO items (name, barcode, sell_price, cost_price, avg_cost_price, min_stock, opening_quantity) VALUES ('Item A', 'BC001', 100, 60, 60, 5, 10), ('Item B', 'BC002', 200, 150, 150, 3, 2)")
    
    # فاتورة مبيعات اليوم
    cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, total, paid_amount, status) VALUES ('SAL-001', NOW(), 1, 'sales', 500, 500, 'paid')")
    cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total, cost_price) VALUES (1, 1, 5, 100, 500, 60)")
    
    # فاتورة غير مدفوعة
    cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, total, paid_amount, status) VALUES ('SAL-002', NOW(), 1, 'sales', 300, 0, 'unpaid')")
    
    # دفعة قبض
    cur.execute("INSERT INTO payments (party_id, type, amount) VALUES (1, 'receipt', 500)")
    
    # مصروف
    cur.execute("INSERT INTO expenses (date, amount) VALUES (NOW(), 100)")
    
    # حركة مخزون
    cur.execute("INSERT INTO inventory_movements (item_id, type, quantity) VALUES (1, 'out', 3)")
    cur.execute("INSERT INTO inventory_movements (item_id, type, quantity) VALUES (2, 'out', 1)")
    
    cur.close(); conn.close()
    print("[SETUP] Ready")

def test_1():
    print("\n[TEST 1] Today sales")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT COALESCE(SUM(total),0), COUNT(*) FROM invoices WHERE type='sales' AND DATE(date)=CURRENT_DATE")
        total, count = cur.fetchone()
        test("today sales=500", float(total) == 500, f"got {total}")
        test("today count=2", count == 2, f"got {count}")
    except Exception as e: test("today sales", False, str(e))
    finally: cur.close(); conn.close()

def test_2():
    print("\n[TEST 2] Collections")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT COALESCE(SUM(amount),0) FROM payments WHERE type='receipt'")
        total = float(cur.fetchone()[0])
        test("collections=500", total == 500, f"got {total}")
    except Exception as e: test("collections", False, str(e))
    finally: cur.close(); conn.close()

def test_3():
    print("\n[TEST 3] Receivables")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT COALESCE(SUM(total-paid_amount),0) FROM invoices WHERE type='sales' AND paid_amount<total")
        total = float(cur.fetchone()[0])
        test("receivables=300", total == 300, f"got {total}")
    except Exception as e: test("receivables", False, str(e))
    finally: cur.close(); conn.close()

def test_4():
    print("\n[TEST 4] Expenses")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT COALESCE(SUM(amount),0) FROM expenses WHERE DATE(date)=CURRENT_DATE")
        total = float(cur.fetchone()[0])
        test("expenses=100", total == 100, f"got {total}")
    except Exception as e: test("expenses", False, str(e))
    finally: cur.close(); conn.close()

def test_5():
    print("\n[TEST 5] Stock value")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("""
            SELECT COALESCE(SUM(
                (i.opening_quantity + COALESCE((SELECT SUM(CASE WHEN type IN ('in','opening') THEN quantity WHEN type='out' THEN -quantity WHEN type='adjustment' THEN quantity ELSE 0 END) FROM inventory_movements WHERE item_id=i.id), 0)) * COALESCE(i.avg_cost_price, i.cost_price, 0)
            ), 0)
            FROM items i WHERE i.is_kit IS NOT TRUE
        """)
        val = float(cur.fetchone()[0])
        # Item A: stock=7, cost=60 -> 420
        # Item B: stock=1, cost=150 -> 150
        # Total = 570
        test("stock value=570", val == 570, f"got {val}")
    except Exception as e: test("stock value", False, str(e))
    finally: cur.close(); conn.close()

def test_6():
    print("\n[TEST 6] Gross profit")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("""
            SELECT COALESCE(SUM(il.total),0) - COALESCE(SUM(il.quantity * il.cost_price),0)
            FROM invoice_lines il JOIN invoices i ON il.invoice_id = i.id
            WHERE i.type='sales'
        """)
        profit = float(cur.fetchone()[0])
        # Sales=500, Cost=5*60=300, Profit=200
        test("gross profit=200", profit == 200, f"got {profit}")
    except Exception as e: test("profit", False, str(e))
    finally: cur.close(); conn.close()

def test_7():
    print("\n[TEST 7] Low stock items")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("""
            SELECT i.name, i.min_stock,
                i.opening_quantity + COALESCE((SELECT SUM(CASE WHEN type IN ('in','opening') THEN quantity WHEN type='out' THEN -quantity WHEN type='adjustment' THEN quantity ELSE 0 END) FROM inventory_movements WHERE item_id=i.id), 0)
            FROM items i WHERE i.min_stock IS NOT NULL AND i.min_stock > 0
        """)
        low = []
        for r in cur.fetchall():
            name, min_s, stock = r[0], float(r[1]), float(r[2])
            if stock <= min_s:
                low.append(name)
        test("Item B is low", 'Item B' in low, f"got {low}")
        test("Item A not low", 'Item A' not in low, f"got {low}")
    except Exception as e: test("low stock", False, str(e))
    finally: cur.close(); conn.close()

def test_8():
    print("\n[TEST 8] Sales report with date filter")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        from_date = datetime.now() - timedelta(days=30)
        to_date = datetime.now() + timedelta(days=1)
        cur.execute("SELECT COUNT(*), COALESCE(SUM(total),0) FROM invoices WHERE type='sales' AND date BETWEEN %s AND %s", (from_date, to_date))
        count, total = cur.fetchone()
        test("report count=2", count == 2, f"got {count}")
        test("report total=800", float(total) == 800, f"got {total}")
    except Exception as e: test("report", False, str(e))
    finally: cur.close(); conn.close()

def test_9():
    print("\n[TEST 9] Top items")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("""
            SELECT i.name, SUM(il.quantity), SUM(il.total)
            FROM invoice_lines il JOIN invoices inv ON il.invoice_id=inv.id JOIN items i ON il.item_id=i.id
            WHERE inv.type='sales' GROUP BY i.name ORDER BY SUM(il.total) DESC LIMIT 5
        """)
        rows = cur.fetchall()
        test("top items count=1", len(rows) == 1, f"got {len(rows)}")
        if rows:
            test("top item=Item A", rows[0][0] == 'Item A', f"got {rows[0][0]}")
            test("top qty=5", float(rows[0][1]) == 5, f"got {rows[0][1]}")
    except Exception as e: test("top items", False, str(e))
    finally: cur.close(); conn.close()

def cleanup():
    print("\n[CLEANUP]...")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["invoice_lines","invoices","items","parties","payments","expenses","inventory_movements"]:
        cur.execute(f"DELETE FROM {t}")
    cur.close(); conn.close(); print("[CLEANUP] Done")

def main():
    print("=" * 60); print("  Techtoon Dashboard & Reports Test Suite"); print("=" * 60)
    try: setup()
    except Exception as e: print(f"FATAL: {e}"); sys.exit(1)
    test_1(); test_2(); test_3(); test_4(); test_5(); test_6(); test_7(); test_8(); test_9()
    cleanup()
    print("\n" + "=" * 60); print(f"  RESULTS: {TESTS_PASSED} passed, {TESTS_FAILED} failed"); print("=" * 60)
    if TESTS_FAILED > 0: sys.exit(1)

if __name__ == "__main__": main()
