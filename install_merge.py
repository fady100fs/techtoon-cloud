# -*- coding: utf-8 -*-
"""
install_merge.py — سكربت منشئ دمج الصفحات (Techtoon Accounting)

طريقة الاستخدام:
  1) انسخ هذا الكود بالكامل
  2) احفظه في ملف اسمه install_merge.py
     في جذر مشروع Techtoon (بجانب app.py و services.py)
  3) شغّل في التيرمينال:  python install_merge.py

السكربت ينشئ 7 ملفات:
  • page_runner.py              (جديد)
  • sidebar.py                  (مستبدل + نسخة احتياطية)
  • pages/4_🧾_الفواتير.py       (جديد)
  • pages/5_📊_التقارير_الموحدة.py (جديد)
  • pages/11_💾_النسخ_الموحد.py   (جديد)
  • pages/27_👥_الموظفون_الموحد.py (جديد)
  • pages/36_⚙️_النظام_الموحد.py   (جديد)

لا يحذف أي صفحة أصلية.
"""

import os, sys
from pathlib import Path
from datetime import datetime
import py_compile

PROJECT = Path(__file__).resolve().parent
os.chdir(PROJECT)

# ══════════════════════════════════════════════════
#  محتوى الملفات
# ══════════════════════════════════════════════════
FILES = {}

FILES['page_runner.py'] = r'''# -*- coding: utf-8 -*-
"""
page_runner.py — مساعد لتشغيل صفحة Streamlit موجودة داخل تبويب.

عند دمج عدة صفحات في صفحة واحدة بتبويبات، نحتاج لتنفيذ كود الصفحة
الأصلية داخل التبويب. لكن الصفحة الأصلية تحتوي على استدعاءات رأس
(set_page_config, render_sidebar) تتعارض مع الصفحة المدموجة.

هذا المساعد:
  1) يقرأ كود الصفحة الأصلية
  2) يخطّي استدعاءات الرأس (set_page_config و render_sidebar)
  3) ينفّذ باقي الكود داخل السياق الحالي (التبويب)

الصفحات الأصلية تبقى موجودة كما هي — لا حذف ولا تعديل.
"""
import re
import unicodedata
from pathlib import Path

import streamlit as st

PAGES_DIR = Path(__file__).resolve().parent / "pages"

_STRIP_PATTERNS = [
    re.compile(r'^st\.set_page_config\(.*?\)\s*$', re.MULTILINE | re.DOTALL),
    re.compile(r'^render_sidebar\(\)\s*$', re.MULTILINE),
]


def _find_page_file(keyword):
    target = unicodedata.normalize("NFC", keyword)
    candidates = []
    for f in PAGES_DIR.glob("*.py"):
        name_nfc = unicodedata.normalize("NFC", f.name)
        if keyword in name_nfc or target in name_nfc:
            candidates.append(f)
    if not candidates:
        return None
    candidates.sort(key=lambda p: len(p.name))
    return candidates[0]


def run_page(keyword):
    page_path = _find_page_file(keyword)
    if page_path is None:
        st.error(f"⚠️ لم يتم العثور على صفحة تحتوي على: `{keyword}`")
        st.caption(f"المتوقع العثور على ملف في: `{PAGES_DIR}`")
        return
    try:
        source = page_path.read_text(encoding="utf-8")
    except Exception as e:
        st.error(f"⚠️ تعذّر قراءة الملف `{page_path.name}`: {e}")
        return
    for pattern in _STRIP_PATTERNS:
        source = pattern.sub("pass  # ⏭️ تم تخطّيه بواسطة الصفحة المدموجة", source)
    g = {"__file__": str(page_path), "__name__": "__merged_tab__", "__doc__": None, "__builtins__": __builtins__}
    try:
        exec(compile(source, str(page_path), "exec"), g)
    except Exception as e:
        st.error(f"⚠️ خطأ أثناء تشغيل `{page_path.name}`: {e}")
        import traceback
        with st.expander("🔍 تفاصيل الخطأ", expanded=False):
            st.code(traceback.format_exc(), language="python")


def run_pages_in_tabs(tabs_spec):
    labels = [f"{icon} {label}" for label, _, icon in tabs_spec]
    tabs = st.tabs(labels)
    for tab, (_, keyword, _) in zip(tabs, tabs_spec):
        with tab:
            run_page(keyword)
'''

