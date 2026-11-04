# update_database.py
"""
سكريبت تحديث قاعدة البيانات
يضيف جميع الأعمدة والجداول المطلوبة للبرنامج
"""

import sqlite3
from pathlib import Path

DB_PATH = Path(__file__).parent / "accounting.db"


def add_missing_columns():
    """إضافة الأعمدة المفقودة للجداول الموجودة"""
    if not DB_PATH.exists():
        print("❌ قاعدة البيانات غير موجودة!")
        return
    
    conn = sqlite3.connect(str(DB_PATH))
    cursor = conn.cursor()
    
    try:
        columns_to_add = [
            ("items", "category_id", "INTEGER"),
            ("items", "avg_cost_price", "REAL DEFAULT 0.0"),
            ("items", "total_purchased_qty", "REAL DEFAULT 0.0"),
            ("invoice_lines", "cost_price", "REAL DEFAULT 0.0"),
            ("invoices", "due_date", "DATETIME"),
            ("invoices", "discount_amount", "REAL DEFAULT 0.0"),
            ("invoices", "discount_percentage", "REAL DEFAULT 0.0"),
            ("invoices", "tax_rate", "REAL DEFAULT 14.0"),
            ("invoices", "tax_amount", "REAL DEFAULT 0.0"),
            ("invoices", "net_amount", "REAL DEFAULT 0.0"),
            ("invoices", "created_by", "INTEGER"),
            ("payments", "cash_box_id", "INTEGER DEFAULT 1"),
            ("payments", "created_by", "INTEGER"),
            ("journal_entries", "created_by", "INTEGER"),
            ("journal_entries", "period_id", "INTEGER"),
            ("parties", "credit_limit", "REAL DEFAULT 0.0")
        ]
        
        for table, col_name, col_type in columns_to_add:
            cursor.execute(f"PRAGMA table_info({table})")
            existing_columns = [row[1] for row in cursor.fetchall()]
            
            if col_name not in existing_columns:
                sql = f"ALTER TABLE {table} ADD COLUMN {col_name} {col_type}"
                cursor.execute(sql)
                print(f"  ✅ تمت إضافة عمود: {table}.{col_name}")
            else:
                print(f"  ️  عمود {table}.{col_name} موجود مسبقاً")
        
        # إنشاء الجداول الأساسية
        tables = {
            'categories': """
                CREATE TABLE IF NOT EXISTS categories (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name VARCHAR(100) NOT NULL UNIQUE,
                    description TEXT,
                    parent_id INTEGER,
                    is_active BOOLEAN DEFAULT 1,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (parent_id) REFERENCES categories(id)
                )
            """,
            'cash_boxes': """
                CREATE TABLE IF NOT EXISTS cash_boxes (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name VARCHAR(100) NOT NULL,
                    code VARCHAR(20) NOT NULL UNIQUE,
                    type VARCHAR(20) NOT NULL DEFAULT 'نقدية',
                    account_id INTEGER NOT NULL,
                    responsible_person VARCHAR(100),
                    max_limit REAL,
                    is_active BOOLEAN DEFAULT 1,
                    notes TEXT,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (account_id) REFERENCES accounts(id)
                )
            """,
            'cash_transfers': """
                CREATE TABLE IF NOT EXISTS cash_transfers (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    date DATETIME DEFAULT CURRENT_TIMESTAMP,
                    from_cash_box_id INTEGER NOT NULL,
                    to_cash_box_id INTEGER NOT NULL,
                    amount REAL NOT NULL,
                    notes TEXT,
                    created_by INTEGER,
                    FOREIGN KEY (from_cash_box_id) REFERENCES cash_boxes(id),
                    FOREIGN KEY (to_cash_box_id) REFERENCES cash_boxes(id)
                )
            """,
            'cost_history': """
                CREATE TABLE IF NOT EXISTS cost_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    item_id INTEGER NOT NULL,
                    invoice_id INTEGER NOT NULL,
                    purchase_price REAL NOT NULL,
                    quantity REAL NOT NULL,
                    total_cost REAL NOT NULL,
                    purchase_date DATETIME NOT NULL,
                    supplier_id INTEGER,
                    FOREIGN KEY (item_id) REFERENCES items(id),
                    FOREIGN KEY (invoice_id) REFERENCES invoices(id),
                    FOREIGN KEY (supplier_id) REFERENCES parties(id)
                )
            """,
            'users': """
                CREATE TABLE IF NOT EXISTS users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    username VARCHAR(50) NOT NULL UNIQUE,
                    password_hash VARCHAR(255) NOT NULL,
                    full_name VARCHAR(100) NOT NULL,
                    email VARCHAR(100),
                    role VARCHAR(20) NOT NULL DEFAULT 'مشاهد',
                    is_active BOOLEAN DEFAULT 1,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    last_login DATETIME
                )
            """
        }
        
        for table_name, create_sql in tables.items():
            cursor.execute(create_sql)
            print(f"✅ جدول {table_name}")
        
        conn.commit()
        print("\n✅ تم تحديث الأعمدة والجداول الأساسية بنجاح!")
        
    except Exception as e:
        conn.rollback()
        print(f"\n❌ خطأ: {e}")
    finally:
        conn.close()


