# -*- coding: utf-8 -*-
"""
Techtoon Accounting - Streamlit App (complete)
Includes: login, dashboard, sidebar grouped by Expander, all pages routing,
cash boxes page, dynamic balances, expression-evaluation helper.
"""
import os
import sys
import streamlit as st

# Ensure local imports work regardless of CWD
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))


# ------------------------------------------------------------------------------
# Database connection (reuse project database.py if present, else env vars)
# ------------------------------------------------------------------------------
try:
    from database import get_connection  # type: ignore
except Exception:
    import psycopg2

    def get_connection():
        return psycopg2.connect(
            host=os.environ.get("PGHOST"),
            dbname=os.environ.get("PGDATABASE"),
            user=os.environ.get("PGUSER"),
            password=os.environ.get("PGPASSWORD"),
            sslmode=os.environ.get("PGSSLMODE", "require"),
            port=os.environ.get("PGPORT", "5432"),
        )


# ------------------------------------------------------------------------------
# Page config + RTL styling
# ---------------------------------------------------------------------------------
st.set_page_config(
    page_title="Techtoon Accounting",
    page_icon="📊",
    layout="wide",
    initial_sidebar_state="expanded",
)

st.markdown(
    """
    <style>
    section.main, div.block-container { direction: rtl; text-align: right; }
    .stSidebar, .stSidebar * { direction: rtl; }
    table, th, td { text-align: right; }
    .stMetric, .stMetric-value { text-align: right; }
    [data-testid="stExpander"] details summary { direction: rtl; }
    </style>
    """,
    unsafe_allow_html=True,
)


# ------------------------------------------------------------------------------
# Auth
# ------------------------------------------------------------------------------
def _verify_password(plain, hashed):
    """تحقّق صارم: bcrypt فقط — لا مقارنة نصية أبدًا (وإلا لا يتحقق من كلمة المرور)."""
    if not plain or not hashed:
        return False
    try:
        import bcrypt
        if isinstance(hashed, str):
            hashed = hashed.encode()
        if isinstance(plain, str):
            plain = plain.encode()
        return bcrypt.checkpw(plain, hashed)
    except Exception:
        return False


def login_user(username, password):
    try:
        # get_connection() هو context manager — لا يمكن استدعاء cursor() عليه مباشرة
        with get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    "SELECT id, username, COALESCE(full_name, username), "
                    "COALESCE(password_hash, ''), COALESCE(is_active, true) "
                    "FROM users WHERE username = %s",
                    (username,),
                )
                row = cur.fetchone()
    except Exception:
        return {"error": "تعذر الاتصال بقاعدة البيانات"}
    if not row:
        return None
    uid, uname, fname, phash, active = row
    if not active:
        return {"error": "الحساب غير نشط، راجع المدير"}
    if not phash:
        # لا يوجد hash صالح — لا يمكن الدخول (لا نسمح بدخول بكلمة مرور فارغة)
        return {"error": "لا توجد كلمة مرور لهذا الحساب — راجع المدير"}
    if _verify_password(password, phash):
        return {"id": uid, "username": uname, "full_name": fname}
    return None


def show_login():
    st.title("🔐 تسجيل الدخول")
    st.caption("Techtoon Accounting")
    with st.form("login_form"):
        u = st.text_input("اسم المستخدم")
        p = st.text_input("كلمة المرور", type="password")
        submitted = st.form_submit_button("دخول", use_container_width=True)
    if submitted:
        res = login_user(u, p)
        if res is None:
            st.error("بيانات الدخول غير صحيحة")
        elif "error" in res:
            st.error(res["error"])
        else:
            st.session_state.user = res
            st.session_state.logged_in = True
            st.session_state.current_page = "dashboard"
            st.rerun()


def logout():
    st.session_state.clear()
    st.rerun()


