# form_manager.py
"""
مدير النماذج الموحّد
====================
- يوفر طريقة موحّدة لتفريغ كل خانات أي نموذج بعد الحفظ/الحذف/التعديل
- يعتمد على بادئات (prefixes) لمفاتيح session_state
- يستخدم Queue للتنفيذ في الـ run التالي (لتجنّب خطأ Streamlit)

═══════════════════════════════════════════════════════════
المبدأ العام (اقرأه مرة واحدة، طبّقه في كل صفحة):
═══════════════════════════════════════════════════════════

1. في كل صفحة، حدّد بادئة ثابتة للنموذج:
       PREFIX = "cm_"        # مثال: حركات الخزينة

2. كل widget في النموذج يستخدم مفتاحاً بهذه البادئة:
       st.text_input("الوصف:", key=f"{PREFIX}desc")
       st.number_input("المبلغ:", key=f"{PREFIX}amount")
       st.selectbox("الحساب:", key=f"{PREFIX}account", ...)

3. بعد الحفظ/الحذف/التعديل، استدعِ:
       clear_form(PREFIX)
       st.rerun()

   → سيُفرّغ كل المفاتيح التي تبدأ بـ "cm_"

═══════════════════════════════════════════════════════════
"""

import streamlit as st


# المفتاح المخفي الذي نخزّن فيه الطلبات المعلّقة
_PENDING_CLEAR_KEY = "_pending_form_clear"


def clear_form(*prefixes, extra_keys=(), exclude_keys=()):
    """يؤجّل حذف كل المفاتيح التي تبدأ بـ prefixes إلى بداية الـ run التالي.

    Args:
        *prefixes: بادئات المفاتيح (مثل "cm_", "inv_", "pay_")
        extra_keys: مفاتيح إضافية بدون بادئة (نادرة الاستخدام)
        exclude_keys: مفاتيح مستثناة من الحذف حتى لو طابقت prefix

    مثال:
        clear_form("cm_")          # يحذف cm_desc, cm_amount, ...
        clear_form("inv_", "new_") # يحذف بادئتين
    """
    if _PENDING_CLEAR_KEY not in st.session_state:
        st.session_state[_PENDING_CLEAR_KEY] = {
            "prefixes": set(),
            "keys": set(),
            "exclude": set(),
        }

    pending = st.session_state[_PENDING_CLEAR_KEY]
    pending["prefixes"].update(prefixes)
    pending["keys"].update(extra_keys)
    pending["exclude"].update(exclude_keys)


def clear_all_except(*keep_prefixes, extra_keep=()):
    """يحذف كل المفاتيح ما عدا قائمة prefixes معينة.

    مفيد لو أردت تفريغ كل شيء في الصفحة ما عدا بعض المفاتيح.
    """
    preserve = {"current_user", "_active_page", "_pending_form_clear",
                "_pending_state", "registered_forms"}
    keep = tuple(keep_prefixes) + tuple(extra_keep)

    for k in list(st.session_state.keys()):
        name = str(k)
        if name in preserve or name.startswith("_"):
            continue
        if any(name.startswith(p) for p in keep):
            continue
        if name in keep:
            continue
        del st.session_state[k]


def apply_pending_clears():
    """يُنفّذ الحذف المُؤجّل — يُستدعى تلقائياً من sidebar."""
    pending = st.session_state.pop(_PENDING_CLEAR_KEY, None)
    if not pending:
        return

    prefixes = tuple(pending.get("prefixes", ()))
    keys = set(pending.get("keys", ()))
    excludes = set(pending.get("exclude", ()))

    preserve_always = {
        "current_user", "_active_page", "registered_forms",
        "_pending_state", "_pending_form_clear",
    }

    for k in list(st.session_state.keys()):
        name = str(k)
        if name in preserve_always:
            continue
        if name.startswith("_"):
            continue
        if name in excludes:
            continue
        if name in keys:
            del st.session_state[k]
            continue
        if prefixes and name.startswith(prefixes):
            del st.session_state[k]


def show_clear_hint():
    """يعرض تلميحاً للمستخدم بأن الحقول ستُفرَّغ بعد الحفظ."""
    st.caption("💡 بعد الحفظ، ستُفرَّغ كل الحقول تلقائياً.")


# ═══════════════════════════════════════════════════════════
# القاعدة الذهبية (Reference للنسخ في الصفحات):
# ═══════════════════════════════════════════════════════════
#
# from form_manager import clear_form
#
# PREFIX = "xx_"   # بادئة فريدة لكل صفحة
#
# st.text_input("...", key=f"{PREFIX}name")
# st.number_input("...", key=f"{PREFIX}amount")
#
# if st.button("💾 حفظ"):
#     save_data()
#     clear_form(PREFIX)
#     st.rerun()
#
# ═══════════════════════════════════════════════════════════