def add_expenses_tables():
    """إضافة جداول المصروفات"""
    if not DB_PATH.exists():
        print("❌ قاعدة البيانات غير موجودة!")
        return
    
    conn = sqlite3.connect(str(DB_PATH))
    cursor = conn.cursor()
    
    try:
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS expense_categories (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name VARCHAR(100) NOT NULL UNIQUE,
                description TEXT,
                account_id INTEGER NOT NULL,
                is_active BOOLEAN DEFAULT 1,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (account_id) REFERENCES accounts(id)
            )
        """)
        print("✅ جدول expense_categories")
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS expenses (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                date DATETIME DEFAULT CURRENT_TIMESTAMP,
                category_id INTEGER NOT NULL,
                amount REAL NOT NULL,
                description TEXT NOT NULL,
                payment_method VARCHAR(50) NOT NULL,
                cash_box_id INTEGER NOT NULL,
                reference_number VARCHAR(100),
                receipt_image VARCHAR(255),
                created_by INTEGER,
                notes TEXT,
                FOREIGN KEY (category_id) REFERENCES expense_categories(id),
                FOREIGN KEY (cash_box_id) REFERENCES cash_boxes(id)
            )
        """)
        print("✅ جدول expenses")
        
        conn.commit()
        print("\n✅ تم إضافة جداول المصروفات بنجاح!")
        
    except Exception as e:
        conn.rollback()
        print(f"\n❌ خطأ: {e}")
    finally:
        conn.close()


def add_warehouse_tables():
    """إضافة جداول المخازن"""
    if not DB_PATH.exists():
        print("❌ قاعدة البيانات غير موجودة!")
        return
    
    conn = sqlite3.connect(str(DB_PATH))
    cursor = conn.cursor()
    
    try:
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS warehouses (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name VARCHAR(100) NOT NULL UNIQUE,
                code VARCHAR(20) NOT NULL UNIQUE,
                location VARCHAR(200),
                responsible_person VARCHAR(100),
                is_active BOOLEAN DEFAULT 1,
                notes TEXT,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP
            )
        """)
        print("✅ جدول warehouses")
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS stock_levels (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                warehouse_id INTEGER NOT NULL,
                item_id INTEGER NOT NULL,
                quantity REAL DEFAULT 0.0,
                last_updated DATETIME DEFAULT CURRENT_TIMESTAMP,
                UNIQUE(warehouse_id, item_id),
                FOREIGN KEY (warehouse_id) REFERENCES warehouses(id),
                FOREIGN KEY (item_id) REFERENCES items(id)
            )
        """)
        print("✅ جدول stock_levels")
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS warehouse_transfers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                date DATETIME DEFAULT CURRENT_TIMESTAMP,
                from_warehouse_id INTEGER NOT NULL,
                to_warehouse_id INTEGER NOT NULL,
                item_id INTEGER NOT NULL,
                quantity REAL NOT NULL,
                notes TEXT,
                created_by INTEGER,
                status VARCHAR(20) DEFAULT 'completed',
                FOREIGN KEY (from_warehouse_id) REFERENCES warehouses(id),
                FOREIGN KEY (to_warehouse_id) REFERENCES warehouses(id),
                FOREIGN KEY (item_id) REFERENCES items(id)
            )
        """)
        print("✅ جدول warehouse_transfers")
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS stock_counts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                date DATETIME DEFAULT CURRENT_TIMESTAMP,
                warehouse_id INTEGER NOT NULL,
                item_id INTEGER NOT NULL,
                system_quantity REAL NOT NULL,
                counted_quantity REAL NOT NULL,
                difference REAL NOT NULL,
                notes TEXT,
                counted_by INTEGER,
                FOREIGN KEY (warehouse_id) REFERENCES warehouses(id),
                FOREIGN KEY (item_id) REFERENCES items(id)
            )
        """)
        print("✅ جدول stock_counts")
        
        conn.commit()
        print("\n✅ تم إضافة جداول المخازن بنجاح!")
        
    except Exception as e:
        conn.rollback()
        print(f"\n❌ خطأ: {e}")
    finally:
        conn.close()


