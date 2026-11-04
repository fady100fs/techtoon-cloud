import time
# sidebar.py
# القائمة الجانبية (للصفحات الداخلية) + الصلاحيات + حارس الدخول
# ⭐ نسخة محدّثة: الصفحات المدموجة تحل محل الصفحات الفردية في القائمة
import inspect
import re
from pathlib import Path

import streamlit as st
from feature_flags import (
    s3_backup_enabled, barcode_enabled, pdf_reports_enabled,
    recurring_invoices_enabled, fixed_assets_enabled,
    loans_enabled, payroll_enabled,
)
from session_auth import restore_session, logout_user
from form_manager import apply_pending_clears
import os
from datetime import datetime

try:
    if os.getenv("DEPLOY_MODE", "cloud").lower() == "local":
        from sync_manager import get_sync_state, full_sync
        from local_mirror import get_local_db_size_mb
        _sync = get_sync_state()
        _size = get_local_db_size_mb()
        with st.sidebar:
            st.markdown("---")
            st.markdown("### 💾 حالة المزامنة")
            if _sync["in_progress"]:
                st.info("⏳ جاري المزامنة...")
            elif _sync["ok"] and _sync["last_sync"]:
                _ago = (datetime.now() - _sync["last_sync"]).total_seconds() / 60
                _color = "🟢" if _ago < 35 else "🟡" if _ago < 60 else "🔴"
                st.success(f"{_color} آخر مزامنة: {_sync['last_sync'].strftime('%H:%M')} (منذ {int(_ago)}د)")
            else:
                st.warning("⚠️ لم تتم المزامنة بعد")
            if _sync["error"]:
                st.caption(f"❌ {_sync['error'][:80]}")
            st.caption(f"📊 حجم SQLite: {_size:.1f} MB")
            st.caption(f"📈 صفوف مُزامَنة: {_sync['rows_synced']:,}")
            _c1, _c2 = st.columns(2)
            with _c1:
                if st.button("🔄 مزامنة", use_container_width=True):
                    with st.spinner("جاري المزامنة..."):
                        full_sync(verbose=False)
                    st.cache_data.clear()
                    st.rerun()
            with _c2:
                if st.button("🧹 كاش", use_container_width=True):
                    st.cache_data.clear()
                    st.toast("✅ تم مسح الكاش")
                    st.rerun()
except Exception as _e:
    pass

PAGES_DIR = Path(__file__).parent / "pages"

HIDE_DEFAULT_NAV = """
<style>
    [data-testid="stSidebarNav"], [data-testid="stSidebarNavItems"],
    [data-testid="stSidebarNavSeparator"], [data-testid="stSidebarNavView"] {
        display: none !important;
    }
</style>
"""

ALL = {"ADMIN", "ACCOUNTANT", "SALESPERSON", "VIEWER"}
ADMIN_ONLY = {"ADMIN"}
MANAGEMENT = {"ADMIN", "ACCOUNTANT"}
REPORTS = {"ADMIN", "ACCOUNTANT", "VIEWER"}
SALES = {"ADMIN", "ACCOUNTANT", "SALESPERSON"}

