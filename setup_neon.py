# setup_neon.py — إعداد Neon: إنشاء الجداول + نقل البيانات من SQLite
import sqlite3
from pathlib import Path
from database import Base, engine, SessionLocal
import models

print("=" * 60)
print("🔨 إعداد قاعدة بيانات Neon")
print("=" * 60)

# ==========================================
# 1. إنشاء الجداول في Neon
# ==========================================
print("\n📋 الخطوة 1: إنشاء الجداول في Neon...")
try:
    Base.metadata.create_all(bind=engine)
    print("✅ تم إنشاء الجداول")
except Exception as e:
    print(f"❌ فشل إنشاء الجداول: {e}")
    exit(1)

# ==========================================
# 2. فتح SQLite المحلي
# ==========================================
SQLITE_PATH = Path(__file__).parent / "accounting.db"
if not SQLITE_PATH.exists():
    print(f"\n⚠️ ملف SQLite مش موجود: {SQLITE_PATH}")
    print("🎉 الجداول بس اتعملت. مفيش بيانات هنقلها.")
    exit(0)

sqlite_conn = sqlite3.connect(str(SQLITE_PATH))
sqlite_conn.row_factory = sqlite3.Row
cursor = sqlite_conn.cursor()
print(f"\n📂 الخطوة 2: فتح SQLite: {SQLITE_PATH.name}")

# ==========================================
# 3. نقل البيانات
# ==========================================
print("\n🚚 الخطوة 3: نقل البيانات من SQLite → Neon...")

TABLES_ORDER = [
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

neon_db = SessionLocal()
total_rows = 0
skipped_tables = []
failed_tables = []

for table_name, model_name in TABLES_ORDER:
    try:
        # نتأكد إن الجدول موجود في SQLite
        cursor.execute(
            f"SELECT name FROM sqlite_master WHERE type='table' AND name='{table_name}'"
        )
        if not cursor.fetchone():
            skipped_tables.append(table_name)
            continue

        cursor.execute(f"SELECT * FROM {table_name}")
        rows = cursor.fetchall()
        if not rows:
            print(f"⏭️  {table_name}: فارغ")
            continue

        model = getattr(models, model_name)
        data = [dict(row) for row in rows]

        # إدخال جماعي
        neon_db.execute(model.__table__.insert(), data)
        neon_db.commit()
        total_rows += len(data)
        print(f"✅ {table_name}: {len(data)} صف")

    except Exception as e:
        neon_db.rollback()
        failed_tables.append((table_name, str(e)[:80]))
        print(f"❌ {table_name}: {str(e)[:80]}")

# ==========================================
# 4. تنظيف وتقرير نهائي
# ==========================================
sqlite_conn.close()
neon_db.close()

print("\n" + "=" * 60)
print(f"🎉 تم نقل {total_rows} صف إجمالًا")
if skipped_tables:
    print(f"⏭️  جداول متخطاة (مش موجودة في SQLite): {skipped_tables}")
if failed_tables:
    print(f"❌ جداول فشلت:")
    for t, err in failed_tables:
        print(f"    - {t}: {err}")
print("=" * 60)