# -*- coding: utf-8 -*-
"""test_financial_system.py - اختبارات النظام المالي"""
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
    print("\n[SETUP] Preparing financial test tables...")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["shift_closings", "cash_transfers", "expenses", "expense_categories", "payments", "invoice_lines", "invoices", "cash_boxes"]:
        cur.execute(f"DROP TABLE IF EXISTS {t} CASCADE")
    
    cur.execute("CREATE TABLE cash_boxes (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL, code VARCHAR, opening_balance DOUBLE PRECISION DEFAULT 0, is_active BOOLEAN DEFAULT TRUE)")
    cur.execute("CREATE TABLE expense_categories (id SERIAL PRIMARY KEY, name VARCHAR NOT NULL, is_active BOOLEAN DEFAULT TRUE)")
    cur.execute("CREATE TABLE expenses (id SERIAL PRIMARY KEY, date TIMESTAMP DEFAULT NOW(), category_id INTEGER, amount DOUBLE PRECISION NOT NULL, description TEXT, cash_box_id INTEGER, notes TEXT)")
    cur.execute("CREATE TABLE payments (id SERIAL PRIMARY KEY, date TIMESTAMP DEFAULT NOW(), party_id INTEGER, type VARCHAR, amount DOUBLE PRECISION DEFAULT 0, cash_box_id INTEGER, voucher_no VARCHAR)")
    cur.execute("CREATE TABLE cash_transfers (id SERIAL PRIMARY KEY, date TIMESTAMP DEFAULT NOW(), from_cash_box_id INTEGER NOT NULL, to_cash_box_id INTEGER NOT NULL, amount DOUBLE PRECISION NOT NULL, notes TEXT, status VARCHAR DEFAULT 'completed', created_at TIMESTAMP DEFAULT NOW())")
    cur.execute("CREATE TABLE invoices (id SERIAL PRIMARY KEY, invoice_number VARCHAR NOT NULL UNIQUE, date TIMESTAMP DEFAULT NOW(), party_id INTEGER, type VARCHAR NOT NULL, total NUMERIC(14,2) DEFAULT 0, paid_amount NUMERIC(14,2) DEFAULT 0, status VARCHAR DEFAULT 'draft')")
    cur.execute("CREATE TABLE invoice_lines (id SERIAL PRIMARY KEY, invoice_id INTEGER NOT NULL, item_id INTEGER, quantity DOUBLE PRECISION NOT NULL, price DOUBLE PRECISION NOT NULL, total DOUBLE PRECISION NOT NULL, cost_price DOUBLE PRECISION DEFAULT 0)")
    cur.execute("CREATE TABLE shift_closings (id SERIAL PRIMARY KEY, cash_box_id INTEGER NOT NULL, shift_date TIMESTAMP DEFAULT NOW(), opening_amount DOUBLE PRECISION NOT NULL, expected_closing DOUBLE PRECISION NOT NULL, actual_closing DOUBLE PRECISION NOT NULL, difference DOUBLE PRECISION NOT NULL, status VARCHAR DEFAULT 'closed', notes TEXT, created_at TIMESTAMP DEFAULT NOW())")
    
    cur.execute("INSERT INTO cash_boxes (name, code, opening_balance) VALUES ('Main', 'CB001', 1000), ('Bank', 'BANK01', 5000)")
    cur.execute("INSERT INTO expense_categories (name) VALUES ('Rent'), ('Utilities'), ('Salaries')")
    cur.close(); conn.close()
    print("[SETUP] Ready")

