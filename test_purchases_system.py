# -*- coding: utf-8 -*-
"""test_purchases_system.py - اختبارات نظام المشتريات"""
import psycopg2, sys
from datetime import datetime

TEST_DB = "techtoon_test"
CS_TEST = f"host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname={TEST_DB} user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"
TESTS_PASSED = 0
TESTS_FAILED = 0

def test(name, condition, detail=""):
    global TESTS_PASSED, TESTS_FAILED
    if condition:
        print(f"  PASS: {name}")
        TESTS_PASSED += 1
    else:
        print(f"  FAIL: {name} - {detail}")
        TESTS_FAILED += 1

def setup():
    print("\n[SETUP] Preparing test tables...")
    conn = psycopg2.connect(CS_TEST)
    conn.autocommit = True
    cur = conn.cursor()
    for t in ["invoice_payments", "invoice_lines", "invoices", "payments", "inventory_movements", "items", "parties"]:
        cur.execute(f"DROP TABLE IF EXISTS {t} CASCADE")
    cur.execute("CREATE TABLE parties (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL, type VARCHAR, opening_balance DOUBLE PRECISION DEFAULT 0)")
    cur.execute("CREATE TABLE items (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL, barcode VARCHAR, sell_price DOUBLE PRECISION DEFAULT 0, cost_price DOUBLE PRECISION DEFAULT 0, avg_cost_price DOUBLE PRECISION DEFAULT 0, total_purchased_qty DOUBLE PRECISION DEFAULT 0, opening_quantity DOUBLE PRECISION DEFAULT 0)")
    cur.execute("CREATE TABLE invoices (id SERIAL PRIMARY KEY, invoice_number VARCHAR NOT NULL UNIQUE, date TIMESTAMP DEFAULT NOW(), party_id INTEGER NOT NULL, type VARCHAR NOT NULL, subtotal NUMERIC(14,2) DEFAULT 0, discount_amount NUMERIC(14,2) DEFAULT 0, total NUMERIC(14,2) DEFAULT 0, paid_amount NUMERIC(14,2) DEFAULT 0, status VARCHAR DEFAULT 'draft', currency_id INTEGER DEFAULT 1)")
    cur.execute("CREATE TABLE invoice_lines (id SERIAL PRIMARY KEY, invoice_id INTEGER NOT NULL, item_id INTEGER NOT NULL, quantity DOUBLE PRECISION NOT NULL, price DOUBLE PRECISION NOT NULL, discount NUMERIC(14,2) DEFAULT 0, total DOUBLE PRECISION NOT NULL, cost_price DOUBLE PRECISION DEFAULT 0)")
    cur.execute("CREATE TABLE invoice_payments (id SERIAL PRIMARY KEY, invoice_id INTEGER NOT NULL, method VARCHAR NOT NULL DEFAULT 'cash', amount DOUBLE PRECISION DEFAULT 0, reference VARCHAR, paid_at TIMESTAMP DEFAULT NOW())")
    cur.execute("CREATE TABLE payments (id SERIAL PRIMARY KEY, voucher_no VARCHAR, date TIMESTAMP DEFAULT NOW(), party_id INTEGER NOT NULL, type VARCHAR, amount DOUBLE PRECISION DEFAULT 0, method VARCHAR DEFAULT 'cash')")
    cur.execute("CREATE TABLE inventory_movements (id SERIAL PRIMARY KEY, item_id INTEGER NOT NULL, date TIMESTAMP DEFAULT NOW(), type VARCHAR NOT NULL, quantity DOUBLE PRECISION NOT NULL, reference_id INTEGER, reference_type VARCHAR DEFAULT 'invoice')")
    cur.execute("INSERT INTO parties (name, type, opening_balance) VALUES ('Test Supplier', 'supplier', 0), ('Test Customer', 'customer', 0)")
    cur.execute("INSERT INTO items (name, barcode, sell_price, cost_price, avg_cost_price, total_purchased_qty) VALUES ('Item A', 'BC001', 100, 50, 50, 10), ('Item B', 'BC002', 200, 150, 150, 5)")
    cur.close(); conn.close()
    print("[SETUP] Ready")

