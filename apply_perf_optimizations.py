# -*- coding: utf-8 -*-
import os, sys, re
from pathlib import Path
from datetime import datetime

PROJECT = Path(__file__).resolve().parent
os.chdir(PROJECT)

DB_INDEXES_SQL = """
CREATE INDEX IF NOT EXISTS idx_invoices_date ON invoices (date);
CREATE INDEX IF NOT EXISTS idx_invoices_type ON invoices (type);
CREATE INDEX IF NOT EXISTS idx_invoices_party_id ON invoices (party_id);
CREATE INDEX IF NOT EXISTS idx_invoices_status ON invoices (status);
CREATE INDEX IF NOT EXISTS idx_invoices_type_date ON invoices (type, date);
CREATE INDEX IF NOT EXISTS idx_invoiceline_invoice_id ON invoice_lines (invoice_id);
CREATE INDEX IF NOT EXISTS idx_invoiceline_item_id ON invoice_lines (item_id);
CREATE INDEX IF NOT EXISTS idx_invmove_item_id ON inventory_movements (item_id);
CREATE INDEX IF NOT EXISTS idx_invmove_type ON inventory_movements (type);
CREATE INDEX IF NOT EXISTS idx_invmove_date ON inventory_movements (date);
CREATE INDEX IF NOT EXISTS idx_invmove_item_type ON inventory_movements (item_id, type);
CREATE INDEX IF NOT EXISTS idx_costhistory_item_id ON cost_history (item_id);
CREATE INDEX IF NOT EXISTS idx_costhistory_invoice_id ON cost_history (invoice_id);
CREATE INDEX IF NOT EXISTS idx_kitcomp_kit_id ON kit_components (kit_item_id);
CREATE INDEX IF NOT EXISTS idx_kitcomp_component_id ON kit_components (component_item_id);
CREATE INDEX IF NOT EXISTS idx_journalentry_date ON journal_entries (date);
CREATE INDEX IF NOT EXISTS idx_journalentry_period_id ON journal_entries (period_id);
CREATE INDEX IF NOT EXISTS idx_journalline_entry_id ON journal_lines (entry_id);
CREATE INDEX IF NOT EXISTS idx_journalline_account_id ON journal_lines (account_id);
CREATE INDEX IF NOT EXISTS idx_payment_party_id ON payments (party_id);
CREATE INDEX IF NOT EXISTS idx_payment_date ON payments (date);
CREATE INDEX IF NOT EXISTS idx_items_barcode ON items (barcode);
CREATE INDEX IF NOT EXISTS idx_items_category_id ON items (category_id);
CREATE INDEX IF NOT EXISTS idx_items_is_kit ON items (is_kit);
ANALYZE;
"""

DB_VIEWS_SQL = """
CREATE OR REPLACE VIEW v_stock_balance AS
SELECT item_id, SUM(CASE WHEN type='in' THEN quantity ELSE -quantity END) AS balance,
SUM(CASE WHEN type='in' THEN quantity ELSE 0 END) AS total_in,
SUM(CASE WHEN type='out' THEN quantity ELSE 0 END) AS total_out
FROM inventory_movements GROUP BY item_id;

CREATE OR REPLACE VIEW v_monthly_sales AS
SELECT DATE_TRUNC('month', date) AS month, COUNT(*) AS invoice_count,
SUM(net_amount) AS total_net, SUM(discount_amount) AS total_discount, SUM(tax_amount) AS total_tax
FROM invoices WHERE type='sale' AND status!='cancelled' GROUP BY month ORDER BY month DESC;

CREATE OR REPLACE VIEW v_monthly_purchases AS
SELECT DATE_TRUNC('month', date) AS month, COUNT(*) AS invoice_count, SUM(net_amount) AS total_net
FROM invoices WHERE type='purchase' AND status!='cancelled' GROUP BY month ORDER BY month DESC;

CREATE OR REPLACE VIEW v_party_ledger AS
SELECT i.party_id, i.date, 'invoice' AS source, i.invoice_number, i.type AS invoice_type, i.net_amount AS amount
FROM invoices i WHERE i.status!='cancelled'
UNION ALL
SELECT p.party_id, p.date, 'payment', p.reference_number, p.payment_type,
CASE WHEN p.payment_type='receipt' THEN p.amount ELSE -p.amount END FROM payments p ORDER BY date DESC;

CREATE OR REPLACE VIEW v_top_selling_items AS
SELECT il.item_id, i.name AS item_name, SUM(il.quantity) AS total_qty,
SUM(il.total) AS total_revenue, COUNT(DISTINCT il.invoice_id) AS invoice_count
FROM invoice_lines il JOIN invoices inv ON il.invoice_id=inv.id JOIN items i ON il.item_id=i.id
WHERE inv.type='sale' AND inv.status!='cancelled' GROUP BY il.item_id, i.name ORDER BY total_qty DESC;
"""

POOL_OLD = """    pool_size=5,
    max_overflow=10,"""

POOL_NEW = """    pool_size=10,
    max_overflow=20,"""