def add_fixed_asset_tables():
    """إضافة جداول الأصول الثابتة"""
    if not DB_PATH.exists():
        print("❌ قاعدة البيانات غير موجودة!")
        return
    
    conn = sqlite3.connect(str(DB_PATH))
    cursor = conn.cursor()
    
    try:
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS fixed_assets (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name VARCHAR(100) NOT NULL,
                code VARCHAR(20) NOT NULL UNIQUE,
                category VARCHAR(50) NOT NULL,
                purchase_date DATETIME NOT NULL,
                purchase_cost REAL NOT NULL,
                salvage_value REAL DEFAULT 0.0,
                useful_life_years INTEGER NOT NULL,
                depreciation_method VARCHAR(20) NOT NULL DEFAULT 'خطي',
                accumulated_depreciation REAL DEFAULT 0.0,
                net_book_value REAL NOT NULL,
                location VARCHAR(200),
                responsible_person VARCHAR(100),
                status VARCHAR(20) DEFAULT 'active',
                notes TEXT,
                created_by INTEGER,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (created_by) REFERENCES users(id)
            )
        """)
        print("✅ جدول fixed_assets")
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS depreciation_records (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                asset_id INTEGER NOT NULL,
                date DATETIME NOT NULL,
                depreciation_amount REAL NOT NULL,
                accumulated_depreciation REAL NOT NULL,
                net_book_value REAL NOT NULL,
                notes TEXT,
                created_by INTEGER,
                FOREIGN KEY (asset_id) REFERENCES fixed_assets(id),
                FOREIGN KEY (created_by) REFERENCES users(id)
            )
        """)
        print("✅ جدول depreciation_records")
        
        conn.commit()
        print("\n✅ تم إضافة جداول الأصول الثابتة بنجاح!")
        
    except Exception as e:
        conn.rollback()
        print(f"\n❌ خطأ: {e}")
    finally:
        conn.close()


def add_loan_tables():
    """إضافة جداول القروض"""
    if not DB_PATH.exists():
        print("❌ قاعدة البيانات غير موجودة!")
        return
    
    conn = sqlite3.connect(str(DB_PATH))
    cursor = conn.cursor()
    
    try:
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS loans (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                loan_number VARCHAR(50) NOT NULL UNIQUE,
                loan_type VARCHAR(20) NOT NULL,
                borrower_name VARCHAR(100) NOT NULL,
                borrower_type VARCHAR(50),
                principal_amount REAL NOT NULL,
                interest_rate REAL DEFAULT 0.0,
                loan_date DATETIME NOT NULL,
                maturity_date DATETIME NOT NULL,
                term_months INTEGER NOT NULL,
                installment_amount REAL NOT NULL,
                total_interest REAL DEFAULT 0.0,
                total_amount REAL NOT NULL,
                paid_amount REAL DEFAULT 0.0,
                remaining_amount REAL NOT NULL,
                status VARCHAR(20) DEFAULT 'نشط',
                cash_box_id INTEGER,
                notes TEXT,
                created_by INTEGER,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (cash_box_id) REFERENCES cash_boxes(id),
                FOREIGN KEY (created_by) REFERENCES users(id)
            )
        """)
        print("✅ جدول loans")
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS loan_installments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                loan_id INTEGER NOT NULL,
                installment_number INTEGER NOT NULL,
                due_date DATETIME NOT NULL,
                principal_amount REAL NOT NULL,
                interest_amount REAL DEFAULT 0.0,
                total_amount REAL NOT NULL,
                paid_amount REAL DEFAULT 0.0,
                paid_date DATETIME,
                status VARCHAR(20) DEFAULT 'pending',
                notes TEXT,
                FOREIGN KEY (loan_id) REFERENCES loans(id)
            )
        """)
        print("✅ جدول loan_installments")
        
        conn.commit()
        print("\n✅ تم إضافة جداول القروض بنجاح!")
        
    except Exception as e:
        conn.rollback()
        print(f"\n❌ خطأ: {e}")
    finally:
        conn.close()