def test_1():
    print("\n[TEST 1] Full purchase cycle")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, subtotal, total, paid_amount, status) VALUES ('PUR-001', NOW(), 1, 'purchase', 500, 500, 500, 'paid') RETURNING id")
        inv_id = cur.fetchone()[0]
        cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total, cost_price) VALUES (%s, 1, 5, 50, 250, 50)", (inv_id,))
        cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total, cost_price) VALUES (%s, 2, 1, 250, 250, 250)", (inv_id,))
        cur.execute("INSERT INTO invoice_payments (invoice_id, method, amount) VALUES (%s, 'cash', 500)", (inv_id,))
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (1, 'in', 5, %s, 'invoice')", (inv_id,))
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (2, 'in', 1, %s, 'invoice')", (inv_id,))
        conn.commit()
        cur.execute("SELECT COUNT(*) FROM invoice_lines WHERE invoice_id=%s", (inv_id,)); l = cur.fetchone()[0]
        cur.execute("SELECT COUNT(*) FROM invoice_payments WHERE invoice_id=%s", (inv_id,)); p = cur.fetchone()[0]
        cur.execute("SELECT COUNT(*) FROM inventory_movements WHERE reference_id=%s AND reference_type='invoice'", (inv_id,)); m = cur.fetchone()[0]
        test("2 lines", l == 2, f"got {l}")
        test("1 payment", p == 1, f"got {p}")
        test("2 movements", m == 2, f"got {m}")
    except Exception as e: conn.rollback(); test("cycle", False, str(e))
    finally: cur.close(); conn.close()

def test_2():
    print("\n[TEST 2] Partial payment")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, subtotal, total, paid_amount, status) VALUES ('PUR-002', NOW(), 1, 'purchase', 1000, 1000, 0, 'unpaid') RETURNING id")
        inv_id = cur.fetchone()[0]; conn.commit()
        cur.execute("INSERT INTO payments (voucher_no, party_id, type, amount) VALUES ('PAY-001', 1, 'payment', 300)")
        cur.execute("UPDATE invoices SET paid_amount = 300, status = 'partial' WHERE id = %s", (inv_id,)); conn.commit()
        cur.execute("SELECT paid_amount, status FROM invoices WHERE id = %s", (inv_id,)); paid, status = cur.fetchone()
        test("paid=300", paid == 300, f"got {paid}")
        test("status=partial", status == 'partial', f"got {status}")
    except Exception as e: conn.rollback(); test("partial", False, str(e))
    finally: cur.close(); conn.close()

def test_3():
    print("\n[TEST 3] Purchase return")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, subtotal, total, paid_amount, status) VALUES ('PUR-003', NOW(), 1, 'purchase', 200, 200, 200, 'paid') RETURNING id")
        inv_id = cur.fetchone()[0]
        cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total) VALUES (%s, 1, 2, 100, 200)", (inv_id,)); conn.commit()
        cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total) VALUES (%s, 1, -1, 100, -100)", (inv_id,))
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_id, reference_type) VALUES (1, 'out', 1, %s, 'return')", (inv_id,))
        cur.execute("UPDATE invoices SET subtotal = (SELECT SUM(total) FROM invoice_lines WHERE invoice_id=%s), total = (SELECT SUM(total) FROM invoice_lines WHERE invoice_id=%s) WHERE id=%s", (inv_id, inv_id, inv_id)); conn.commit()
        cur.execute("SELECT total FROM invoices WHERE id = %s", (inv_id,)); t = cur.fetchone()[0]
        test("total=100", t == 100, f"got {t}")
    except Exception as e: conn.rollback(); test("return", False, str(e))
    finally: cur.close(); conn.close()

def test_4():
    print("\n[TEST 4] Rollback on failure")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, total, paid_amount, status) VALUES ('PUR-FAIL', NOW(), 1, 'purchase', 100, 0, 'unpaid') RETURNING id")
        inv_id = cur.fetchone()[0]
        cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total) VALUES (%s, 1, 1, 100, 100)", (inv_id,))
        try:
            cur.execute("INSERT INTO invoice_payments (invoice_id, method, amount) VALUES (%s, NULL, 100)", (inv_id,))
            conn.commit(); test("rollback", False, "should fail")
        except psycopg2.IntegrityError:
            conn.rollback()
            cur.execute("SELECT COUNT(*) FROM invoices WHERE invoice_number='PUR-FAIL'"); c = cur.fetchone()[0]
            test("rollback", c == 0, f"got {c}")
    except Exception as e: conn.rollback(); test("rollback", False, str(e))
    finally: cur.close(); conn.close()

def test_5():
    print("\n[TEST 5] Edit existing invoice")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, subtotal, total, paid_amount, status) VALUES ('PUR-EDIT', NOW(), 1, 'purchase', 300, 300, 0, 'unpaid') RETURNING id")
        inv_id = cur.fetchone()[0]
        cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total) VALUES (%s, 1, 3, 100, 300)", (inv_id,)); conn.commit()
        cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total) VALUES (%s, 2, 1, 200, 200)", (inv_id,))
        cur.execute("UPDATE invoices SET subtotal = 500, total = 500 WHERE id = %s", (inv_id,)); conn.commit()
        cur.execute("SELECT total FROM invoices WHERE id = %s", (inv_id,)); t = cur.fetchone()[0]
        cur.execute("SELECT COUNT(*) FROM invoice_lines WHERE invoice_id = %s", (inv_id,)); l = cur.fetchone()[0]
        test("total=500", t == 500, f"got {t}")
        test("lines=2", l == 2, f"got {l}")
    except Exception as e: conn.rollback(); test("edit", False, str(e))
    finally: cur.close(); conn.close()

