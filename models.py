# models.py
from sqlalchemy import Column, Integer, String, Float, Boolean, ForeignKey, DateTime, Enum, Text, UniqueConstraint
from sqlalchemy.orm import relationship
from datetime import datetime
import enum
from database import Base

# ==================== 1. العملات ====================
class Currency(Base):
    __tablename__ = 'currencies'
    
    id = Column(Integer, primary_key=True, index=True)
    code = Column(String(10), unique=True, nullable=False)
    name = Column(String(50), nullable=False)
    symbol = Column(String(10), nullable=False)
    exchange_rate = Column(Float, default=1.0)
    is_default = Column(Boolean, default=False)

# ==================== 2. شجرة الحسابات ====================
class AccountType(enum.Enum):
    ASSET = "أصول"
    LIABILITY = "خصوم"
    EQUITY = "حقوق ملكية"
    REVENUE = "إيرادات"
    EXPENSE = "مصروفات"

class Account(Base):
    __tablename__ = 'accounts'
    
    id = Column(Integer, primary_key=True, index=True)
    code = Column(String(20), unique=True, nullable=False, index=True)
    name = Column(String(100), nullable=False)
    type = Column(Enum(AccountType), nullable=False)
    parent_id = Column(Integer, ForeignKey('accounts.id'), nullable=True)
    
    parent = relationship("Account", remote_side=[id], backref="children")
    journal_lines = relationship("JournalLine", back_populates="account")

# ==================== 3. المستخدمون ====================
class UserRole(enum.Enum):
    ADMIN = "مدير"
    ACCOUNTANT = "محاسب"
    SALESPERSON = "بائع"
    VIEWER = "مشاهد"

class User(Base):
    __tablename__ = 'users'
    
    id = Column(Integer, primary_key=True, index=True)
    username = Column(String(50), unique=True, nullable=False)
    password_hash = Column(String(255), nullable=False)
    full_name = Column(String(100), nullable=False)
    email = Column(String(100), nullable=True)
    role = Column(Enum(UserRole), nullable=False, default=UserRole.VIEWER)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    last_login = Column(DateTime, nullable=True)

# ==================== 4. الفترات المحاسبية ====================
class PeriodStatus(enum.Enum):
    OPEN = "مفتوح"
    CLOSED = "مغلق"
    LOCKED = "مقفل"

class AccountingPeriod(Base):
    __tablename__ = 'accounting_periods'
    
    id = Column(Integer, primary_key=True, index=True)
    period_name = Column(String(50), nullable=False)
    start_date = Column(DateTime, nullable=False)
    end_date = Column(DateTime, nullable=False)
    status = Column(Enum(PeriodStatus), default=PeriodStatus.OPEN)
    is_fiscal_year = Column(Boolean, default=False)
    closing_date = Column(DateTime, nullable=True)
    closing_notes = Column(Text, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    closed_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    journal_entries = relationship("JournalEntry", back_populates="period")

# ==================== 5. العملاء والموردين ====================
class Party(Base):
    __tablename__ = 'parties'
    
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(100), nullable=False)
    type = Column(String(20), nullable=False)
    phone = Column(String(20), nullable=True)
    address = Column(Text, nullable=True)
    credit_limit = Column(Float, default=0.0)
    account_id = Column(Integer, ForeignKey('accounts.id'), nullable=False)
    
    account = relationship("Account")
    invoices = relationship("Invoice", back_populates="party")

# ==================== 6. الخزائن ====================
class CashBoxType(enum.Enum):
    CASH = "نقدية"
    BANK = "بنك"
    WALLET = "محفظة إلكترونية"
    OTHER = "أخرى"

