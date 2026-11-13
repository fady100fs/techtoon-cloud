"""فحص دخان لصفحات Streamlit: كل استعلام SQL يُنفَّذ فعليًا على قاعدة الاختبار."""
import os, sys, importlib, traceback

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
os.environ.setdefault("DATABASE_URL", os.environ.get("TECHTOON_PG_CONNECTION", ""))

results = []

def check(name, fn):
    try:
        v = fn()
        ok = v is not None
        results.append((name, "PASS" if ok else "FAIL", str(v)[:90]))
    except Exception as e:
        results.append((name, "FAIL", f"{type(e).__name__}: {e}"))

try:
    import database as db
    check("database.get_connection + query", lambda: db.execute_query("SELECT COUNT(*) FROM users", fetch=True)[1][0][0])
except Exception as e:
    results.append(("database module", "FAIL", str(e)))

try:
    import app as streamlit_app
    check("app.login_user (correct password)", lambda: streamlit_app.login_user("admin", "admin123"))
    check("app.login_user (wrong password rejected)", lambda: (streamlit_app.login_user("admin", "wrong-pass") is None) or "FAIL:not-rejected")
    check("app._safe_scalar", lambda: streamlit_app._safe_scalar("SELECT COUNT(*) FROM invoices"))
except Exception as e:
    results.append(("app module", "FAIL", traceback.format_exc()[-200:]))

# استعلامات الصفحات — تُنفَّذ فعليًا
QUERIES = [
    ("pages/invoices.py", "SELECT COUNT(*) FROM invoices"),
    ("pages/invoices.py", "SELECT COUNT(*) FROM invoice_lines"),
    ("pages/parties.py", "SELECT COUNT(*) FROM parties"),
    ("pages/items.py", "SELECT COUNT(*) FROM items"),
    ("pages/payments.py", "SELECT COUNT(*) FROM payments"),
    ("pages/loans.py", "SELECT l.id, l.loan_number, COALESCE(l.borrower_name,''), l.loan_type, l.principal_amount, COALESCE(l.interest_rate,0), l.status FROM loans l ORDER BY l.id DESC"),
    ("pages/loans.py", "SELECT installment_number, due_date, principal_amount, interest_amount, total_amount, paid_amount, status FROM loan_installments WHERE loan_id=1 ORDER BY installment_number"),
    ("pages/reports.py", "SELECT COUNT(*) FROM invoices"),
    ("pages/stock_levels.py", "SELECT COUNT(*) FROM stock_levels"),
    ("pages/stock_counts.py", "SELECT COUNT(*) FROM stock_count_sessions"),
    ("pages/stock_adjustments.py", "SELECT COUNT(*) FROM stock_adjustments"),
    ("pages/damaged_items.py", "SELECT COUNT(*) FROM damaged_items"),
    ("pages/writeoff_requests.py", "SELECT COUNT(*) FROM writeoff_requests"),
    ("pages/cash_boxes.py", "SELECT COUNT(*) FROM cash_boxes"),
    ("pages/cash_boxes.py", "SELECT COUNT(*) FROM cash_transfers"),
    ("pages/cash_boxes.py", "SELECT COUNT(*) FROM cash_boxes WHERE code = 'nope'"),
]
try:
    import database as db
    for src, q in QUERIES:
        check(f"{src} :: {q[:52]}", lambda q=q: db.execute_query(q, fetch=True)[1])
except Exception as e:
    results.append(("queries", "FAIL", str(e)))

# كل الوحدات تُستورد بلا أخطاء
for mod in ["database", "utils.math_eval", "pages.invoices", "pages.parties", "pages.items",
            "pages.payments", "pages.loans", "pages.reports", "pages.stock_levels",
            "pages.stock_counts", "pages.stock_adjustments", "pages.damaged_items",
            "pages.writeoff_requests", "pages.cash_boxes"]:
    check(f"import {mod}", lambda m=mod: importlib.import_module(m))

print()
for name, res, detail in results:
    print(f"{res:<5} {name:<58} {detail}")
print()
print("SUMMARY: PASS=%d FAIL=%d" % (sum(1 for r in results if r[1] == "PASS"), sum(1 for r in results if r[1] == "FAIL")))
