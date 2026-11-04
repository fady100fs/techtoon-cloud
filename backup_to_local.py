# backup_to_local.py
# Export all database tables from Neon to CSV files (local backup).

import sys
from pathlib import Path
from datetime import datetime

# Add project root to path
sys.path.append(str(Path(__file__).resolve().parent))

import pandas as pd
from sqlalchemy import text
from database import engine

BACKUP_ROOT = Path(__file__).parent / "backups"
BACKUP_ROOT.mkdir(exist_ok=True)

TABLES = [
    "currencies", "accounts", "users", "accounting_periods",
    "parties", "cash_boxes", "categories", "items",
    "invoices", "invoice_lines", "cost_history", "kit_components",
    "inventory_movements", "cost_centers", "cost_allocations",
    "cost_center_transactions", "journal_entries", "journal_lines",
    "payments", "cash_transfers", "expense_categories", "expenses",
    "warehouses", "stock_levels", "warehouse_transfers", "stock_counts",
    "fixed_assets", "depreciation_records", "loans", "loan_installments",
    "employees", "salary_records", "budgets",
]

timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
backup_folder = BACKUP_ROOT / f"backup_{timestamp}"
backup_folder.mkdir(exist_ok=True)

print("=" * 60)
print(f"Backup started: {timestamp}")
print(f"Destination: {backup_folder}")
print("=" * 60)

total_rows = 0
failed = []

with engine.connect() as conn:
    for table in TABLES:
        try:
            df = pd.read_sql(text(f"SELECT * FROM {table}"), conn)
            csv_path = backup_folder / f"{table}.csv"
            df.to_csv(csv_path, index=False, encoding="utf-8-sig")
            total_rows += len(df)
            print(f"OK    {table:30} {len(df):>6} rows")
        except Exception as e:
            failed.append((table, str(e)[:60]))
            print(f"FAIL  {table:30} {str(e)[:60]}")

print()
print("=" * 60)
print(f"Done. Total rows: {total_rows}")
if failed:
    print(f"Failed tables: {len(failed)}")
    for t, e in failed:
        print(f"  - {t}: {e}")
print("=" * 60)

# Optionally delete backups older than 30 days
from datetime import timedelta
cutoff = datetime.now() - timedelta(days=30)
deleted = 0
for folder in BACKUP_ROOT.glob("backup_*"):
    try:
        # Extract timestamp from folder name
        ts_str = folder.name.replace("backup_", "")
        folder_date = datetime.strptime(ts_str, "%Y%m%d_%H%M%S")
        if folder_date < cutoff:
            import shutil
            shutil.rmtree(folder)
            deleted += 1
    except Exception:
        pass

if deleted:
    print(f"Cleaned up {deleted} old backups (older than 30 days).")