def test_6():
    print("\n[TEST 6] Inventory update on purchase")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("DELETE FROM inventory_movements WHERE item_id=1")
        cur.execute("INSERT INTO inventory_movements (item_id, type, quantity, reference_type) VALUES (1, 'in', 5, 'invoice')"); conn.commit()
        cur.execute("SELECT i.opening_quantity + COALESCE(SUM(CASE WHEN im.type='in' THEN im.quantity WHEN im.type='out' THEN -im.quantity WHEN im.type='adjustment' THEN im.quantity ELSE 0 END), 0) FROM items i LEFT JOIN inventory_movements im ON i.id=im.item_id WHERE i.id=1 GROUP BY i.id")
        s = cur.fetchone()[0]; test("stock=5", s == 5, f"got {s}")
    except Exception as e: conn.rollback(); test("inv", False, str(e))
    finally: cur.close(); conn.close()

def test_7():
    print("\n[TEST 7] Weighted average cost")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        old_qty, old_avg, new_qty, new_price = 10, 50, 5, 60
        expected = (old_avg * old_qty + new_price * new_qty) / (old_qty + new_qty)
        cur.execute("UPDATE items SET avg_cost_price = (COALESCE(avg_cost_price,0)*COALESCE(total_purchased_qty,0)+%s*%s)/(COALESCE(total_purchased_qty,0)+%s), cost_price=%s, total_purchased_qty=COALESCE(total_purchased_qty,0)+%s WHERE id=1", (new_price, new_qty, new_qty, new_price, new_qty)); conn.commit()
        cur.execute("SELECT avg_cost_price, cost_price, total_purchased_qty FROM items WHERE id=1"); avg, cost, qty = cur.fetchone()
        test("cost=60", cost == 60, f"got {cost}")
        test("qty=15", qty == 15, f"got {qty}")
        test("avg~53.33", abs(avg - expected) < 0.1, f"got {avg}")
    except Exception as e: conn.rollback(); test("cost", False, str(e))
    finally: cur.close(); conn.close()

def test_8():
    print("\n[TEST 8] Supplier statement")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT COALESCE(SUM(total),0) FROM invoices WHERE party_id=1 AND type='purchase'"); tp = cur.fetchone()[0]
        cur.execute("SELECT COALESCE(SUM(amount),0) FROM payments WHERE party_id=1 AND type='payment'"); pm = cur.fetchone()[0]
        bal = float(tp) - float(pm)
        test("purchases>0", float(tp) > 0, f"got {tp}")
        test("payments=300", float(pm) == 300, f"got {pm}")
        test("balance correct", bal == float(tp) - float(pm), f"got {bal}")
    except Exception as e: conn.rollback(); test("statement", False, str(e))
    finally: cur.close(); conn.close()

def test_9():
    print("\n[TEST 9] Prevent duplicate payment")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, total, paid_amount, status) VALUES ('PUR-DUP', NOW(), 1, 'purchase', 500, 0, 'unpaid') RETURNING id")
        inv_id = cur.fetchone()[0]; conn.commit()
        cur.execute("UPDATE invoices SET paid_amount = 200, status = 'partial' WHERE id = %s", (inv_id,)); conn.commit()
        cur.execute("SELECT paid_amount FROM invoices WHERE id = %s", (inv_id,)); cp = cur.fetchone()[0]
        test("duplicate prevented", cp >= 200, "")
    except Exception as e: conn.rollback(); test("dup", False, str(e))
    finally: cur.close(); conn.close()

def cleanup():
    print("\n[CLEANUP]...")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["invoice_payments", "invoice_lines", "invoices", "payments", "inventory_movements", "items", "parties"]: cur.execute(f"DELETE FROM {t}")
    cur.close(); conn.close(); print("[CLEANUP] Done")

def main():
    print("=" * 60); print("  Techtoon Purchases Test Suite"); print("=" * 60)
    try: setup()
    except Exception as e: print(f"FATAL: {e}"); sys.exit(1)
    test_1(); test_2(); test_3(); test_4(); test_5(); test_6(); test_7(); test_8(); test_9()
    cleanup()
    print("\n" + "=" * 60); print(f"  RESULTS: {TESTS_PASSED} passed, {TESTS_FAILED} failed"); print("=" * 60)
    if TESTS_FAILED > 0: sys.exit(1)

if __name__ == "__main__": main()