PAGE_ACCESS = {
    "تسجيل_الدخول": ALL,
    "الفواتير": SALES,
    "التقارير_الموحدة": REPORTS,
    "النسخ_الموحد": MANAGEMENT,
    "الموظفون_الموحد": ADMIN_ONLY,
    "النظام_الموحد": MANAGEMENT,
    "الاصناف": ALL,
    "التصنيفات": MANAGEMENT,
    "عملاء": ALL,
    "شجرة_الحسابات": MANAGEMENT,
    "كشف_حساب": REPORTS,
    "العملات": MANAGEMENT,
    "المصروفات": MANAGEMENT,
    "الخزائن": MANAGEMENT,
    "قيود_اليومية": MANAGEMENT,
    "سند_قبض": MANAGEMENT,
    "سند_صرف": MANAGEMENT,
    "إدارة_المخزون": MANAGEMENT,
    "الأصول_الثابتة": MANAGEMENT,
    "الإغلاق_المحاسبي": MANAGEMENT,
    "مراكز_التكلفة": MANAGEMENT,
    "الميزانية_العمومية": REPORTS,
    "لوحة_التحكم": REPORTS,
    "تحليل_الربحية": MANAGEMENT,
    "التنبيهات": ALL,
    "مركز_التذكيرات": ALL,
    "قارئ_الباركود": ALL,
    "البحث_الموحد": ALL,
    "فواتير_البيع": SALES,
    "فواتير_الشراء": SALES,
    "فهرس_الفواتير": ALL,
    "التقارير": REPORTS,
    "تقارير_متقدمة": REPORTS,
    "تقرير_الارباح": MANAGEMENT,
    "التقارير_المالية": REPORTS,
    "تقارير_PDF": MANAGEMENT,
    "النسخ_الاحتياطي": MANAGEMENT,
    "النسخ_السحابي": MANAGEMENT,
    "إدارة_القروض": MANAGEMENT,
    "الموظفين_والرواتب": ADMIN_ONLY,
    "إدارة_المستخدمين": ADMIN_ONLY,
    "الإعدادات": MANAGEMENT,
    "فحص_السلامة": MANAGEMENT,
}

GROUPS = [
    ("👤 الحساب", [("حسابي", "تسجيل_الدخول", "👤")]),
    ("📋 تكويد", [
        ("الأصناف", "الاصناف", "📦"),
        ("التصنيفات", "التصنيفات", "🗂️"),
        ("العملاء والموردين", "عملاء", "👥"),
        ("شجرة الحسابات", "شجرة_الحسابات", "📚"),
        ("كشف حساب", "كشف_حساب", "📋"),
        ("العملات", "العملات", "💱"),
    ]),
    ("⚙️ عمليات", [
        ("فواتير البيع", "فواتير_البيع", "🛒"),
        ("فواتير الشراء", "فواتير_الشراء", "🛍️"),
        ("الخزائن", "الخزائن", "🏦"),
        ("قيود اليومية", "قيود_اليومية", "📝"),
        ("📥 سند قبض", "سند_قبض", "📥"),
        ("📤 سند صرف", "سند_صرف", "📤"),
    ]),
    ("📦 مخزون", [
        ("إدارة المخزون", "إدارة_المخزون", "📦"),
        ("الأصول الثابتة", "الأصول_الثابتة", "🏭"),
    ]),
    ("💼 مالية", [
        ("الموظفون والرواتب", "الموظفون_الموحد", "👥"),
        ("الإغلاق المحاسبي", "الإغلاق_المحاسبي", "📅"),
        ("مراكز التكلفة", "مراكز_التكلفة", "🏢"),
        ("الميزانية العمومية", "الميزانية_العمومية", "⚖️"),
    ]),
    ("📊 تقارير", [
        ("لوحة التحكم", "لوحة_التحكم", "🏠"),
        ("التقارير الموحدة", "التقارير_الموحدة", "📊"),
        ("تحليل الربحية", "تحليل_الربحية", "📈"),
        ("التنبيهات", "التنبيهات", "🔔"),
        ("مركز التذكيرات", "مركز_التذكيرات", "⏰"),
    ]),
    ("🛠️ أدوات ونظام", [
        ("🔍 البحث الموحد", "البحث_الموحد", "🔍"),
        ("قارئ الباركود", "قارئ_الباركود", "📷"),
        ("💾 النسخ الاحتياطي", "النسخ_الموحد", "💾"),
        ("⚙️ النظام والإعدادات", "النظام_الموحد", "⚙️"),
    ]),
]


_sidebar_last_ts = 0.0

def _norm(text):
    text = re.sub(r"^\d+_", "", text)
    text = re.sub(r"[^\w\s]", "", text)
    return text.strip()


