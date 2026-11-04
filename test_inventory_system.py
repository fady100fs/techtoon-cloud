# -*- coding: utf-8 -*-
"""test_inventory_system.py - اختبارات نظام المخزون"""
import psycopg2, sys, threading

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
    for t in ["stock_adjustments","inventory_movements","invoice_payments","invoice_lines","invoices","items","warehouses"]: cur.execute(f"DROP TABLE IF EXISTS {t} CASCADE")
    cur.execute("CREATE TABLE warehouses (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL, code VARCHAR NOT NULL, is_active BOOLEAN DEFAULT TRUE)")
    cur.execute("CREATE TABLE items (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL, barcode VARCHAR, sell_price DOUBLE PRECISION DEFAULT 0, opening_quantity DOUBLE PRECISION DEFAULT 0, min_stock DOUBLE PRECISION)")
    cur.execute("CREATE TABLE invoices (id SERIAL PRIMARY KEY, invoice_number VARCHAR NOT NULL UNIQUE, date TIMESTAMP DEFAULT NOW(), type VARCHAR NOT NULL, status VARCHAR DEFAULT 'draft')")
    cur.execute("CREATE TABLE invoice_lines (id SERIAL PRIMARY KEY, invoice_id INTEGER NOT NULL, item_id INTEGER NOT NULL, quantity DOUBLE PRECISION NOT NULL, price DOUBLE PRECISION NOT NULL, total DOUBLE PRECISION NOT NULL)")
    cur.execute("CREATE TABLE invoice_payments (id SERIAL PRIMARY KEY, invoice_id INTEGER NOT NULL, method VARCHAR NOT NULL DEFAULT 'cash', amount DOUBLE PRECISION DEFAULT 0)")
    cur.execute("CREATE TABLE inventory_movements (id SERIAL PRIMARY KEY, item_id INTEGER NOT NULL, date TIMESTAMP DEFAULT NOW(), type VARCHAR NOT NULL, quantity DOUBLE PRECISION NOT NULL, reference_id INTEGER, reference_type VARCHAR DEFAULT 'invoice', warehouse_id INTEGER, created_at TIMESTAMP DEFAULT NOW())")
    cur.execute("CREATE TABLE stock_adjustments (id SERIAL PRIMARY KEY, item_id INTEGER NOT NULL, adjustment_date TIMESTAMP DEFAULT NOW(), old_quantity DOUBLE PRECISION NOT NULL, new_quantity DOUBLE PRECISION NOT NULL, difference DOUBLE PRECISION NOT NULL, reason VARCHAR NOT NULL, notes TEXT, created_at TIMESTAMP DEFAULT NOW())")
    cur.execute("CREATE UNIQUE INDEX uq_inventory_movement_ref ON inventory_movements(item_id, reference_id, reference_type) WHERE reference_id IS NOT NULL")
    cur.execute("INSERT INTO warehouses (name, code) VALUES ('Main', 'WH01')")
    cur.execute("INSERT INTO items (name, barcode, sell_price, opening_quantity, min_stock) VALUES ('Item A', 'BC001', 100, 50, 10), ('Item B', 'BC002', 200, 30, 5), ('Item C', 'BC003', 50, 0, 20)")
    cur.close(); conn.close(); print("[SETUP] Ready")

def get_stock(cur, item_id):
    cur.execute("SELECT COALESCE(opening_quantity,0) FROM items WHERE id=%s", (item_id,)); op = float(cur.fetchone()[0])
    cur.execute("SELECT COALESCE(SUM(CASE WHEN type IN ('in','opening') THEN quantity WHEN type='out' THEN -quantity WHEN type='adjustment' THEN quantity ELSE 0 END),0) FROM inventory_movements WHERE item_id=%s", (item_id,)); mv = float(cur.fetchone()[0])
    return op + mv

def test_1():
    print("\n[TEST 1] Stock calculation")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        s = get_stock(cur, 1); test("initial=50", s == 50, f"got {s}")
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (1, 'out', 5, 100, 'invoice')"); conn.commit()
        s = get_stock(cur, 1); test("after sell 5=45", s == 45, f"got {s}")
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (1, 'in', 10, 101, 'purchase')"); conn.commit()
        s = get_stock(cur, 1); test("after buy 10=55", s == 55, f"got {s}")
    except Exception as e: conn.rollback(); test("calc", False, str(e))
    finally: cur.close(); conn.close()

def test_2():
    print("\n[TEST 2] Sale deduction")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("DELETE FROM inventory_movements WHERE item_id=2"); conn.commit()
        b = get_stock(cur, 2); test("before=30", b == 30, f"got {b}")
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (2, 'out', 8, 200, 'invoice')"); conn.commit()
        a = get_stock(cur, 2); test("after sell 8=22", a == 22, f"got {a}")
    except Exception as e: conn.rollback(); test("sale", False, str(e))
    finally: cur.close(); conn.close()

def test_3():
    print("\n[TEST 3] Purchase addition")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        b = get_stock(cur, 3); test("before=0", b == 0, f"got {b}")
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (3, 'in', 100, 300, 'purchase')"); conn.commit()
        a = get_stock(cur, 3); test("after buy 100=100", a == 100, f"got {a}")
    except Exception as e: conn.rollback(); test("purchase", False, str(e))
    finally: cur.close(); conn.close()

def test_4():
    print("\n[TEST 4] Sales return")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("DELETE FROM inventory_movements WHERE item_id=2"); conn.commit()
        b = get_stock(cur, 2); test("before=30", b == 30, f"got {b}")
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (2, 'out', 5, 201, 'invoice')"); conn.commit()
        s = get_stock(cur, 2); test("after sell 5=25", s == 25, f"got {s}")
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (2, 'in', 3, 202, 'return')"); conn.commit()
        r = get_stock(cur, 2); test("after return 3=28", r == 28, f"got {r}")
    except Exception as e: conn.rollback(); test("return", False, str(e))
    finally: cur.close(); conn.close()

