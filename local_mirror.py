# local_mirror.py
# -*- coding: utf-8 -*-
"""
نسخة SQLite محلية (Mirror) لقاعدة بيانات Neon.
الهدف: تسريع القراءات محلياً من ~1-3 ثانية إلى ~50ms.
Neon يبقى المصدر الرسمي للبيانات.
"""
import os
import logging
from sqlalchemy import create_engine, event
from sqlalchemy.orm import sessionmaker, Session

logger = logging.getLogger(__name__)

# 📂 مكان قاعدة البيانات المحلية
DATA_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "data")
os.makedirs(DATA_DIR, exist_ok=True)

LOCAL_DB_PATH = os.path.join(DATA_DIR, "local_mirror.db")

# ⚙️ إنشاء Engine محلي سريع
local_engine = create_engine(
    f"sqlite:///{LOCAL_DB_PATH}",
    connect_args={
        "check_same_thread": False,   # للسماح بالاستخدام من Threads
        "timeout": 30,
    },
    pool_pre_ping=True,
    echo=False,
)

# ⚡ تسريع SQLite (WAL + ذاكرة أسرع)
@event.listens_for(local_engine, "connect")
def _set_sqlite_pragma(dbapi_conn, connection_record):
    cursor = dbapi_conn.cursor()
    cursor.execute("PRAGMA journal_mode=WAL")       # ⭐ كتابة أسرع
    cursor.execute("PRAGMA synchronous=NORMAL")     # ⭐ توازن الأمان/السرعة
    cursor.execute("PRAGMA cache_size=-64000")      # 64MB cache
    cursor.execute("PRAGMA temp_store=MEMORY")      # temp في الرام
    cursor.execute("PRAGMA foreign_keys=OFF")       # ⭐ إيقاف FK أثناء المزامنة
    cursor.close()

LocalSession = sessionmaker(bind=local_engine, expire_on_commit=False)


def init_local_schema():
    """إنشاء كل الجداول محلياً عند أول تشغيل."""
    import models
    models.Base.metadata.create_all(local_engine)
    logger.info("✅ تم إنشاء schema SQLite محلياً")


def get_local_session() -> Session:
    return LocalSession()


def get_local_db_size_mb() -> float:
    """حجم قاعدة البيانات المحلية بالميجابايت."""
    if os.path.exists(LOCAL_DB_PATH):
        return os.path.getsize(LOCAL_DB_PATH) / (1024 * 1024)
    return 0.0


def reset_local_db():
    """حذف وإعادة إنشاء SQLite — للطوارئ فقط."""
    try:
        if os.path.exists(LOCAL_DB_PATH):
            os.remove(LOCAL_DB_PATH)
        # احذف ملفات WAL
        for suffix in ("-wal", "-shm"):
            p = LOCAL_DB_PATH + suffix
            if os.path.exists(p):
                os.remove(p)
        init_local_schema()
        logger.info("🔄 تم إعادة إنشاء SQLite المحلي")
    except Exception as e:
        logger.error(f"فشل إعادة إنشاء SQLite: {e}")