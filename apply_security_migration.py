# -*- coding: utf-8 -*-
import psycopg2
CS = "host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname=neondb user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"
conn = psycopg2.connect(CS); conn.autocommit = True; cur = conn.cursor()
print("Applying security migration...")

sqls = [
    # 1) جدول الصلاحيات
    """CREATE TABLE IF NOT EXISTS user_permissions (
        id SERIAL PRIMARY KEY,
        user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
        permission_key VARCHAR NOT NULL,
        is_granted BOOLEAN DEFAULT TRUE,
        created_at TIMESTAMP DEFAULT NOW(),
        UNIQUE(user_id, permission_key)
    )""",
    
    # 2) جدول سجل التدقيق
    """CREATE TABLE IF NOT EXISTS audit_log (
        id SERIAL PRIMARY KEY,
        user_id INTEGER,
        username VARCHAR,
        action VARCHAR NOT NULL,
        entity_type VARCHAR,
        entity_id INTEGER,
        details TEXT,
        ip_address VARCHAR,
        created_at TIMESTAMP DEFAULT NOW()
    )""",
    
    # 3) إضافة password_hash لو مش موجود (آمن)
    "ALTER TABLE users ADD COLUMN IF NOT EXISTS password_hash VARCHAR",
    
    # 4) فهارس
    "CREATE INDEX IF NOT EXISTS idx_audit_log_user ON audit_log(user_id)",
    "CREATE INDEX IF NOT EXISTS idx_audit_log_action ON audit_log(action)",
    "CREATE INDEX IF NOT EXISTS idx_audit_log_date ON audit_log(created_at)",
    "CREATE INDEX IF NOT EXISTS idx_user_permissions_user ON user_permissions(user_id)",
    
    # 5) صلاحيات افتراضية للأدمن
    """INSERT INTO user_permissions (user_id, permission_key, is_granted)
        SELECT u.id, p.key, TRUE
        FROM users u
        CROSS JOIN (VALUES 
            ('sales'), ('edit_price'), ('discounts'), ('cancel_invoice'),
            ('returns'), ('expenses'), ('profit_reports'), ('stock_adjustment'),
            ('manage_users'), ('settings'), ('backup')
        ) AS p(key)
        WHERE u.role = 'ADMIN'
        ON CONFLICT (user_id, permission_key) DO NOTHING""",
    
    # 6) صلاحيات افتراضية للكاشير (بيع فقط)
    """INSERT INTO user_permissions (user_id, permission_key, is_granted)
        SELECT u.id, p.key, TRUE
        FROM users u
        CROSS JOIN (VALUES 
            ('sales'), ('returns')
        ) AS p(key)
        WHERE u.role = 'SALESPERSON'
        ON CONFLICT (user_id, permission_key) DO NOTHING""",
]

for s in sqls:
    try: cur.execute(s); print(f"  OK: {s[:70]}...")
    except Exception as e: print(f"  SKIP: {str(e)[:70]}")

cur.close(); conn.close()
print("Migration complete!")