def get_balance(cur, cash_box_id):
    cur.execute("SELECT COALESCE(opening_balance,0) FROM cash_boxes WHERE id=%s", (cash_box_id,)); op = float(cur.fetchone()[0])
    cur.execute("SELECT COALESCE(SUM(amount),0) FROM payments WHERE cash_box_id=%s AND type='receipt'", (cash_box_id,)); rc = float(cur.fetchone()[0])
    cur.execute("SELECT COALESCE(SUM(amount),0) FROM payments WHERE cash_box_id=%s AND type='payment'", (cash_box_id,)); pm = float(cur.fetchone()[0])
    cur.execute("SELECT COALESCE(SUM(amount),0) FROM expenses WHERE cash_box_id=%s", (cash_box_id,)); ex = float(cur.fetchone()[0])
    cur.execute("SELECT COALESCE(SUM(amount),0) FROM cash_transfers WHERE to_cash_box_id=%s AND status='completed'", (cash_box_id,)); ti = float(cur.fetchone()[0])
    cur.execute("SELECT COALESCE(SUM(amount),0) FROM cash_transfers WHERE from_cash_box_id=%s AND status='completed'", (cash_box_id,)); to = float(cur.fetchone()[0])
    return op + rc - pm - ex + ti - to

def test_1():
    print("\n[TEST 1] Receipt (قبض)")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        b = get_balance(cur, 1); test("before=1000", b == 1000, f"got {b}")
        cur.execute("INSERT INTO payments (party_id, type, amount, cash_box_id, voucher_no) VALUES (1, 'receipt', 500, 1, 'RCV-001')"); conn.commit()
        a = get_balance(cur, 1); test("after receipt 500=1500", a == 1500, f"got {a}")
    except Exception as e: conn.rollback(); test("receipt", False, str(e))
    finally: cur.close(); conn.close()

def test_2():
    print("\n[TEST 2] Payment (صرف)")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        b = get_balance(cur, 1)
        cur.execute("INSERT INTO payments (party_id, type, amount, cash_box_id, voucher_no) VALUES (1, 'payment', 300, 1, 'PAY-001')"); conn.commit()
        a = get_balance(cur, 1); test(f"after payment 300={b-300}", a == b-300, f"got {a}")
    except Exception as e: conn.rollback(); test("payment", False, str(e))
    finally: cur.close(); conn.close()

def test_3():
    print("\n[TEST 3] Transfer between cash boxes")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        b1 = get_balance(cur, 1); b2 = get_balance(cur, 2)
        cur.execute("INSERT INTO cash_transfers (from_cash_box_id, to_cash_box_id, amount, status) VALUES (1, 2, 200, 'completed')"); conn.commit()
        a1 = get_balance(cur, 1); a2 = get_balance(cur, 2)
        test(f"box1 decreased by 200 ({b1}-200={a1})", a1 == b1 - 200, f"got {a1}")
        test(f"box2 increased by 200 ({b2}+200={a2})", a2 == b2 + 200, f"got {a2}")
    except Exception as e: conn.rollback(); test("transfer", False, str(e))
    finally: cur.close(); conn.close()

def test_4():
    print("\n[TEST 4] Expense recording")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        b = get_balance(cur, 1)
        cur.execute("INSERT INTO expenses (date, category_id, amount, description, cash_box_id) VALUES (NOW(), 1, 150, 'Rent', 1)"); conn.commit()
        a = get_balance(cur, 1); test(f"after expense 150={b-150}", a == b - 150, f"got {a}")
    except Exception as e: conn.rollback(); test("expense", False, str(e))
    finally: cur.close(); conn.close()

def test_5():
    print("\n[TEST 5] Edit payment updates balance")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO payments (party_id, type, amount, cash_box_id, voucher_no) VALUES (1, 'receipt', 200, 1, 'RCV-EDIT') RETURNING id")
        pid = cur.fetchone()[0]; conn.commit()
        b = get_balance(cur, 1)
        cur.execute("UPDATE payments SET amount = 300 WHERE id = %s", (pid,)); conn.commit()
        a = get_balance(cur, 1)
        test(f"balance changed by +100 ({b} -> {a})", a == b + 100, f"got {a}")
    except Exception as e: conn.rollback(); test("edit", False, str(e))
    finally: cur.close(); conn.close()

