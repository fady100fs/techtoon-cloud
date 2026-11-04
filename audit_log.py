# audit_log.py
"""
سجل العمليات - تتبع جميع التغييرات في النظام
"""

import sqlite3
from datetime import datetime, timedelta  # ⭐ الإصلاح: timedelta كان ناقصاً
from pathlib import Path

# مسار قاعدة بيانات السجل
AUDIT_DB_PATH = Path(__file__).parent / "audit_log.db"


def init_audit_db():
    """تهيئة قاعدة بيانات السجل"""
    conn = sqlite3.connect(str(AUDIT_DB_PATH))
    cursor = conn.cursor()
    
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS audit_logs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            timestamp DATETIME NOT NULL,
            user_name TEXT NOT NULL,
            user_id INTEGER,
            action_type TEXT NOT NULL,
            table_name TEXT,
            record_id INTEGER,
            description TEXT NOT NULL,
            old_value TEXT,
            new_value TEXT,
            ip_address TEXT,
            success BOOLEAN DEFAULT 1
        )
    """)
    
    cursor.execute("""
        CREATE INDEX IF NOT EXISTS idx_timestamp ON audit_logs(timestamp)
    """)
    
    cursor.execute("""
        CREATE INDEX IF NOT EXISTS idx_user ON audit_logs(user_name)
    """)
    
    cursor.execute("""
        CREATE INDEX IF NOT EXISTS idx_action ON audit_logs(action_type)
    """)
    
    conn.commit()
    conn.close()


def log_action(user_name, action_type, description, user_id=None, 
               table_name=None, record_id=None, old_value=None, 
               new_value=None, ip_address=None, success=True):
    """تسجيل عملية في السجل"""
    try:
        conn = sqlite3.connect(str(AUDIT_DB_PATH))
        cursor = conn.cursor()
        
        cursor.execute("""
            INSERT INTO audit_logs 
            (timestamp, user_name, user_id, action_type, table_name, 
             record_id, description, old_value, new_value, ip_address, success)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            datetime.now(),
            user_name,
            user_id,
            action_type,
            table_name,
            record_id,
            description,
            str(old_value) if old_value else None,
            str(new_value) if new_value else None,
            ip_address,
            success
        ))
        
        conn.commit()
        conn.close()
        return True
    except Exception as e:
        print(f"خطأ في تسجيل العملية: {e}")
        return False


def get_audit_logs(start_date=None, end_date=None, user_name=None, 
                   action_type=None, limit=100):
    """الحصول على سجل العمليات"""
    try:
        conn = sqlite3.connect(str(AUDIT_DB_PATH))
        conn.row_factory = sqlite3.Row
        cursor = conn.cursor()
        
        query = "SELECT * FROM audit_logs WHERE 1=1"
        params = []
        
        if start_date:
            query += " AND timestamp >= ?"
            params.append(start_date)
        
        if end_date:
            query += " AND timestamp <= ?"
            params.append(end_date)
        
        if user_name:
            query += " AND user_name = ?"
            params.append(user_name)
        
        if action_type:
            query += " AND action_type = ?"
            params.append(action_type)
        
        query += " ORDER BY timestamp DESC LIMIT ?"
        params.append(limit)
        
        cursor.execute(query, params)
        results = [dict(row) for row in cursor.fetchall()]
        
        conn.close()
        return results
    except Exception as e:
        print(f"خطأ في جلب السجل: {e}")
        return []


def get_audit_summary(days=30):
    """ملخص العمليات"""
    try:
        conn = sqlite3.connect(str(AUDIT_DB_PATH))
        cursor = conn.cursor()
        
        since = (datetime.now() - timedelta(days=days)).isoformat()
        
        cursor.execute("""
            SELECT 
                action_type,
                COUNT(*) as count,
                COUNT(CASE WHEN success = 1 THEN 1 END) as success_count,
                COUNT(CASE WHEN success = 0 THEN 1 END) as fail_count
            FROM audit_logs
            WHERE timestamp >= ?
            GROUP BY action_type
            ORDER BY count DESC
        """, (since,))
        
        results = [dict(row) for row in cursor.fetchall()]
        
        cursor.execute("""
            SELECT user_name, COUNT(*) as count
            FROM audit_logs
            WHERE timestamp >= ?
            GROUP BY user_name
            ORDER BY count DESC
            LIMIT 10
        """, (since,))
        
        top_users = [dict(row) for row in cursor.fetchall()]
        
        conn.close()
        
        return {
            'by_action': results,
            'top_users': top_users,
            'period_days': days
        }
    except Exception as e:
        print(f"خطأ في جلب الملخص: {e}")
        return {'by_action': [], 'top_users': [], 'period_days': days}


# تهيئة قاعدة البيانات عند الاستيراد
init_audit_db()