# session_auth.py
# تسجيل دخول دائم: يبقى المستخدم مسجلاً بعد Refresh (عبر Cookie + ملف جلسات)
# يتطلب Streamlit 1.37 أو أحدث (st.context.cookies)
# ✅ Rate Limiting: حماية من brute force
import importlib
import json
import secrets
import time
from datetime import date, datetime
from enum import Enum
from pathlib import Path

import streamlit as st
import streamlit.components.v1 as components

COOKIE_NAME = "techtoon_session"
SESSION_DAYS = 7
_STORE = Path(__file__).parent / ".sessions.json"

# =========================================================
# Rate Limiting — إعدادات
# =========================================================
_RATE_LIMIT_FILE = Path(__file__).parent / ".login_attempts.json"
_MAX_ATTEMPTS = 5           # عدد المحاولات المسموحة
_LOCKOUT_SECONDS = 300      # مدة الحظر (5 دقائق)
_ATTEMPT_WINDOW = 900       # نافذة العد (15 دقيقة)


# ---------------------------------------------------------
# تحويل القيم الخاصة (Enum / datetime) من وإلى JSON
# ---------------------------------------------------------
def _encode(obj):
    if isinstance(obj, Enum):
        return {"__enum__": type(obj).__name__, "value": obj.value}
    if isinstance(obj, datetime):
        return {"__datetime__": obj.isoformat()}
    if isinstance(obj, date):
        return {"__date__": obj.isoformat()}
    return str(obj)


def _decode(d):
    if "__datetime__" in d:
        return datetime.fromisoformat(d["__datetime__"])
    if "__date__" in d:
        return date.fromisoformat(d["__date__"])
    if "__enum__" in d:
        try:
            cls = getattr(importlib.import_module("models"), d["__enum__"])
            return cls(d["value"])
        except Exception:
            return d["value"]
    return d


# ---------------------------------------------------------
# تخزين الجلسات (token -> بيانات المستخدم)
# ---------------------------------------------------------
def _load():
    try:
        data = json.loads(_STORE.read_text(encoding="utf-8"), object_hook=_decode)
    except Exception:
        return {}
    now = time.time()
    return {k: v for k, v in data.items() if v.get("expires", 0) > now}


def _save(data):
    _STORE.write_text(
        json.dumps(data, ensure_ascii=False, default=_encode), encoding="utf-8"
    )


# ---------------------------------------------------------
# التعامل مع الـ Cookie
# ---------------------------------------------------------
def _set_cookie(value, max_age):
    js = f"""
    var c = "{COOKIE_NAME}={value}; max-age={max_age}; path=/; SameSite=Lax";
    try {{ window.parent.document.cookie = c; }}
    catch (e) {{ document.cookie = c; }}
    """
    components.html(f"<script>{js}</script>", height=0)


def _read_cookie():
    try:
        return st.context.cookies.get(COOKIE_NAME)
    except Exception:
        return None


# =========================================================
# Rate Limiting — التنفيذ
# =========================================================
def _load_attempts():
    """يقرأ سجل المحاولات من الملف (مع تنظيف القديم)."""
    try:
        if _RATE_LIMIT_FILE.exists():
            data = json.loads(_RATE_LIMIT_FILE.read_text(encoding="utf-8"))
            now = time.time()
            return {
                k: v for k, v in data.items()
                if isinstance(v, dict)
                and now - v.get("first_attempt", 0) < _ATTEMPT_WINDOW
            }
    except Exception:
        pass
    return {}


def _save_attempts(data):
    """يحفظ سجل المحاولات."""
    try:
        _RATE_LIMIT_FILE.write_text(
            json.dumps(data, ensure_ascii=False),
            encoding="utf-8"
        )
    except Exception:
        pass


def check_rate_limit(identifier):
    """يتحقق إذا كان المستخدم مسموحاً له بالمحاولة.

    Returns:
        tuple: (is_allowed: bool, seconds_remaining: int)
    """
    if not identifier:
        return True, 0

    data = _load_attempts()
    key = str(identifier).strip().lower()
    record = data.get(key)

    if not record:
        return True, 0

    attempts = record.get("count", 0)
    first = record.get("first_attempt", 0)
    now = time.time()

    if now - first >= _ATTEMPT_WINDOW:
        return True, 0

    if attempts >= _MAX_ATTEMPTS:
        elapsed = now - first
        remaining = int(_ATTEMPT_WINDOW - elapsed)
        return False, max(remaining, 0)

    return True, 0


def record_failed_attempt(identifier):
    """يسجل محاولة فاشلة."""
    if not identifier:
        return
    data = _load_attempts()
    key = str(identifier).strip().lower()
    now = time.time()

    if key in data:
        record = data[key]
        if now - record.get("first_attempt", 0) >= _ATTEMPT_WINDOW:
            record = {"count": 1, "first_attempt": now, "last_attempt": now}
        else:
            record["count"] = record.get("count", 0) + 1
            record["last_attempt"] = now
    else:
        record = {"count": 1, "first_attempt": now, "last_attempt": now}

    data[key] = record
    _save_attempts(data)


def clear_attempts(identifier):
    """يحذف سجل المحاولات بعد نجاح الدخول."""
    if not identifier:
        return
    data = _load_attempts()
    key = str(identifier).strip().lower()
    data.pop(key, None)
    _save_attempts(data)


def get_remaining_attempts(identifier):
    """يرجع عدد المحاولات المتبقية قبل الحظر."""
    if not identifier:
        return _MAX_ATTEMPTS
    data = _load_attempts()
    key = str(identifier).strip().lower()
    record = data.get(key)
    if not record:
        return _MAX_ATTEMPTS
    return max(0, _MAX_ATTEMPTS - record.get("count", 0))


# ---------------------------------------------------------
# الدوال العامة للجلسة
# ---------------------------------------------------------
def login_user(user):
    """استدعِها بعد نجاح التحقق من كلمة المرور."""
    token = secrets.token_urlsafe(32)
    data = _load()
    data[token] = {
        "user": user,
        "expires": time.time() + SESSION_DAYS * 86400,
    }
    _save(data)

    st.session_state.current_user = user
    st.session_state["_auth_token"] = token

    _set_cookie(token, SESSION_DAYS * 86400)
    time.sleep(1)


def restore_session():
    """تعيد المستخدم من الـ Cookie بعد Refresh."""
    if "current_user" in st.session_state:
        return True

    token = _read_cookie()
    if not token:
        return False

    entry = _load().get(token)
    if not entry:
        return False

    st.session_state.current_user = entry["user"]
    st.session_state["_auth_token"] = token
    return True


def logout_user():
    token = st.session_state.get("_auth_token") or _read_cookie()
    if token:
        data = _load()
        data.pop(token, None)
        _save(data)

    st.session_state.pop("current_user", None)
    st.session_state.pop("_auth_token", None)

    _set_cookie("", 0)
    time.sleep(1)