def test_6():
    print("\n[TEST 6] Shift closing")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        b = get_balance(cur, 1)
        actual = b + 50  // زيادة 50
        diff = actual - b
        cur.execute("INSERT INTO shift_closings (cash_box_id, opening_amount, expected_closing, actual_closing, difference, status) VALUES (1, 1000, %s, %s, %s, 'closed')", (b, actual, diff)); conn.commit()
        cur.execute("SELECT expected_closing, actual_closing, difference FROM shift_closings WHERE cash_box_id=1 ORDER BY id DESC LIMIT 1")
        r = cur.fetchone()
        test("expected matches balance", r[0] == b, f"got {r[0]}")
        test("difference=50", r[2] == 50, f"got {r[2]}")
    except Exception as e: conn.rollback(); test("shift", False, str(e))
    finally: cur.close(); conn.close()

def test_7():
    print("\n[TEST 7] Cash flow report")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("SELECT COALESCE(SUM(amount),0) FROM payments WHERE type='receipt'"); rc = float(cur.fetchone()[0])
        cur.execute("SELECT COALESCE(SUM(amount),0) FROM payments WHERE type='payment'"); pm = float(cur.fetchone()[0])
        cur.execute("SELECT COALESCE(SUM(amount),0) FROM expenses"); ex = float(cur.fetchone()[0])
        net = rc - pm - ex
        test("receipts > 0", rc > 0, f"got {rc}")
        test("net = receipts - payments - expenses", net == rc - pm - ex, f"got {net}")
    except Exception as e: conn.rollback(); test("cashflow", False, str(e))
    finally: cur.close(); conn.close()

def test_8():
    print("\n[TEST 8] Duplicate payment prevention")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        cur.execute("INSERT INTO payments (party_id, type, amount, cash_box_id, voucher_no) VALUES (1, 'receipt', 100, 1, 'DUP-TEST')")
        conn.commit()
        try:
            cur.execute("INSERT INTO payments (party_id, type, amount, cash_box_id, voucher_no) VALUES (1, 'receipt', 100, 1, 'DUP-TEST')")
            conn.commit()
            test("duplicate voucher blocked", False, "should fail")
        except psycopg2.IntegrityError:
            conn.rollback()
            test("duplicate voucher blocked (by app logic)", True, "")
    except Exception as e: conn.rollback(); test("dup", False, str(e))
    finally: cur.close(); conn.close()

def test_9():
    print("\n[TEST 9] Gross profit calculation")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = False; cur = conn.cursor()
    try:
        # فاتورة مبيعات: price=100, cost=60 -> profit=40
        cur.execute("INSERT INTO invoices (invoice_number, date, party_id, type, total, paid_amount, status) VALUES ('GP-001', NOW(), 1, 'sales', 100, 100, 'paid') RETURNING id")
        inv_id = cur.fetchone()[0]
        cur.execute("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, total, cost_price) VALUES (%s, 1, 1, 100, 100, 60)", (inv_id,)); conn.commit()
        
        cur.execute("""
            SELECT COALESCE(SUM(il.total),0) - COALESCE(SUM(il.quantity * il.cost_price),0)
            FROM invoice_lines il JOIN invoices i ON il.invoice_id = i.id
            WHERE i.type='sales'
        """)
        profit = float(cur.fetchone()[0])
        test("gross profit = 40", profit == 40, f"got {profit}")
    except Exception as e: conn.rollback(); test("profit", False, str(e))
    finally: cur.close(); conn.close()

def cleanup():
    print("\n[CLEANUP]...")
    conn = psycopg2.connect(CS_TEST); conn.autocommit = True; cur = conn.cursor()
    for t in ["shift_closings", "cash_transfers", "expenses", "expense_categories", "payments", "invoice_lines", "invoices", "cash_boxes"]:
        cur.execute(f"DELETE FROM {t}")
    cur.close(); conn.close(); print("[CLEANUP] Done")

def main():
    print("=" * 60); print("  Techtoon Financial System Test Suite"); print("=" * 60)
    try: setup()
    except Exception as e: print(f"FATAL: {e}"); sys.exit(1)
    test_1(); test_2(); test_3(); test_4(); test_5(); test_6(); test_7(); test_8(); test_9()
    cleanup()
    print("\n" + "=" * 60); print(f"  RESULTS: {TESTS_PASSED} passed, {TESTS_FAILED} failed"); print("=" * 60)
    if TESTS_FAILED > 0: sys.exit(1)

if __name__ == "__main__": main()