def add_employee_tables():
    """إضافة جداول الموظفين والرواتب"""
    if not DB_PATH.exists():
        print("❌ قاعدة البيانات غير موجودة!")
        return
    
    conn = sqlite3.connect(str(DB_PATH))
    cursor = conn.cursor()
    
    try:
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS employees (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                employee_code VARCHAR(20) NOT NULL UNIQUE,
                full_name VARCHAR(100) NOT NULL,
                national_id VARCHAR(20),
                phone VARCHAR(20),
                email VARCHAR(100),
                address TEXT,
                job_title VARCHAR(100) NOT NULL,
                department VARCHAR(100),
                hire_date DATETIME NOT NULL,
                basic_salary REAL NOT NULL,
                allowances REAL DEFAULT 0.0,
                deductions REAL DEFAULT 0.0,
                social_insurance REAL DEFAULT 0.0,
                tax_rate REAL DEFAULT 0.0,
                bank_account VARCHAR(50),
                bank_name VARCHAR(100),
                status VARCHAR(20) DEFAULT 'يعمل',
                notes TEXT,
                created_by INTEGER,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (created_by) REFERENCES users(id)
            )
        """)
        print("✅ جدول employees")
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS salary_records (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                employee_id INTEGER NOT NULL,
                month INTEGER NOT NULL,
                year INTEGER NOT NULL,
                basic_salary REAL NOT NULL,
                allowances REAL DEFAULT 0.0,
                overtime REAL DEFAULT 0.0,
                bonus REAL DEFAULT 0.0,
                gross_salary REAL NOT NULL,
                social_insurance REAL DEFAULT 0.0,
                tax REAL DEFAULT 0.0,
                other_deductions REAL DEFAULT 0.0,
                total_deductions REAL DEFAULT 0.0,
                net_salary REAL NOT NULL,
                payment_date DATETIME,
                payment_method VARCHAR(50),
                status VARCHAR(20) DEFAULT 'pending',
                notes TEXT,
                created_by INTEGER,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (employee_id) REFERENCES employees(id),
                FOREIGN KEY (created_by) REFERENCES users(id)
            )
        """)
        print("✅ جدول salary_records")
        
        conn.commit()
        print("\n✅ تم إضافة جداول الموظفين والرواتب بنجاح!")
        
    except Exception as e:
        conn.rollback()
        print(f"\n❌ خطأ: {e}")
    finally:
        conn.close()