# ------------------------------------------------------------------------------
# Navigation (grouped by Expander - mirrors desktop sidebar)
# ------------------------------------------------------------------------------
PAGES = {
    "🏠 الرئيسية": {
        "dashboard": "📊 لوحة المعلومات",
    },
    "🧾 المبيعات": {
        "invoices": "🧾 الفواتير",
    },
    "👥 الأطراف": {
        "parties": "👤 العملاء والموردين",
    },
    "📦 المخزون": {
        "items": "📦 الأصناف",
        "stock_levels": "📋 أرصدة المخزون",
        "stock_counts": "🔢 الجرد",
        "stock_adjustments": "⚖️ تسويات المخزون",
        "damaged_items": "💔 الأصناف التالفة",
        "writeoff_requests": "🗑️ طلبات الإلغاء",
    },
    "💰 المالية": {
        "payments": "💵 المدفوعات",
        "cash_boxes": "🏦 الخزائن",
        "loans": "💳 القروض",
    },
    "📊 التقارير": {
        "reports": "📑 التقارير",
    },
}


def show_sidebar():
    with st.sidebar:
        user = st.session_state.get("user", {})
        st.markdown(f"### 👋 {user.get('full_name', 'مستخدم')}")
        if st.button("🚪 تسجيل الخروج", use_container_width=True, key="btn_logout"):
            logout()
        st.divider()
        st.markdown("#### 📂 الصفحات")
        current = st.session_state.get("current_page", "dashboard")
        for group, items in PAGES.items():
            expanded = current in items
            with st.expander(group, expanded=expanded):
                for key, label in items.items():
                    btn_type = "primary" if key == current else "secondary"
                    if st.button(label, use_container_width=True, key=f"nav_{key}",
                                    type=btn_type):
                        st.session_state.current_page = key
                        st.rerun()


# ------------------------------------------------------------------------------
# Routing (defensive - a broken/missing page never crashes the whole app)
# ------------------------------------------------------------------------------
def route(page):
    if page == "dashboard":
        show_dashboard()
        return
    try:
        mod = __import__(f"pages.{page}", fromlist=["show"])
    except ModuleNotFoundError:
        st.warning(f"صفحة '{page}' غير موجودة بعد (pages/{page}.py).")
        return
    except Exception as e:
        st.error(f"خطأ في استيراد الصفحة '{page}': {e}")
        return
    try:
        if hasattr(mod, "show"):
            mod.show()
        else:
            st.warning(f"الصفحة '{page}' لا تحتوي على دالة show()")
    except Exception as e:
        st.error(f"خطأ أثناء عرض الصفحة '{page}': {e}")


# ------------------------------------------------------------------------------
# Inline dashboard
# --------------------------------------------------------------------------------
def _safe_scalar(sql, params=()):
    try:
        # get_connection() هو context manager — لا يمكن استدعاء cursor() عليه مباشرة
        with get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(sql, params)
                r = cur.fetchone()
                return r[0] if r else 0
    except Exception as e:
        print(f"[db] {e}")
        return 0


def show_dashboard():
    st.title("📊 لوحة المعلومات")
    st.caption("نظرة عامة سريعة على النظام")

    c1, c2, c3, c4 = st.columns(4)
    c1.metric("🧾 الفواتير", _safe_scalar("SELECT COUNT(*) FROM invoices"))
    c2.metric("📦 الأصناف", _safe_scalar("SELECT COUNT(*) FROM items"))
    c3.metric("👥 الأطراف", _safe_scalar("SELECT COUNT(*) FROM parties"))
    c4.metric("💳 القروض", _safe_scalar("SELECT COUNT(*) FROM loans"))

    st.divider()
    c5, c6, c7, c8 = st.columns(4)
    c5.metric("💵 المدفوعات", _safe_scalar("SELECT COUNT(*) FROM payments"))
    c6.metric("🏦 الخزائن", _safe_scalar("SELECT COUNT(*) FROM cash_boxes"))
    c7.metric("⚠️ التالف", _safe_scalar("SELECT COUNT(*) FROM damaged_items"))
    c8.metric("🗑️ الإلغاءات", _safe_scalar("SELECT COUNT(*) FROM writeoff_requests"))

    st.divider()
    st.info("اختر صفحة من القائمة الجانبية للبدء.")


# ------------------------------------------------------------------------------
# Entry point
# ------------------------------------------------------------------------------
def main():
    if not st.session_state.get("logged_in"):
        show_login()
        return
    show_sidebar()
    page = st.session_state.get("current_page", "dashboard")
    route(page)


if __name__ == "__main__":
    main()