@st.cache_data(ttl=3600, show_spinner=False)
def _build_index():
    return [(f, _norm(f.stem)) for f in sorted(PAGES_DIR.glob("*.py"))]


def _find_page(term, index):
    t = _norm(term)
    for f, name in index:
        if name == t:
            return f
    matches = [(f, name) for f, name in index if t in name]
    if not matches:
        return None
    matches.sort(key=lambda x: len(x[1]))
    return matches[0][0]


def _role_name(user):
    role = user.get("role")
    name = getattr(role, "name", None)
    if name:
        return str(name).upper()
    try:
        from models import UserRole
        return UserRole(role).name.upper()
    except Exception:
        return str(role).upper()


def role_label(user):
    role = user.get("role", "")
    return getattr(role, "value", role)


def can_access(term, user):
    if not user:
        return False
    allowed = PAGE_ACCESS.get(term, ADMIN_ONLY)
    return _role_name(user) in allowed


CREATE_ROLES = {"ADMIN", "ACCOUNTANT", "SALESPERSON"}
MODIFY_ROLES = {"ADMIN"}
MODIFY_WORDS = ("حذف", "تعديل", "delete", "edit", "🗑", "✏")


def can_create(user=None):
    user = user or st.session_state.get("current_user")
    return bool(user) and _role_name(user) in CREATE_ROLES


def can_modify(user=None):
    user = user or st.session_state.get("current_user")
    return bool(user) and _role_name(user) in MODIFY_ROLES


def require_modify(action="هذه العملية"):
    if can_modify():
        return True
    st.error(f"⛔ {action} للمدير فقط.")
    return False


_PRESERVE_KEYS = ("current_user", "_active_page")
_EXTRA_CLEAR_KEYS = (
    "_editing_invoice_id", "_editing_invoice_no", "_editing_invoice_type",
    "_pending_state", "_pending_form_clear",
    "_inv_flash", "_items_flash", "_inv_edit_flash",
)


def _reset_page_state_on_navigation(caller_path):
    current = str(caller_path)
    prev = st.session_state.get("_active_page")
    if prev is None:
        st.session_state["_active_page"] = current
        return
    if prev == current:
        return
    for k in list(st.session_state.keys()):
        name = str(k)
        if name in _PRESERVE_KEYS:
            continue
        if name.startswith("_"):
            continue
        del st.session_state[k]
    for k in _EXTRA_CLEAR_KEYS:
        st.session_state.pop(k, None)
    st.session_state["_active_page"] = current


def queue_state_updates(delete_prefixes=(), delete_keys=(), set_values=None):
    pending = st.session_state.get("_pending_state") or {"prefixes": [], "keys": [], "set": {}}
    pending["prefixes"] = list(pending["prefixes"]) + list(delete_prefixes)
    pending["keys"] = list(pending["keys"]) + list(delete_keys)
    pending["set"] = {**pending["set"], **(set_values or {})}
    st.session_state["_pending_state"] = pending


def request_form_clear(*prefixes):
    queue_state_updates(delete_prefixes=prefixes)


def _apply_pending_state():
    try:
        pending = st.session_state.pop("_pending_state", None)
        if not pending:
            return
        delete_keys = pending.get("keys", pending.get("delete_keys", [])) or []
        set_values = pending.get("set", pending.get("set_values", {})) or {}
        delete_prefixes = pending.get("prefixes", []) or []
        for pfx in delete_prefixes:
            try:
                pfx = str(pfx)
                if not pfx:
                    continue
                for k in list(st.session_state.keys()):
                    if str(k).startswith(pfx):
                        del st.session_state[k]
            except Exception:
                continue
        if delete_keys:
            try:
                keys_iter = list(delete_keys) if not isinstance(delete_keys, str) else [delete_keys]
            except Exception:
                keys_iter = []
            for k in keys_iter:
                try:
                    name = str(k) if k is not None else ""
                    if not name:
                        continue
                    st.session_state.pop(name, None)
                except Exception:
                    continue
        if set_values and isinstance(set_values, dict):
            for k, v in set_values.items():
                try:
                    name = str(k) if k is not None else ""
                    if not name:
                        continue
                    st.session_state[name] = v
                except Exception:
                    continue
    except Exception:
        pass