FILES['sidebar.py'] = r'''# sidebar.py
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
    "المدفوعات": MANAGEMENT,
    "المصروفات": MANAGEMENT,
    "الخزائن": MANAGEMENT,
    "قيود_اليومية": MANAGEMENT,
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
        ("الفواتير", "الفواتير", "🧾"),
        ("المدفوعات", "المدفوعات", "💰"),
        ("حركات الخزينة", "المصروفات", "💹"),
        ("الخزائن", "الخزائن", "🏦"),
        ("قيود اليومية", "قيود_اليومية", "📝"),
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


def render_sidebar():
    caller = Path(inspect.currentframe().f_back.f_code.co_filename).resolve()
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
        if st.button("🚪 تسجيل الخروج", key="sidebar_logout"):
            logout_user()
            st.rerun()
    if caller.parent == PAGES_DIR.resolve():
        term = _term_for_file(caller)
        if not can_access(term, user):
            st.error("⛔ ليست لديك صلاحية للوصول إلى هذه الصفحة.")
            st.page_link("app.py", label="🏠 العودة إلى الصفحة الرئيسية")
            st.stop()
'''

FILES['pages/4_🧾_الفواتير.py'] = r'''# -*- coding: utf-8 -*-
"""صفحة الفواتير الموحّدة — تدمج فواتير البيع والشراء والفهرس في 3 تبويبات."""
import sys
from pathlib import Path
sys.path.append(str(Path(__file__).resolve().parent.parent))

import streamlit as st
st.set_page_config(page_title="الفواتير", page_icon="🧾", layout="wide")

from sidebar import render_sidebar, queue_state_updates
render_sidebar()

from auth_required import require_login, get_current_user_id
current_user = require_login()
current_user_id = get_current_user_id()

from page_runner import run_page

st.title("🧾 الفواتير")
st.caption("إدارة فواتير البيع والشراء وفهرس كل الفواتير في مكان واحد")

tab_sale, tab_purchase, tab_index = st.tabs(["📤 فواتير البيع", "📥 فواتير الشراء", "📋 فهرس الفواتير"])

with tab_sale:
    run_page("فواتير_البيع")
with tab_purchase:
    run_page("فواتير_الشراء")
with tab_index:
    run_page("فهرس_الفواتير")
'''

FILES['pages/5_📊_التقارير_الموحدة.py'] = r'''# -*- coding: utf-8 -*-
"""صفحة التقارير الموحّدة — تدمج 5 صفحات تقارير في 3 تبويبات."""
import sys
from pathlib import Path
sys.path.append(str(Path(__file__).resolve().parent.parent))

import streamlit as st
st.set_page_config(page_title="التقارير الموحدة", page_icon="📊", layout="wide")

from sidebar import render_sidebar
render_sidebar()

from auth_required import require_login
current_user = require_login()

from page_runner import run_page

st.title("📊 التقارير الموحدة")
st.caption("كل التقارير في مكان واحد: مالية، مخزون، مبيعات، أرباح، PDF")

tab_basic, tab_advanced, tab_pdf = st.tabs(["📊 أساسية", "📈 متقدمة", "📄 PDF"])

with tab_basic:
    run_page("التقارير")

with tab_advanced:
    report_type = st.selectbox("نوع التقرير المتقدم:", options=[
        "📈 تقارير متقدمة", "💰 تقرير الأرباح الدقيق", "📊 التقارير المالية",
    ], key="advanced_report_selector")
    report_map = {
        "📈 تقارير متقدمة": "تقارير_متقدمة",
        "💰 تقرير الأرباح الدقيق": "تقرير_الارباح",
        "📊 التقارير المالية": "التقارير_المالية",
    }
    run_page(report_map.get(report_type, "تقارير_متقدمة"))

with tab_pdf:
    run_page("تقارير_PDF")
'''

FILES['pages/11_💾_النسخ_الموحد.py'] = r'''# -*- coding: utf-8 -*-
"""صفحة النسخ الاحتياطي الموحّدة — تدمج النسخ المحلي والسحابي في 2 تبويبات."""
import sys
from pathlib import Path
sys.path.append(str(Path(__file__).resolve().parent.parent))

import streamlit as st
st.set_page_config(page_title="النسخ الاحتياطي", page_icon="💾", layout="wide")

from sidebar import render_sidebar
render_sidebar()

from auth_required import require_login, get_current_user_id, get_current_user_name
current_user = require_login()
current_user_id = get_current_user_id()
current_user_name = get_current_user_name()

from page_runner import run_page

st.title("💾 النسخ الاحتياطي")
st.caption("نسخ احتياطي محلي وسحابي في مكان واحد")

tab_local, tab_cloud = st.tabs(["💽 محلي", "☁️ سحابي"])

with tab_local:
    run_page("النسخ_الاحتياطي")
with tab_cloud:
    run_page("النسخ_السحابي")
'''