def add_budget_tables():
    """إضافة جدول الموازنات"""
    if not DB_PATH.exists():
        print("❌ قاعدة البيانات غير موجودة!")
        return
    
    conn = sqlite3.connect(str(DB_PATH))
    cursor = conn.cursor()
    
    try:
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS budgets (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                account_id INTEGER NOT NULL,
                month INTEGER NOT NULL,
                year INTEGER NOT NULL,
                budgeted_amount REAL NOT NULL,
                notes TEXT,
                created_by INTEGER,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                UNIQUE(account_id, month, year),
                FOREIGN KEY (account_id) REFERENCES accounts(id),
                FOREIGN KEY (created_by) REFERENCES users(id)
            )
        """)
        print("✅ جدول budgets")
        
        conn.commit()
        print("\n✅ تم إضافة جدول الموازنات بنجاح!")
        
    except Exception as e:
        conn.rollback()
        print(f"\n❌ خطأ: {e}")
    finally:
        conn.close()


def add_accounting_period_tables():
    """إضافة جدول الفترات المحاسبية"""
    if not DB_PATH.exists():
        print("❌ قاعدة البيانات غير موجودة!")
        return
    
    conn = sqlite3.connect(str(DB_PATH))
    cursor = conn.cursor()
    
    try:
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS accounting_periods (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                period_name VARCHAR(50) NOT NULL,
                start_date DATETIME NOT NULL,
                end_date DATETIME NOT NULL,
                status VARCHAR(20) DEFAULT 'مفتوح',
                is_fiscal_year BOOLEAN DEFAULT 0,
                closing_date DATETIME,
                closing_notes TEXT,
                created_by INTEGER,
                closed_by INTEGER,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (created_by) REFERENCES users(id),
                FOREIGN KEY (closed_by) REFERENCES users(id)
            )
        """)
        print("✅ جدول accounting_periods")
        
        conn.commit()
        print("\n✅ تم إضافة جدول الفترات المحاسبية بنجاح!")
        
    except Exception as e:
        conn.rollback()
        print(f"\n❌ خطأ: {e}")
    finally:
        conn.close()


def add_cost_center_tables():
    """إضافة جداول مراكز التكلفة"""
    if not DB_PATH.exists():
        print("❌ قاعدة البيانات غير موجودة!")
        return
    
    conn = sqlite3.connect(str(DB_PATH))
    cursor = conn.cursor()
    
    try:
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS cost_centers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                code VARCHAR(20) NOT NULL UNIQUE,
                name VARCHAR(100) NOT NULL,
                description TEXT,
                manager_name VARCHAR(100),
                parent_id INTEGER,
                is_active BOOLEAN DEFAULT 1,
                budget_limit REAL,
                notes TEXT,
                created_by INTEGER,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (parent_id) REFERENCES cost_centers(id),
                FOREIGN KEY (created_by) REFERENCES users(id)
            )
        """)
        print("✅ جدول cost_centers")
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS cost_allocations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                cost_center_id INTEGER NOT NULL,
                account_id INTEGER NOT NULL,
                allocation_percentage REAL NOT NULL,
                month INTEGER NOT NULL,
                year INTEGER NOT NULL,
                notes TEXT,
                created_by INTEGER,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (cost_center_id) REFERENCES cost_centers(id),
                FOREIGN KEY (account_id) REFERENCES accounts(id),
                FOREIGN KEY (created_by) REFERENCES users(id)
            )
        """)
        print("✅ جدول cost_allocations")
        
        cursor.execute("""
            CREATE TABLE IF NOT EXISTS cost_center_transactions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                cost_center_id INTEGER NOT NULL,
                date DATETIME NOT NULL,
                description VARCHAR(255) NOT NULL,
                amount REAL NOT NULL,
                transaction_type VARCHAR(20) NOT NULL,
                reference_type VARCHAR(50),
                reference_id INTEGER,
                created_by INTEGER,
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (cost_center_id) REFERENCES cost_centers(id),
                FOREIGN KEY (created_by) REFERENCES users(id)
            )
        """)
        print("✅ جدول cost_center_transactions")
        
        conn.commit()
        print("\n✅ تم إضافة جداول مراكز التكلفة بنجاح!")
        
    except Exception as e:
        conn.rollback()
        print(f"\n❌ خطأ: {e}")
    finally:
        conn.close()