class CashBox(Base):
    __tablename__ = 'cash_boxes'
    
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(100), nullable=False)
    code = Column(String(20), unique=True, nullable=False)
    type = Column(Enum(CashBoxType), nullable=False, default=CashBoxType.CASH)
    account_id = Column(Integer, ForeignKey('accounts.id'), nullable=False)
    responsible_person = Column(String(100), nullable=True)
    max_limit = Column(Float, nullable=True)
    is_active = Column(Boolean, default=True)
    notes = Column(Text, nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    account = relationship("Account")
    payments = relationship("Payment", back_populates="cash_box")
    transfers_from = relationship("CashTransfer", foreign_keys="CashTransfer.from_cash_box_id", back_populates="from_cash_box")
    transfers_to = relationship("CashTransfer", foreign_keys="CashTransfer.to_cash_box_id", back_populates="to_cash_box")

# ==================== 7. التصنيفات ====================
class Category(Base):
    __tablename__ = 'categories'
    
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(100), nullable=False, unique=True)
    description = Column(Text, nullable=True)
    parent_id = Column(Integer, ForeignKey('categories.id'), nullable=True)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    parent = relationship("Category", remote_side=[id], backref="children")
    items = relationship("Item", back_populates="category")

# ==================== 8. الأصناف ====================
class Item(Base):
    __tablename__ = 'items'
    
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(100), nullable=False)
    barcode = Column(String(50), nullable=True)
    cost_price = Column(Float, default=0.0)
    sell_price = Column(Float, default=0.0)
    is_kit = Column(Boolean, default=False)
    min_stock = Column(Float, default=10.0)
    category_id = Column(Integer, ForeignKey('categories.id'), nullable=True)
    
    avg_cost_price = Column(Float, default=0.0)
    total_purchased_qty = Column(Float, default=0.0)
    
    inventory_account_id = Column(Integer, ForeignKey('accounts.id'), nullable=True)
    cogs_account_id = Column(Integer, ForeignKey('accounts.id'), nullable=True)
    revenue_account_id = Column(Integer, ForeignKey('accounts.id'), nullable=True)
    
    category = relationship("Category", back_populates="items")
    components = relationship("KitComponent", foreign_keys="KitComponent.kit_item_id", back_populates="kit_item")
    used_in_kits = relationship("KitComponent", foreign_keys="KitComponent.component_item_id", back_populates="component_item")
    cost_history = relationship("CostHistory", back_populates="item")

# ==================== 9. سجل تكاليف الشراء ====================
class CostHistory(Base):
    __tablename__ = 'cost_history'
    
    id = Column(Integer, primary_key=True, index=True)
    item_id = Column(Integer, ForeignKey('items.id'), nullable=False)
    invoice_id = Column(Integer, ForeignKey('invoices.id'), nullable=False)
    purchase_price = Column(Float, nullable=False)
    quantity = Column(Float, nullable=False)
    total_cost = Column(Float, nullable=False)
    purchase_date = Column(DateTime, nullable=False)
    supplier_id = Column(Integer, ForeignKey('parties.id'), nullable=True)
    
    item = relationship("Item", back_populates="cost_history")
    invoice = relationship("Invoice")
    supplier = relationship("Party")

# ==================== 10. مكونات الأصناف المدمجة ====================
class KitComponent(Base):
    __tablename__ = 'kit_components'
    
    id = Column(Integer, primary_key=True, index=True)
    kit_item_id = Column(Integer, ForeignKey('items.id'), nullable=False)
    component_item_id = Column(Integer, ForeignKey('items.id'), nullable=False)
    quantity = Column(Float, nullable=False, default=1.0)
    
    kit_item = relationship("Item", foreign_keys=[kit_item_id], back_populates="components")
    component_item = relationship("Item", foreign_keys=[component_item_id], back_populates="used_in_kits")

# ==================== 11. حركات المخزون ====================
class InventoryMovement(Base):
    __tablename__ = 'inventory_movements'
    
    id = Column(Integer, primary_key=True, index=True)
    item_id = Column(Integer, ForeignKey('items.id'), nullable=False)
    date = Column(DateTime, default=datetime.utcnow)
    type = Column(String(20), nullable=False)
    quantity = Column(Float, nullable=False)
    reference_id = Column(Integer, nullable=True)
    
    item = relationship("Item")