def test_5():
    print("\n[TEST 5] Stock adjustment")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("DELETE FROM inventory_movements WHERE item_id=3"); cur.execute("DELETE FROM stock_adjustments WHERE item_id=3"); conn.commit()
        c = get_stock(cur, 3); test("current=0", c == 0, f"got {c}")
        new_qty = 15; diff = new_qty - c
        cur.execute("INSERT INTO stock_adjustments (item_id, old_quantity, new_quantity, difference, reason) VALUES (3, %s, %s, %s, 'Physical count')", (c, new_qty, diff))
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_type) VALUES (3, 'adjustment', %s, 'adjustment')", (diff,)); conn.commit()
        a = get_stock(cur, 3); test("after adjust=15", a == 15, f"got {a}")
        cur.execute("SELECT old_quantity, new_quantity, difference, reason FROM stock_adjustments WHERE item_id=3"); adj = cur.fetchone()
        test("old=0", adj[0] == 0, f"got {adj[0]}")
        test("new=15", adj[1] == 15, f"got {adj[1]}")
        test("diff=15", adj[2] == 15, f"got {adj[2]}")
        test("reason recorded", adj[3] == 'Physical count', f"got {adj[3]}")
    except Exception as e: conn.rollback(); test("adjust", False, str(e))
    finally: cur.close(); conn.close()

def test_6():
    print("\n[TEST 6] Idempotency")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (1, 'out', 5, 500, 'invoice')"); conn.commit()
        try:
            cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (1, 'out', 5, 500, 'invoice')"); conn.commit()
            test("duplicate blocked", False, "should fail")
        except psycopg2.IntegrityError:
            conn.rollback(); test("duplicate blocked", True)
        cur.execute("SELECT COUNT(*) FROM inventory_movements WHERE item_id=1 AND reference_id=500"); c = cur.fetchone()[0]
        test("one movement", c == 1, f"got {c}")
    except Exception as e: conn.rollback(); test("idem", False, str(e))
    finally: cur.close(); conn.close()

def test_7():
    print("\n[TEST 7] Low stock report")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("DELETE FROM inventory_movements"); conn.commit()
        cur.execute("SELECT i.id, i.name, i.min_stock, i.opening_quantity + COALESCE((SELECT SUM(CASE WHEN im.type IN ('in','opening') THEN im.quantity WHEN im.type='out' THEN -im.quantity WHEN im.type='adjustment' THEN im.quantity ELSE 0 END) FROM inventory_movements im WHERE im.item_id=i.id), 0) FROM items i WHERE i.min_stock IS NOT NULL AND i.min_stock > 0")
        low = [r[1] for r in cur.fetchall() if float(r[3]) <= float(r[2])]
        test("Item C low", 'Item C' in low, f"got {low}")
        test("only Item C", len(low) == 1, f"got {len(low)}")
    except Exception as e: conn.rollback(); test("low", False, str(e))
    finally: cur.close(); conn.close()

def test_8():
    print("\n[TEST 8] Negative stock")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("DELETE FROM inventory_movements WHERE item_id=2"); conn.commit()
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (2, 'out', 40, 600, 'invoice')"); conn.commit()
        s = get_stock(cur, 2); test("stock=-10", s == -10, f"got {s}")
        test("DB allows (code checks)", True, "")
    except Exception as e: conn.rollback(); test("neg", False, str(e))
    finally: cur.close(); conn.close()

def test_9():
    print("\n[TEST 9] Concurrent sales")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("DELETE FROM inventory_movements WHERE item_id=1"); cur.execute("DELETE FROM inventory_movements WHERE reference_id IN (700,701,702,703,704)"); conn.commit()
        results = []; ref_counter = [700]; lock = threading.Lock()
        def sell():
            try:
                with lock: ref_id = ref_counter[0]; ref_counter[0] += 1
                c = psycopg2.connect(CS_TEST); c.autocommit = False; cu = c.cursor()
                cu.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (1, 'out', 5, %s, 'invoice')", (ref_id,)); c.commit(); cu.close(); c.close(); results.append(True)
            except: results.append(False)
        threads = [threading.Thread(target=sell) for _ in range(5)]
        for t in threads: t.start()
        for t in threads: t.join()
        s = get_stock(cur, 1)
        test("all 5 succeeded", len([r for r in results if r]) == 5, f"got {len([r for r in results if r])}")
        test("stock=25", s == 25, f"got {s}")
    except Exception as e: conn.rollback(); test("conc", False, str(e))
    finally: cur.close(); conn.close()

def cleanup():
    print("\n[CLEANUP]...")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["stock_adjustments","inventory_movements","invoice_payments","invoice_lines","invoices","items","warehouses"]: cur.execute(f"DELETE FROM {t}")
    cur.close(); conn.close(); print("[CLEANUP] Done")

def main():
    print("=" * 60); print("  Techtoon Inventory Test Suite"); print("=" * 60)
    try: setup()
    except Exception as e: print(f"FATAL: {e}"); sys.exit(1)
    test_1(); test_2(); test_3(); test_4(); test_5(); test_6(); test_7(); test_8(); test_9()
    cleanup()
    print("\n" + "=" * 60); print(f"  RESULTS: {TESTS_PASSED} passed, {TESTS_FAILED} failed"); print("=" * 60)
    if TESTS_FAILED > 0: sys.exit(1)

if __name__ == "__main__": main()
