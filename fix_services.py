# fix_services.py
# يصلح 3 دوال في services.py تلقائيًا (توافق PostgreSQL)
# - get_monthly_profitability
# - get_budget_vs_actual
# - run_annual_depreciation (الجزء اللي فيه strftime)
# ويضيف literal_column للاستيرادات.
#
# الاستخدام: python fix_services.py

import re
import shutil
from pathlib import Path

SERVICES = Path(__file__).parent / "services.py"
BACKUP = SERVICES.with_suffix(".py.before_pg_fix")

if not SERVICES.exists():
    print("❌ services.py غير موجود في نفس المجلد")
    exit(1)

# نسخة احتياطية
shutil.copy(SERVICES, BACKUP)
print(f"✅ نسخة احتياطية: {BACKUP.name}")

content = SERVICES.read_text(encoding="utf-8")
original_len = len(content)

# ==========================================
# 1) ضمان وجود literal_column في الاستيرادات
# ==========================================
if "literal_column" not in content:
    # ندور على "from sqlalchemy import"
    m = re.search(r"from sqlalchemy import ([^\n]+)", content)
    if m:
        imports = m.group(1)
        if "literal_column" not in imports:
            new_imports = imports.strip() + ", literal_column"
            content = content.replace(
                f"from sqlalchemy import {imports}",
                f"from sqlalchemy import {new_imports}",
                1,
            )
            print("✅ أضفنا literal_column للاستيرادات")
    else:
        # مفيش from sqlalchemy import خالص → نضيف واحد
        content = "from sqlalchemy import literal_column\n" + content
        print("✅ أضفنا سطر استيراد literal_column جديد")
else:
    print("ℹ️ literal_column موجود مسبقًا")

# ==========================================
# 2) استبدال get_monthly_profitability
# ==========================================
NEW_MONTHLY = '''def get_monthly_profitability(year=None):
    """الربحية الشهرية — نسخة متوافقة مع PostgreSQL."""
    db = SessionLocal()
    try:
        if year is None:
            year = datetime.now().year

        month_expr = func.to_char(models.Invoice.date, literal_column("'MM'"))
        year_expr = func.to_char(models.Invoice.date, literal_column("'YYYY'"))

        query = db.query(
            month_expr.label('month'),
            func.sum(models.Invoice.net_amount).label('revenue'),
            func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price).label('cost'),
        ).join(
            models.InvoiceLine, models.Invoice.id == models.InvoiceLine.invoice_id
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            year_expr == str(year),
        ).group_by(
            month_expr
        ).order_by(
            month_expr
        ).all()

        monthly_data = []
        for r in query:
            profit = (r.revenue or 0) - (r.cost or 0)
            profit_margin = (profit / r.revenue * 100) if r.revenue and r.revenue > 0 else 0
            monthly_data.append({
                'month': r.month,
                'month_name': datetime.strptime(r.month, '%m').strftime('%B'),
                'revenue': r.revenue or 0,
                'cost': r.cost or 0,
                'profit': profit,
                'profit_margin': profit_margin,
            })
        return monthly_data
    finally:
        db.close()
'''

# ==========================================
# 3) استبدال get_budget_vs_actual
# ==========================================
NEW_BUDGET = '''def get_budget_vs_actual(month, year):
    """مقارنة الموازنة بالفعلي — نسخة متوافقة مع PostgreSQL."""
    db = SessionLocal()
    try:
        budgets = db.query(Budget).filter(
            Budget.month == month,
            Budget.year == year,
        ).all()

        comparison = []
        month_expr = func.to_char(models.JournalEntry.date, literal_column("'MM'"))
        year_expr = func.to_char(models.JournalEntry.date, literal_column("'YYYY'"))

        for budget in budgets:
            account = db.query(models.Account).filter(
                models.Account.id == budget.account_id
            ).first()

            actual = db.query(
                func.sum(models.JournalLine.debit - models.JournalLine.credit)
            ).filter(
                models.JournalLine.account_id == budget.account_id,
                month_expr == f"{month:02d}",
                year_expr == str(year),
            ).join(models.JournalEntry).scalar() or 0.0

            variance = budget.budgeted_amount - actual
            variance_percentage = (variance / budget.budgeted_amount * 100) if budget.budgeted_amount > 0 else 0

            comparison.append({
                'account_code': account.code if account else '-',
                'account_name': account.name if account else '-',
                'budgeted': budget.budgeted_amount,
                'actual': actual,
                'variance': variance,
                'variance_percentage': variance_percentage,
            })
        return comparison
    finally:
        db.close()
'''

# دوال الاستبدال
def replace_function(content, func_name, new_code):
    """يستبدل دالة كاملة بنسختها الجديدة."""
    pattern = rf"def {func_name}\([^)]*\):.*?(?=\ndef |\Z)"
    new_content, count = re.subn(pattern, new_code + "\n\n", content, count=1, flags=re.DOTALL)
    if count == 0:
        print(f"⚠️ لم نجد دالة {func_name} — تحقق يدويًا")
        return content, False
    print(f"✅ استبدلنا {func_name}")
    return new_content, True

content, ok1 = replace_function(content, "get_monthly_profitability", NEW_MONTHLY)
content, ok2 = replace_function(content, "get_budget_vs_actual", NEW_BUDGET)

# ==========================================
# 4) استبدال الجزء اللي فيه strftime في run_annual_depreciation
# ==========================================
old_pattern = (
    "existing = db.query(DepreciationRecord).filter(\n"
    "        DepreciationRecord.asset_id == asset_id,\n"
    "        func.strftime('%Y', DepreciationRecord.date) == str(year)\n"
    "    ).first()"
)

new_snippet = (
    "year_expr = func.to_char(DepreciationRecord.date, literal_column(\"'YYYY'\"))\n"
    "        existing = db.query(DepreciationRecord).filter(\n"
    "            DepreciationRecord.asset_id == asset_id,\n"
    "            year_expr == str(year)\n"
    "        ).first()"
)

if old_pattern in content:
    content = content.replace(old_pattern, new_snippet, 1)
    print("✅ عدّلنا strftime في run_annual_depreciation")
else:
    # نحاول نلاقي بمرونة أكبر
    fallback = re.search(
        r"existing = db\.query\(DepreciationRecord\)\.filter\((.+?)\)\.first\(\)",
        content, flags=re.DOTALL
    )
    if fallback and "func.strftime" in fallback.group(1):
        old_block = fallback.group(0)
        new_block = (
            "year_expr = func.to_char(DepreciationRecord.date, literal_column(\"'YYYY'\"))\n"
            "        existing = db.query(DepreciationRecord).filter(\n"
            "            DepreciationRecord.asset_id == asset_id,\n"
            "            year_expr == str(year)\n"
            "        ).first()"
        )
        content = content.replace(old_block, new_block, 1)
        print("✅ عدّلنا strftime في run_annual_depreciation (بمرونة)")
    else:
        print("⚠️ لم نجد كتلة strftime في run_annual_depreciation — تحقق يدويًا")

# ==========================================
# 5) كتابة الملف النهائي
# ==========================================
SERVICES.write_text(content, encoding="utf-8")
print()
print("=" * 60)
print(f"📊 الحجم قبل: {original_len:,} | بعد: {len(content):,}")
print("🎉 تم الانتهاء! الملف محدّث.")
print(f"💾 نسخة احتياطية محفوظة في: {BACKUP.name}")
print("=" * 60)
print()
print("الخطوة التالية: شغّل البرنامج وتأكد إن الصفحات شغالة.")