# ==================== 12. مراكز التكلفة (جديد) ====================
class CostCenter(Base):
    __tablename__ = 'cost_centers'
    
    id = Column(Integer, primary_key=True, index=True)
    code = Column(String(20), unique=True, nullable=False)
    name = Column(String(100), nullable=False)
    description = Column(Text, nullable=True)
    manager_name = Column(String(100), nullable=True)
    parent_id = Column(Integer, ForeignKey('cost_centers.id'), nullable=True)
    is_active = Column(Boolean, default=True)
    budget_limit = Column(Float, nullable=True)
    notes = Column(Text, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    parent = relationship("CostCenter", remote_side=[id], backref="children")
    allocations = relationship("CostAllocation", back_populates="cost_center")
    transactions = relationship("CostCenterTransaction", back_populates="cost_center")

class CostAllocation(Base):
    __tablename__ = 'cost_allocations'
    
    id = Column(Integer, primary_key=True, index=True)
    cost_center_id = Column(Integer, ForeignKey('cost_centers.id'), nullable=False)
    account_id = Column(Integer, ForeignKey('accounts.id'), nullable=False)
    allocation_percentage = Column(Float, nullable=False)
    month = Column(Integer, nullable=False)
    year = Column(Integer, nullable=False)
    notes = Column(Text, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    cost_center = relationship("CostCenter", back_populates="allocations")
    account = relationship("Account")

class CostCenterTransaction(Base):
    __tablename__ = 'cost_center_transactions'
    
    id = Column(Integer, primary_key=True, index=True)
    cost_center_id = Column(Integer, ForeignKey('cost_centers.id'), nullable=False)
    date = Column(DateTime, nullable=False)
    description = Column(String(255), nullable=False)
    amount = Column(Float, nullable=False)
    transaction_type = Column(String(20), nullable=False)  # expense, revenue, transfer
    reference_type = Column(String(50), nullable=True)
    reference_id = Column(Integer, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    cost_center = relationship("CostCenter", back_populates="transactions")

# ==================== 13. القيود اليومية ====================
class JournalEntry(Base):
    __tablename__ = 'journal_entries'
    
    id = Column(Integer, primary_key=True, index=True)
    date = Column(DateTime, default=datetime.utcnow)
    description = Column(String(255), nullable=True)
    reference_type = Column(String(50), nullable=True)
    reference_id = Column(Integer, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    period_id = Column(Integer, ForeignKey('accounting_periods.id'), nullable=True)
    
    lines = relationship("JournalLine", back_populates="entry", cascade="all, delete-orphan")
    period = relationship("AccountingPeriod", back_populates="journal_entries")

class JournalLine(Base):
    __tablename__ = 'journal_lines'
    
    id = Column(Integer, primary_key=True, index=True)
    entry_id = Column(Integer, ForeignKey('journal_entries.id'), nullable=False)
    account_id = Column(Integer, ForeignKey('accounts.id'), nullable=False)
    debit = Column(Float, default=0.0)
    credit = Column(Float, default=0.0)
    
    entry = relationship("JournalEntry", back_populates="lines")
    account = relationship("Account", back_populates="journal_lines")

# ==================== 14. الفواتير ====================
class Invoice(Base):
    __tablename__ = 'invoices'
    
    id = Column(Integer, primary_key=True, index=True)
    invoice_number = Column(String(50), unique=True, nullable=False)
    date = Column(DateTime, default=datetime.utcnow)
    due_date = Column(DateTime, nullable=True)
    party_id = Column(Integer, ForeignKey('parties.id'), nullable=False)
    type = Column(String(20), nullable=False)
    total_amount = Column(Float, default=0.0)
    status = Column(String(20), default='pending')
    currency_id = Column(Integer, ForeignKey('currencies.id'), nullable=False, default=1)
    
    discount_amount = Column(Float, default=0.0)
    discount_percentage = Column(Float, default=0.0)
    tax_rate = Column(Float, default=14.0)
    tax_amount = Column(Float, default=0.0)
    net_amount = Column(Float, default=0.0)
    
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    
    party = relationship("Party", back_populates="invoices")
    lines = relationship("InvoiceLine", back_populates="invoice", cascade="all, delete-orphan")
    currency = relationship("Currency")

class InvoiceLine(Base):
    __tablename__ = 'invoice_lines'
    
    id = Column(Integer, primary_key=True, index=True)
    invoice_id = Column(Integer, ForeignKey('invoices.id'), nullable=False)
    item_id = Column(Integer, ForeignKey('items.id'), nullable=False)
    quantity = Column(Float, nullable=False)
    price = Column(Float, nullable=False)
    total = Column(Float, nullable=False)
    cost_price = Column(Float, default=0.0)
    
    invoice = relationship("Invoice", back_populates="lines")
    item = relationship("Item")

# ==================== 15. المدفوعات ====================
class Payment(Base):
    __tablename__ = 'payments'
    
    id = Column(Integer, primary_key=True, index=True)
    date = Column(DateTime, default=datetime.utcnow)
    party_id = Column(Integer, ForeignKey('parties.id'), nullable=False)
    amount = Column(Float, nullable=False)
    payment_type = Column(String(20), nullable=False)
    payment_method = Column(String(50), nullable=False)
    reference_number = Column(String(100), nullable=True)
    notes = Column(Text, nullable=True)
    currency_id = Column(Integer, ForeignKey('currencies.id'), nullable=False, default=1)
    cash_box_id = Column(Integer, ForeignKey('cash_boxes.id'), nullable=False, default=1)
    
    party = relationship("Party")
    currency = relationship("Currency")
    cash_box = relationship("CashBox", back_populates="payments")

# ==================== 16. التحويلات بين الخزائن ====================
class CashTransfer(Base):
    __tablename__ = 'cash_transfers'
    
    id = Column(Integer, primary_key=True, index=True)
    date = Column(DateTime, default=datetime.utcnow)
    from_cash_box_id = Column(Integer, ForeignKey('cash_boxes.id'), nullable=False)
    to_cash_box_id = Column(Integer, ForeignKey('cash_boxes.id'), nullable=False)
    amount = Column(Float, nullable=False)
    notes = Column(Text, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    
    from_cash_box = relationship("CashBox", foreign_keys=[from_cash_box_id], back_populates="transfers_from")
    to_cash_box = relationship("CashBox", foreign_keys=[to_cash_box_id], back_populates="transfers_to")

# ==================== 17. المصروفات ====================
class ExpenseCategory(Base):
    __tablename__ = 'expense_categories'
    
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(100), nullable=False, unique=True)
    description = Column(Text, nullable=True)
    account_id = Column(Integer, ForeignKey('accounts.id'), nullable=False)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    account = relationship("Account")
    expenses = relationship("Expense", back_populates="category")

class Expense(Base):
    __tablename__ = 'expenses'
    
    id = Column(Integer, primary_key=True, index=True)
    date = Column(DateTime, default=datetime.utcnow)
    category_id = Column(Integer, ForeignKey('expense_categories.id'), nullable=False)
    amount = Column(Float, nullable=False)
    description = Column(Text, nullable=False)
    payment_method = Column(String(50), nullable=False)
    cash_box_id = Column(Integer, ForeignKey('cash_boxes.id'), nullable=False)
    reference_number = Column(String(100), nullable=True)
    receipt_image = Column(String(255), nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    notes = Column(Text, nullable=True)
    
    category = relationship("ExpenseCategory", back_populates="expenses")
    cash_box = relationship("CashBox")

# ==================== 18. المخازن ====================
class Warehouse(Base):
    __tablename__ = 'warehouses'
    
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(100), nullable=False, unique=True)
    code = Column(String(20), unique=True, nullable=False)
    location = Column(String(200), nullable=True)
    responsible_person = Column(String(100), nullable=True)
    is_active = Column(Boolean, default=True)
    notes = Column(Text, nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    stock_levels = relationship("StockLevel", back_populates="warehouse")
    transfers_from = relationship("WarehouseTransfer", foreign_keys="WarehouseTransfer.from_warehouse_id", back_populates="from_warehouse")
    transfers_to = relationship("WarehouseTransfer", foreign_keys="WarehouseTransfer.to_warehouse_id", back_populates="to_warehouse")

class StockLevel(Base):
    __tablename__ = 'stock_levels'
    
    id = Column(Integer, primary_key=True, index=True)
    warehouse_id = Column(Integer, ForeignKey('warehouses.id'), nullable=False)
    item_id = Column(Integer, ForeignKey('items.id'), nullable=False)
    quantity = Column(Float, default=0.0)
    last_updated = Column(DateTime, default=datetime.utcnow)
    
    warehouse = relationship("Warehouse", back_populates="stock_levels")
    item = relationship("Item")
    
    __table_args__ = (
        UniqueConstraint('warehouse_id', 'item_id', name='uq_warehouse_item'),
    )

class WarehouseTransfer(Base):
    __tablename__ = 'warehouse_transfers'
    
    id = Column(Integer, primary_key=True, index=True)
    date = Column(DateTime, default=datetime.utcnow)
    from_warehouse_id = Column(Integer, ForeignKey('warehouses.id'), nullable=False)
    to_warehouse_id = Column(Integer, ForeignKey('warehouses.id'), nullable=False)
    item_id = Column(Integer, ForeignKey('items.id'), nullable=False)
    quantity = Column(Float, nullable=False)
    notes = Column(Text, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    status = Column(String(20), default='completed')
    
    from_warehouse = relationship("Warehouse", foreign_keys=[from_warehouse_id], back_populates="transfers_from")
    to_warehouse = relationship("Warehouse", foreign_keys=[to_warehouse_id], back_populates="transfers_to")
    item = relationship("Item")

class StockCount(Base):
    __tablename__ = 'stock_counts'
    
    id = Column(Integer, primary_key=True, index=True)
    date = Column(DateTime, default=datetime.utcnow)
    warehouse_id = Column(Integer, ForeignKey('warehouses.id'), nullable=False)
    item_id = Column(Integer, ForeignKey('items.id'), nullable=False)
    system_quantity = Column(Float, nullable=False)
    counted_quantity = Column(Float, nullable=False)
    difference = Column(Float, nullable=False)
    notes = Column(Text, nullable=True)
    counted_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    
    warehouse = relationship("Warehouse")
    item = relationship("Item")

# ==================== 19. الأصول الثابتة ====================
class DepreciationMethod(enum.Enum):
    STRAIGHT_LINE = "خطي"
    DECLINING_BALANCE = "متناقص"
    UNITS_OF_PRODUCTION = "وحدات الإنتاج"

class FixedAsset(Base):
    __tablename__ = 'fixed_assets'
    
    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(100), nullable=False)
    code = Column(String(20), unique=True, nullable=False)
    category = Column(String(50), nullable=False)
    purchase_date = Column(DateTime, nullable=False)
    purchase_cost = Column(Float, nullable=False)
    salvage_value = Column(Float, default=0.0)
    useful_life_years = Column(Integer, nullable=False)
    depreciation_method = Column(Enum(DepreciationMethod), nullable=False, default=DepreciationMethod.STRAIGHT_LINE)
    accumulated_depreciation = Column(Float, default=0.0)
    net_book_value = Column(Float, nullable=False)
    location = Column(String(200), nullable=True)
    responsible_person = Column(String(100), nullable=True)
    status = Column(String(20), default='active')
    notes = Column(Text, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    depreciation_records = relationship("DepreciationRecord", back_populates="asset")

class DepreciationRecord(Base):
    __tablename__ = 'depreciation_records'
    
    id = Column(Integer, primary_key=True, index=True)
    asset_id = Column(Integer, ForeignKey('fixed_assets.id'), nullable=False)
    date = Column(DateTime, nullable=False)
    depreciation_amount = Column(Float, nullable=False)
    accumulated_depreciation = Column(Float, nullable=False)
    net_book_value = Column(Float, nullable=False)
    notes = Column(Text, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    
    asset = relationship("FixedAsset", back_populates="depreciation_records")

# ==================== 20. القروض ====================
class LoanType(enum.Enum):
    RECEIVED = "قرض مستلم"
    GIVEN = "قرض ممنوح"

class LoanStatus(enum.Enum):
    ACTIVE = "نشط"
    COMPLETED = "مكتمل"
    DEFAULTED = "متعثر"
    CANCELLED = "ملغي"

class Loan(Base):
    __tablename__ = 'loans'
    
    id = Column(Integer, primary_key=True, index=True)
    loan_number = Column(String(50), unique=True, nullable=False)
    loan_type = Column(Enum(LoanType), nullable=False)
    borrower_name = Column(String(100), nullable=False)
    borrower_type = Column(String(50), nullable=True)
    principal_amount = Column(Float, nullable=False)
    interest_rate = Column(Float, default=0.0)
    loan_date = Column(DateTime, nullable=False)
    maturity_date = Column(DateTime, nullable=False)
    term_months = Column(Integer, nullable=False)
    installment_amount = Column(Float, nullable=False)
    total_interest = Column(Float, default=0.0)
    total_amount = Column(Float, nullable=False)
    paid_amount = Column(Float, default=0.0)
    remaining_amount = Column(Float, nullable=False)
    status = Column(Enum(LoanStatus), default=LoanStatus.ACTIVE)
    cash_box_id = Column(Integer, ForeignKey('cash_boxes.id'), nullable=True)
    notes = Column(Text, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    installments = relationship("LoanInstallment", back_populates="loan")
    cash_box = relationship("CashBox")

class LoanInstallment(Base):
    __tablename__ = 'loan_installments'
    
    id = Column(Integer, primary_key=True, index=True)
    loan_id = Column(Integer, ForeignKey('loans.id'), nullable=False)
    installment_number = Column(Integer, nullable=False)
    due_date = Column(DateTime, nullable=False)
    principal_amount = Column(Float, nullable=False)
    interest_amount = Column(Float, default=0.0)
    total_amount = Column(Float, nullable=False)
    paid_amount = Column(Float, default=0.0)
    paid_date = Column(DateTime, nullable=True)
    status = Column(String(20), default='pending')
    notes = Column(Text, nullable=True)
    
    loan = relationship("Loan", back_populates="installments")

# ==================== 21. الموظفون ====================
class EmploymentStatus(enum.Enum):
    ACTIVE = "يعمل"
    ON_LEAVE = "إجازة"
    TERMINATED = "منتهي"
    RETIRED = "متقاعد"

class Employee(Base):
    __tablename__ = 'employees'
    
    id = Column(Integer, primary_key=True, index=True)
    employee_code = Column(String(20), unique=True, nullable=False)
    full_name = Column(String(100), nullable=False)
    national_id = Column(String(20), nullable=True)
    phone = Column(String(20), nullable=True)
    email = Column(String(100), nullable=True)
    address = Column(Text, nullable=True)
    job_title = Column(String(100), nullable=False)
    department = Column(String(100), nullable=True)
    hire_date = Column(DateTime, nullable=False)
    basic_salary = Column(Float, nullable=False)
    allowances = Column(Float, default=0.0)
    deductions = Column(Float, default=0.0)
    social_insurance = Column(Float, default=0.0)
    tax_rate = Column(Float, default=0.0)
    bank_account = Column(String(50), nullable=True)
    bank_name = Column(String(100), nullable=True)
    status = Column(Enum(EmploymentStatus), default=EmploymentStatus.ACTIVE)
    notes = Column(Text, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    salary_records = relationship("SalaryRecord", back_populates="employee")

class SalaryRecord(Base):
    __tablename__ = 'salary_records'
    
    id = Column(Integer, primary_key=True, index=True)
    employee_id = Column(Integer, ForeignKey('employees.id'), nullable=False)
    month = Column(Integer, nullable=False)
    year = Column(Integer, nullable=False)
    basic_salary = Column(Float, nullable=False)
    allowances = Column(Float, default=0.0)
    overtime = Column(Float, default=0.0)
    bonus = Column(Float, default=0.0)
    gross_salary = Column(Float, nullable=False)
    social_insurance = Column(Float, default=0.0)
    tax = Column(Float, default=0.0)
    other_deductions = Column(Float, default=0.0)
    total_deductions = Column(Float, default=0.0)
    net_salary = Column(Float, nullable=False)
    payment_date = Column(DateTime, nullable=True)
    payment_method = Column(String(50), nullable=True)
    status = Column(String(20), default='pending')
    notes = Column(Text, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    employee = relationship("Employee", back_populates="salary_records")

# ==================== 22. الموازنات ====================
class Budget(Base):
    __tablename__ = 'budgets'
    
    id = Column(Integer, primary_key=True, index=True)
    account_id = Column(Integer, ForeignKey('accounts.id'), nullable=False)
    month = Column(Integer, nullable=False)
    year = Column(Integer, nullable=False)
    budgeted_amount = Column(Float, nullable=False)
    notes = Column(Text, nullable=True)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)
    
    account = relationship("Account")
    
    __table_args__ = (
        UniqueConstraint('account_id', 'month', 'year', name='uq_budget_account_month_year'),
    )
# ==================== 23. الفواتير المتكررة (جديد) ====================
class RecurrenceFrequency(enum.Enum):
    DAILY = "يومي"
    WEEKLY = "أسبوعي"
    BIWEEKLY = "كل أسبوعين"
    MONTHLY = "شهري"
    QUARTERLY = "ربع سنوي"
    SEMIANNUAL = "نصف سنوي"
    ANNUAL = "سنوي"


class RecurringInvoiceTemplate(Base):
    __tablename__ = 'recurring_invoice_templates'

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String(100), nullable=False)
    party_id = Column(Integer, ForeignKey('parties.id'), nullable=False)
    invoice_type = Column(String(20), nullable=False)  # sale / purchase
    frequency = Column(Enum(RecurrenceFrequency), nullable=False, default=RecurrenceFrequency.MONTHLY)
    start_date = Column(DateTime, nullable=False)
    end_date = Column(DateTime, nullable=True)
    next_run_date = Column(DateTime, nullable=False)
    last_run_date = Column(DateTime, nullable=True)
    discount_percentage = Column(Float, default=0.0)
    tax_rate = Column(Float, default=14.0)
    notes = Column(Text, nullable=True)
    is_active = Column(Boolean, default=True)
    auto_post = Column(Boolean, default=False)  # True = نفّذ تلقائياً
    runs_count = Column(Integer, default=0)
    created_by = Column(Integer, ForeignKey('users.id'), nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow)

    party = relationship("Party")
    lines = relationship("RecurringInvoiceLine", back_populates="template",
                         cascade="all, delete-orphan")


class RecurringInvoiceLine(Base):
    __tablename__ = 'recurring_invoice_lines'

    id = Column(Integer, primary_key=True, index=True)
    template_id = Column(Integer, ForeignKey('recurring_invoice_templates.id'), nullable=False)
    item_id = Column(Integer, ForeignKey('items.id'), nullable=False)
    quantity = Column(Float, nullable=False)
    price = Column(Float, nullable=False)

    template = relationship("RecurringInvoiceTemplate", back_populates="lines")
    item = relationship("Item")
# إنشاء جميع الجداول
# ==================== 24. إعدادات التطبيق (جديد) ====================
class AppSetting(Base):
    __tablename__ = 'app_settings'

    id = Column(Integer, primary_key=True, index=True)
    key = Column(String(100), unique=True, nullable=False, index=True)
    value = Column(Text, nullable=True)
    setting_type = Column(String(20), default='text')  # text, number, bool, json, image
    category = Column(String(50), default='general', index=True)
    description = Column(Text, nullable=True)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    updated_by = Column(Integer, ForeignKey('users.id'), nullable=True)
from database import engine
Base.metadata.create_all(bind=engine)