def create_default_data():
    """إنشاء بيانات افتراضية"""
    if not DB_PATH.exists():
        print("❌ قاعدة البيانات غير موجودة!")
        return
    
    conn = sqlite3.connect(str(DB_PATH))
    cursor = conn.cursor()
    
    try:
        # التحقق من وجود حسابات افتراضية
        cursor.execute("SELECT COUNT(*) FROM accounts")
        if cursor.fetchone()[0] == 0:
            print(" إنشاء الحسابات الافتراضية...")
            
            accounts = [
                ("1000", "الأصول", "أصول", None),
                ("1100", "الأصول المتداولة", "أصول", 1),
                ("1101", "النقدية", "أصول", 2),
                ("1201", "العملاء", "أصول", 2),
                ("1301", "المخزون", "أصول", 2),
                ("2000", "الخصوم", "خصوم", None),
                ("2101", "الموردون", "خصوم", 6),
                ("3000", "حقوق الملكية", "حقوق ملكية", None),
                ("3101", "رأس المال", "حقوق ملكية", 8),
                ("4000", "الإيرادات", "إيرادات", None),
                ("4101", "المبيعات", "إيرادات", 10),
                ("5000", "المصروفات", "مصروفات", None),
                ("5101", "تكلفة المبيعات", "مصروفات", 12),
                ("5201", "مصروفات الإهلاك", "مصروفات", 12),
                ("5301", "مصروفات الفوائد", "مصروفات", 12),
                ("5401", "مصروفات الرواتب", "مصروفات", 12)
            ]
            
            for code, name, type_, parent_id in accounts:
                cursor.execute(
                    "INSERT INTO accounts (code, name, type, parent_id) VALUES (?, ?, ?, ?)",
                    (code, name, type_, parent_id)
                )
            
            print("✅ تم إنشاء الحسابات الافتراضية")
        
        # التحقق من وجود عملة افتراضية
        cursor.execute("SELECT COUNT(*) FROM currencies")
        if cursor.fetchone()[0] == 0:
            print("💱 إنشاء العملة الافتراضية...")
            cursor.execute(
                "INSERT INTO currencies (code, name, symbol, exchange_rate, is_default) VALUES (?, ?, ?, ?, ?)",
                ("EGP", "جنيه مصري", "ج.م", 1.0, True)
            )
            print("✅ تم إنشاء العملة الافتراضية")
        
        conn.commit()
        print("\n✅ تم إنشاء البيانات الافتراضية بنجاح!")
        
    except Exception as e:
        conn.rollback()
        print(f"\n❌ خطأ: {e}")
    finally:
        conn.close()


if __name__ == "__main__":
    print("=" * 70)
    print("🔄 بدء تحديث قاعدة البيانات...")
    print("=" * 70)
    print()
    
    print("📌 المرحلة 1: إضافة الأعمدة والجداول الأساسية")
    print("-" * 70)
    add_missing_columns()
    print()
    
    print(" المرحلة 2: إضافة جداول المصروفات")
    print("-" * 70)
    add_expenses_tables()
    print()
    
    print("📌 المرحلة 3: إضافة جداول المخازن")
    print("-" * 70)
    add_warehouse_tables()
    print()
    
    print("📌 المرحلة 4: إضافة جداول الأصول الثابتة")
    print("-" * 70)
    add_fixed_asset_tables()
    print()
    
    print("📌 المرحلة 5: إضافة جداول القروض")
    print("-" * 70)
    add_loan_tables()
    print()
    
    print("📌 المرحلة 6: إضافة جداول الموظفين والرواتب")
    print("-" * 70)
    add_employee_tables()
    print()
    
    print("📌 المرحلة 7: إضافة جدول الموازنات")
    print("-" * 70)
    add_budget_tables()
    print()
    
    print("📌 المرحلة 8: إضافة جدول الفترات المحاسبية")
    print("-" * 70)
    add_accounting_period_tables()
    print()
    
    print("📌 المرحلة 9: إضافة جداول مراكز التكلفة")
    print("-" * 70)
    add_cost_center_tables()
    print()
    
    print(" المرحلة 10: إنشاء البيانات الافتراضية")
    print("-" * 70)
    create_default_data()
    print()
    
    print("=" * 70)
    print("✅ اكتمل تحديث قاعدة البيانات بنجاح!")
    print("=" * 70)
    print()
    print("📋 ملخص ما تم إنجازه:")
    print("  ✅ إضافة 16 عمود جديد للجداول الموجودة")
    print("  ✅ إنشاء 9 جداول جديدة")
    print("  ✅ إنشاء الحسابات الافتراضية")
    print("  ✅ إنشاء العملة الافتراضية")
    print()
    print("🚀 يمكنك الآن تشغيل البرنامج:")
    print("   streamlit run app.py")
    print("=" * 70)