def _is_modify_label(label):
    text = str(label).lower()
    return any(w in text for w in MODIFY_WORDS)


def _install_modify_guard():
    from streamlit.delta_generator import DeltaGenerator
    if getattr(DeltaGenerator.button, "_modify_guard", False):
        return
    def wrap(orig):
        def guarded(self, *args, **kwargs):
            label = args[0] if args else kwargs.get("label", "")
            if _is_modify_label(label) and not can_modify():
                kwargs["disabled"] = True
                if not kwargs.get("help"):
                    kwargs["help"] = "التعديل والحذف للمدير فقط"
                orig(self, *args, **kwargs)
                return False
            return orig(self, *args, **kwargs)
        guarded._modify_guard = True
        return guarded
    DeltaGenerator.button = wrap(DeltaGenerator.button)
    DeltaGenerator.form_submit_button = wrap(DeltaGenerator.form_submit_button)
    main_dg = getattr(st, "_main", None)
    if main_dg is not None:
        st.button = main_dg.button
        st.form_submit_button = main_dg.form_submit_button


def visible_groups(user):
    index = _build_index()
    result = []
    for group_name, pages in GROUPS:
        items = []
        for label, term, icon in pages:
            f = _find_page(term, index)
            if f and can_access(term, user):
                items.append((label, icon, f"pages/{f.name}", f))
        if items:
            result.append((group_name, items))
    return result


def _term_for_file(path):
    index = _build_index()
    for _, pages in GROUPS:
        for _, term, _ in pages:
            f = _find_page(term, index)
            if f and f.resolve() == path:
                return term
    return None


_sidebar_last_run_id = None


def _is_sidebar_already_rendered():
    global _sidebar_last_ts
    now = time.time()
    if now - _sidebar_last_ts > 0.5:
        _sidebar_last_ts = now
        return False
    return True


def render_sidebar():
    caller = Path(inspect.currentframe().f_back.f_code.co_filename).resolve()


    # ⭐ حارس Idempotency
    if _is_sidebar_already_rendered():
        return

    apply_pending_clears()
    restore_session()
    _reset_page_state_on_navigation(caller)
    _apply_pending_state()

    st.markdown(HIDE_DEFAULT_NAV, unsafe_allow_html=True)
    user = st.session_state.get("current_user")
    if not user:
        st.switch_page("app.py")
    _install_modify_guard()
    with st.sidebar:
        st.title("💼 Techtoon")
        st.caption(f"👤 {user.get('full_name', '')} — {role_label(user)}")
        if not can_modify(user):
            st.caption("✏️ التعديل والحذف للمدير فقط")
        if st.button("🔍 البحث الموحد", use_container_width=True, key="top_global_search"):
            index = _build_index()
            f = _find_page("البحث_الموحد", index)
            if f:
                st.switch_page(f"pages/{f.name}")
        st.page_link("app.py", label="🏠 الصفحة الرئيسية")
        for group_name, items in visible_groups(user):
            expanded = any(f.resolve() == caller for _, _, _, f in items)
            with st.expander(group_name, expanded=expanded):
                for label, icon, path, _ in items:
                    st.page_link(path, label=f"{icon} {label}")
        st.divider()
        
        # بحث موسّع Ctrl+K
        try:
            from global_search import render_global_search
            render_global_search()
        except Exception:
            pass

        if st.button("🚪 تسجيل الخروج", key="sidebar_logout"):
            logout_user()
            st.rerun()
    if caller.parent == PAGES_DIR.resolve():
        term = _term_for_file(caller)
        if not can_access(term, user):
            st.error("⛔ ليست لديك صلاحية للوصول إلى هذه الصفحة.")
            st.page_link("app.py", label="🏠 العودة إلى الصفحة الرئيسية")
            st.stop()