FILES['pages/27_👥_الموظفون_الموحد.py'] = r'''# -*- coding: utf-8 -*-
"""صفحة الموظفين والرواتب الموحّدة — تدمج الموظفين والقروض في 2 تبويبات."""
import sys
from pathlib import Path
sys.path.append(str(Path(__file__).resolve().parent.parent))

import streamlit as st
st.set_page_config(page_title="الموظفون والرواتب", page_icon="👥", layout="wide")

from sidebar import render_sidebar, queue_state_updates, can_modify, require_modify
render_sidebar()

from auth_required import require_login, get_current_user_id
current_user = require_login()
current_user_id = get_current_user_id()

from page_runner import run_page

st.title("👥 الموظفون والرواتب")
st.caption("إدارة الموظفين والرواتب والقروض والسلف في مكان واحد")

tab_staff, tab_loans = st.tabs(["👔 الموظفون والرواتب", "💼 القروض والسلف"])

with tab_staff:
    run_page("الموظفين_والرواتب")
with tab_loans:
    run_page("إدارة_القروض")
'''

FILES['pages/36_⚙️_النظام_الموحد.py'] = r'''# -*- coding: utf-8 -*-
"""صفحة النظام والإعدادات الموحّدة — تدمج الإعدادات والمستخدمين والفحوص في 3 تبويبات."""
import sys
from pathlib import Path
sys.path.append(str(Path(__file__).resolve().parent.parent))

import streamlit as st
st.set_page_config(page_title="النظام والإعدادات", page_icon="⚙️", layout="wide")

from sidebar import render_sidebar, queue_state_updates, can_modify, require_modify
render_sidebar()

from auth_required import require_login, get_current_user_id, get_current_user_name
current_user = require_login()
current_user_id = get_current_user_id()
current_user_name = get_current_user_name()

from page_runner import run_page

st.title("⚙️ النظام والإعدادات")
st.caption("الإعدادات وإدارة المستخدمين والفحوص في مكان واحد")

tab_settings, tab_users, tab_diag = st.tabs(["🔧 الإعدادات", "👤 المستخدمون", "🔍 الفحوص"])

with tab_settings:
    run_page("الإعدادات")
with tab_users:
    run_page("إدارة_المستخدمين")
with tab_diag:
    diag_type = st.selectbox("نوع الفحص:", options=[
        "🔍 فحص السلامة المحاسبية",
        "🩺 فحص الاتصال بقاعدة البيانات",
    ], key="diag_selector")
    diag_map = {
        "🔍 فحص السلامة المحاسبية": "فحص_السلامة",
        "🩺 فحص الاتصال بقاعدة البيانات": "فحص_الاتصال",
    }
    run_page(diag_map.get(diag_type, "فحص_السلامة"))
'''

# ══════════════════════════════════════════════════
def main():
    print("=" * 60)
    print("  Techtoon — تنصيب دمج الصفحات")
    print("=" * 60)
    print(f"\n  المشروع: {PROJECT}")

    if not (PROJECT / "services.py").exists() or not (PROJECT / "pages").is_dir():
        print("\n❌ هذا ليس جذر مشروع Techtoon!")
        sys.exit(1)

    print("\n📋 إنشاء الملفات:")
    created = backed = 0
    for relpath, content in FILES.items():
        target = PROJECT / relpath
        target.parent.mkdir(parents=True, exist_ok=True)
        if target.exists():
            ts = datetime.now().strftime('%Y%m%d_%H%M%S')
            bak = target.with_suffix(target.suffix + f'.bak_{ts}')
            bak.write_bytes(target.read_bytes())
            backed += 1
            print(f"  ✔ {relpath}  (نسخة: {bak.name})")
        else:
            print(f"  ✔ {relpath}  (جديد)")
        target.write_text(content, encoding='utf-8')
        created += 1

    print(f"\n✔ {created} ملف، نسخ احتياطي: {backed}")

    print("\n🔍 فحص صياغي:")
    ok = True
    for relpath in FILES:
        try:
            py_compile.compile(str(PROJECT / relpath), doraise=True)
            print(f"  ✔ {relpath}")
        except Exception as e:
            print(f"  ✗ {relpath}: {e}")
            ok = False

    print("\n" + "=" * 60)
    if ok:
        print("✅ تم تنصيب دمج الصفحات بنجاح!")
        print("\n  الخطوة التالية:  streamlit run app.py")
    else:
        print("⚠️ راجع الأخطاء أعلاه")
    print("=" * 60)


if __name__ == "__main__":
    main()