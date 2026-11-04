# migrate_to_neon.py — نقل البيانات من SQLite المحلي إلى Neon PostgreSQL
import sqlite3
from pathlib import Path

from database import Base, engine, SessionLocal
import models

# ==========================================
# 1. إنشاء الجداول في Neon
# ==========================================
print("🔨 جاري إنشاء الجداول في Neon...")
Base.metadata.create_all(bind=engine)
print("✅ تم إنشاء الجداول")

# ==========================================
# 2. فتح SQLite المحلي
# ==========================================
SQLITE_PATH = Path(__file__).parent / "accounting.db"
if not SQLITE_PATH.exists():
    print(f"❌ ملف SQLite مش موجود: {SQLITE_PATH}")
    exit(1)

sqlite_conn = sqlite3.connect(str(SQLITE_PATH))
sqlite_conn.row_factory = sqlite3.Row
cursor = sqlite_conn.cursor()
print(f"📂 تم فتح: {SQLITE_PATH}")

# ==========================================
# 3. جلسة Neon
# ==========================================
neon_db = SessionLocal()

# ==========================================
# 4. الجداول بالترتيب الصحيح (FK-safe)
# ==========================================
TABLES_ORDER = [
    # (اسم الجدول في SQLite, اسم الموديل في models)
    ("currencies", "Currency"),
    ("accounts", "Account"),
    ("users", "User"),
    ("accounting_periods", "AccountingPeriod"),
    ("parties", "Party"),
    ("cash_boxes", "CashBox"),
    ("categories", "Category"),
    ("items", "Item"),
    ("invoices", "Invoice"),
    ("invoice_lines", "InvoiceLine"),
    ("cost_history", "CostHistory"),
    ("kit_components", "KitComponent"),
    ("inventory_movements", "InventoryMovement"),
    ("cost_centers", "CostCenter"),
    ("cost_allocations", "CostAllocation"),
    ("cost_center_transactions", "CostCenterTransaction"),
    ("journal_entries", "JournalEntry"),
    ("journal_lines", "JournalLine"),
    ("payments", "Payment"),
    ("cash_transfers", "CashTransfer"),
    ("expense_categories", "ExpenseCategory"),
    ("expenses", "Expense"),
    ("warehouses", "Warehouse"),
    ("stock_levels", "StockLevel"),
    ("warehouse_transfers", "WarehouseTransfer"),
    ("stock_counts", "StockCount"),
    ("fixed_assets", "FixedAsset"),
    ("depreciation_records", "DepreciationRecord"),
    ("loans", "Loan"),
    ("loan_installments", "LoanInstallment"),
    ("employees", "Employee"),
    ("salary_records", "SalaryRecord"),
    ("budgets", "Budget"),
]

total_rows = 0

for table_name, model_name in TABLES_ORDER:
    try:
        cursor.execute(f"SELECT * FROM {table_name}")
        rows = cursor.fetchall()
        if not rows:
            print(f"⏭️  {table_name}: فارغ")
            continue

        model = getattr(models, model_name)
        data = [dict(row) for row in rows]

        # إدخال جماعي (bulk insert)
        neon_db.execute(model.__table__.insert(), data)
        neon_db.commit()
        total_rows += len(data)
        print(f"✅ {table_name}: {len(data)} صف")

    except Exception as e:
        neon_db.rollback()
        print(f"❌ {table_name}: {e}")

# ==========================================
# 5. تنظيف
# ==========================================
sqlite_conn.close()
neon_db.close()
print(f"\n🎉 تم نقل {total_rows} صف إجمالًا!")