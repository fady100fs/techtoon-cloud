"""
database.py — إعداد الاتصال بـ Neon PostgreSQL (نسخة محسّنة للأداء)
"""

import os
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, declarative_base
from sqlalchemy.pool import QueuePool, NullPool, NullPool
from dotenv import load_dotenv

# تحميل متغيرات البيئة
load_dotenv()


def _get_database_url() -> str:
    """قراءة الرابط من secrets أو من .env."""
    url = None

    # 1. محاولة st.secrets (على Streamlit Cloud)
    try:
        import streamlit as st
        url = st.secrets.get("DATABASE_URL")
    except Exception:
        pass

    # 2. الرجوع إلى متغيرات البيئة (.env محلياً)
    if not url:
        url = os.getenv("DATABASE_URL")

    if not url:
        raise RuntimeError(
            "DATABASE_URL غير موجود! أضفه في .env محلياً أو في secrets.toml على السحابة."
        )

    # توحيد الصيغة
    if url.startswith("postgres://"):
        url = url.replace("postgres://", "postgresql+psycopg://", 1)
    elif url.startswith("postgresql://") and "+psycopg" not in url:
        url = url.replace("postgresql://", "postgresql+psycopg://", 1)

    return url


# ============ إعداد Engine محسّن ============
DATABASE_URL = _get_database_url()

engine = create_engine(
    DATABASE_URL,
    poolclass=QueuePool,
    pool_size=10,
    max_overflow=20,
    pool_pre_ping=True,
    pool_recycle=280,          # ⭐ يعيد الاتصال قبل نوم Neon (300s)
    pool_timeout=30,
    connect_args={
        "connect_timeout": 10,
        "application_name": "techtoon_accounting",
    },
    echo=False,
    future=True,
)

SessionLocal = sessionmaker(
    autocommit=False,
    autoflush=False,
    bind=engine,
    expire_on_commit=False,
)


# ⭐ هذا السطر الذي كان ناقصاً — مهم جداً لـ models.py
Base = declarative_base()


def get_db():
    """مولّد جلسة قاعدة بيانات."""
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


def get_session():
    """جلسة مباشرة (للاستخدام السريع)."""
    return SessionLocal()

# ═══════════════════════════════════════════════════════
#  Helpers (auto-added) — تعمل مع Neon أو SQLite بأمان
# ═══════════════════════════════════════════════════════
import os as _os

def is_local_mode():
    """هل نحن في وضع محلي (SQLite Mirror)؟"""
    return _os.getenv("DEPLOY_MODE", "cloud").lower() == "local"


def _try_local():
    """محاولة تحميل SQLite Mirror بأمان — ترجع None عند الفشل."""
    try:
        from local_mirror import LocalSession, init_local_schema
        init_local_schema()
        return LocalSession
    except Exception:
        return None


_READ_SESSION_FACTORY = None
_READ_INIT_DONE = False


def _is_local_db_url(url) -> bool:
    """هل رابط قاعدة البيانات يشير إلى جهاز محلي (وليس سحابي)؟"""
    try:
        import re as _re
        _u = str(url or "").lower()
        if _u.startswith("sqlite"):
            return True
        if "@" not in _u:
            return False
        _host = _u.split("@", 1)[1].split("/", 1)[0]
        if ":" in _host:
            _host = _host.split(":", 1)[0]
        _host = _host.strip("[]() ")
        if not _host:
            return False
        if _host in ("localhost", "127.0.0.1", "::1", "host.docker.internal"):
            return True
        if _re.match(r"^192\.168\.\d+\.\d+$", _host):
            return True
        if _re.match(r"^10\.\d+\.\d+\.\d+$", _host):
            return True
        if _re.match(r"^172\.(1[6-9]|2[0-9]|3[01])\.\d+\.\d+$", _host):
            return True
        return False
    except Exception:
        return False


# ⭐ قاعدة البيانات نفسها محلية → القراءة المباشرة أسرع وأدق من أي مرآة
DATABASE_URL_IS_LOCAL = _is_local_db_url(DATABASE_URL)


def _mirror_has_data() -> bool:
    """فحص سريع: هل تحتوي المرآة المحلية على بيانات فعلية؟"""
    try:
        from sqlalchemy import func as _func
        import models as _models
        from local_mirror import LocalSession as _LS
        _db = _LS()
        try:
            for _m in (_models.User, _models.Account, _models.Item,
                       _models.Party, _models.Invoice):
                try:
                    _c = _db.query(_func.count(getattr(_m, "id"))).scalar()
                    if _c and int(_c) > 0:
                        return True
                except Exception:
                    continue
            return False
        finally:
            _db.close()
    except Exception:
        return False


_MIRROR_FALLBACK_NOTED = False


def get_read_session():
    """
    جلسة قراءة:
    - قاعدة بيانات محلية (localhost/شبكة محلية) → قراءة مباشرة (الأسرع والأدق)
    - سحابية + وضع محلي + مرآة تحتوي بيانات → المرآة (سريع)
    - سحابية + وضع محلي + مرآة فارغة → المصدر مباشرة (حماية من بيانات ناقصة)
    """
    global _READ_SESSION_FACTORY, _READ_INIT_DONE, _MIRROR_FALLBACK_NOTED
    if not _READ_INIT_DONE:
        _READ_INIT_DONE = True
        if is_local_mode() and not DATABASE_URL_IS_LOCAL:
            _READ_SESSION_FACTORY = _try_local()
    if _READ_SESSION_FACTORY is not None:
        # ⭐ حماية: مرآة فارغة → اقرأ من المصدر مباشرة وابدأ مزامنة خلفية
        if _mirror_has_data():
            return _READ_SESSION_FACTORY()
        if not _MIRROR_FALLBACK_NOTED:
            _MIRROR_FALLBACK_NOTED = True
            try:
                print("⚠️ المرآة المحلية فارغة — قراءة مؤقتة من المصدر مع مزامنة خلفية")
            except Exception:
                pass
            try:
                import threading as _th
                from sync_manager import full_sync as _fs
                _th.Thread(target=_fs, kwargs={"verbose": False}, daemon=True).start()
            except Exception:
                pass
        return SessionLocal()
    return SessionLocal()


def get_write_session():
    """جلسة كتابة — Neon دائماً."""
    return SessionLocal()


def get_local_db_path():
    """مسار SQLite المحلي (إن وُجد)."""
    try:
        from local_mirror import LOCAL_DB_PATH
        return LOCAL_DB_PATH
    except Exception:
        return None
