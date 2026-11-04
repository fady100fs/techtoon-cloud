# seed_data.py
from database import SessionLocal, engine
import models
from models import AccountType, Currency

def initialize_default_accounts():
    models.Base.metadata.create_all(bind=engine)
    
    db = SessionLocal()
    
    if db.query(models.Account).count() > 0:
        print("شجرة الحسابات موجودة مسبقاً. تم التخطي.")
        db.close()
        return

    print("جاري إنشاء شجرة الحسابات الافتراضية...")

    # 1. إنشاء العملات الافتراضية
    egp = Currency(code="EGP", name="جنيه مصري", symbol="ج.م", exchange_rate=1.0, is_default=True)
    usd = Currency(code="USD", name="دولار أمريكي", symbol="$", exchange_rate=48.5, is_default=False)
    sar = Currency(code="SAR", name="ريال سعودي", symbol="ر.س", exchange_rate=12.9, is_default=False)
    
    db.add_all([egp, usd, sar])
    db.commit()
    print("✅ تم إنشاء العملات الافتراضية (جنيه مصري، دولار، ريال سعودي)")

    # 2. الحسابات الرئيسية (آباء)
    assets = models.Account(code="1000", name="الأصول", type=AccountType.ASSET)
    liabilities = models.Account(code="2000", name="الخصوم", type=AccountType.LIABILITY)
    equity = models.Account(code="3000", name="حقوق الملكية", type=AccountType.EQUITY)
    revenues = models.Account(code="4000", name="الإيرادات", type=AccountType.REVENUE)
    expenses = models.Account(code="5000", name="المصروفات", type=AccountType.EXPENSE)

    db.add_all([assets, liabilities, equity, revenues, expenses])
    db.commit()

    # 3. الحسابات الفرعية
    cash = models.Account(code="1101", name="النقدية", type=AccountType.ASSET, parent_id=assets.id)
    bank = models.Account(code="1102", name="البنك", type=AccountType.ASSET, parent_id=assets.id)
    accounts_receivable = models.Account(code="1201", name="حسابات العملاء (المدينون)", type=AccountType.ASSET, parent_id=assets.id)
    inventory = models.Account(code="1301", name="المخزون", type=AccountType.ASSET, parent_id=assets.id)
    
    accounts_payable = models.Account(code="2101", name="حسابات الموردين (الدائنون)", type=AccountType.LIABILITY, parent_id=liabilities.id)
    
    opening_balance = models.Account(code="3101", name="أرصدة افتتاحية", type=AccountType.EQUITY, parent_id=equity.id)
    capital = models.Account(code="3201", name="رأس المال", type=AccountType.EQUITY, parent_id=equity.id)
    
    sales_revenue = models.Account(code="4101", name="إيرادات المبيعات", type=AccountType.REVENUE, parent_id=revenues.id)
    
    cogs = models.Account(code="5101", name="تكلفة البضاعة المباعة (COGS)", type=AccountType.EXPENSE, parent_id=expenses.id)
    rent_expense = models.Account(code="5201", name="مصروف الإيجار", type=AccountType.EXPENSE, parent_id=expenses.id)
    salaries_expense = models.Account(code="5202", name="مصروف الرواتب", type=AccountType.EXPENSE, parent_id=expenses.id)

    sub_accounts = [
        cash, bank, accounts_receivable, inventory,
        accounts_payable,
        opening_balance, capital,
        sales_revenue,
        cogs, rent_expense, salaries_expense
    ]
    
    db.add_all(sub_accounts)
    db.commit()
    db.close()
    print("✅ تم إنشاء شجرة الحسابات الافتراضية بنجاح!")

if __name__ == "__main__":
    initialize_default_accounts()