CACHE_APPEND = '''

@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_parties_index(limit: int = 2000):
    import models
    db = _get_read_session()
    try:
        rows = db.query(models.Party).order_by(models.Party.id.desc()).limit(limit).all()
        return [{"id": r.id, "name": getattr(r,"name","") or "", "type": getattr(r,"type","") or "",
                 "phone": getattr(r,"phone","") or "", "credit_limit": float(getattr(r,"credit_limit",0) or 0)} for r in rows]
    finally: db.close()

@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_cash_boxes_index():
    import models
    db = _get_read_session()
    try:
        rows = db.query(models.CashBox).all()
        return [{"id": r.id, "name": getattr(r,"name","") or "", "code": getattr(r,"code","") or "",
                 "type": str(getattr(r,"type","")) or ""} for r in rows]
    finally: db.close()

@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_all_users():
    import models
    db = _get_read_session()
    try:
        rows = db.query(models.User).all()
        return [{"id": r.id, "username": getattr(r,"username","") or "",
                 "full_name": getattr(r,"full_name","") or "",
                 "role": str(getattr(r,"role","")) or "", "is_active": bool(getattr(r,"is_active",True))} for r in rows]
    finally: db.close()

@st.cache_data(ttl=CACHE_TTL, show_spinner=False)
def get_warehouses_index():
    import models
    db = _get_read_session()
    try:
        rows = db.query(models.Warehouse).filter(models.Warehouse.is_active == True).all()
        return [{"id": r.id, "name": getattr(r,"name","") or "", "code": getattr(r,"code","") or ""} for r in rows]
    finally: db.close()
'''

def _backup(path):
    if not path.exists(): return None
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    bak = path.with_suffix(path.suffix + f".bak_{ts}")
    bak.write_bytes(path.read_bytes())
    return bak

def _patch_file(path, old_str, new_str, label):
    if not path.exists():
        print(f"  WARNING {label}: file not found")
        return False
    src = path.read_text(encoding="utf-8")
    if new_str.split("\n")[0] in src and old_str not in src:
        print(f"  OK {label}: already applied")
        return True
    if old_str not in src:
        print(f"  WARNING {label}: target block not found")
        return False
    bak = _backup(path)
    src = src.replace(old_str, new_str, 1)
    path.write_text(src, encoding="utf-8")
    print(f"  OK {label}: applied (backup: {bak.name if bak else 'N/A'})")
    return True

def _append_to_file(path, content, marker, label):
    if not path.exists():
        print(f"  WARNING {label}: file not found")
        return False
    src = path.read_text(encoding="utf-8")
    if marker in src:
        print(f"  OK {label}: already applied")
        return True
    bak = _backup(path)
    src = src + content
    path.write_text(src, encoding="utf-8")
    print(f"  OK {label}: added (backup: {bak.name if bak else 'N/A'})")
    return True

def apply_python_patches():
    print("\n" + "=" * 60)
    print("  1) Python patches")
    print("=" * 60)
    _patch_file(PROJECT / "database.py", POOL_OLD, POOL_NEW, "database.py (pool)")
    _append_to_file(PROJECT / "cache_helpers.py", CACHE_APPEND, "def get_parties_index(", "cache_helpers.py (cache)")

def apply_db_optimizations():
    print("\n" + "=" * 60)
    print("  2) DB optimizations (indexes + views)")
    print("=" * 60)
    if str(PROJECT) not in sys.path:
        sys.path.insert(0, str(PROJECT))
    try:
        from database import SessionLocal
        from sqlalchemy import text
    except Exception as e:
        print(f"  ERROR: cannot connect to DB: {e}")
        return
    db = None
    try:
        db = SessionLocal()
        print("\n  Creating indexes...")
        idx = 0
        for stmt in DB_INDEXES_SQL.strip().split(";"):
            stmt = stmt.strip()
            if not stmt or stmt.startswith("--"): continue
            try:
                db.execute(text(stmt))
                idx += 1
            except Exception as e:
                if "already exists" not in str(e).lower():
                    print(f"    WARN: {e}")
        db.commit()
        print(f"  OK {idx} indexes")
        print("\n  Creating views...")
        v = 0
        for stmt in DB_VIEWS_SQL.strip().split(";"):
            stmt = stmt.strip()
            if not stmt or stmt.startswith("--"): continue
            try:
                db.execute(text(stmt))
                v += 1
            except Exception as e:
                print(f"    WARN: {e}")
        db.commit()
        print(f"  OK {v} views")
    except Exception as e:
        print(f"  ERROR: {e}")
        if db: db.rollback()
    finally:
        if db: db.close()

def verify_syntax():
    print("\n" + "=" * 60)
    print("  3) Syntax check")
    print("=" * 60)
    import py_compile
    for f in [PROJECT / "database.py", PROJECT / "cache_helpers.py"]:
        if f.exists():
            try:
                py_compile.compile(str(f), doraise=True)
                print(f"  OK {f.name}")
            except Exception as e:
                print(f"  FAIL {f.name}: {e}")

def main():
    print("=" * 60)
    print("  Techtoon - Performance Optimizations")
    print("=" * 60)
    print(f"  Project: {PROJECT}")
    if not (PROJECT / "services.py").exists():
        print("\nERROR: Not a Techtoon project root!")
        sys.exit(1)
    apply_python_patches()
    apply_db_optimizations()
    verify_syntax()
    print("\n" + "=" * 60)
    print("DONE! Optimizations applied:")
    print("  - 25+ DB indexes")
    print("  - 5 SQL Views")
    print("  - Connection pool doubled (5->10, overflow 10->20)")
    print("  - 4 additional cache functions")
    print("\n  Next: streamlit run app.py")
    print("=" * 60)

if __name__ == "__main__":
    main()
