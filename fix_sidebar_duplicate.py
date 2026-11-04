# -*- coding: utf-8 -*-
"""
fix_sidebar_duplicate.py — إصلاح خطأ StreamlitDuplicateElementKey

المشكلة: عند دمج الصفحات، render_sidebar() تُستدعى مرتين:
  1) من الصفحة المدموجة (4_🧾_الفواتير.py)
  2) من render_invoice_page() داخل invoice_page_common.py
  → StreamlitDuplicateElementKey: key='top_global_search'

الحل: جعل render_sidebar() idempotent — تتخطّى التنفيذ إذا استُدعيت
مرتين في نفس الـ script run، باستخدام Streamlit's ScriptRunContext.

التشغيل: python fix_sidebar_duplicate.py
"""
import re
from pathlib import Path

PROJECT = Path(__file__).resolve().parent
SIDEBAR = PROJECT / "sidebar.py"

# ─────────────────────────────────────────────────────────
# 1) إضافة متغير module-level + دالة مساعدة
# ─────────────────────────────────────────────────────────

NEW_HELPER = '''_sidebar_last_run_id = None


def _is_sidebar_already_rendered():
    """⭐ تحقق إذا كانت render_sidebar() استُدعيت بالفعل في هذا الـ script run.

    يمنع خطأ StreamlitDuplicateElementKey عندما تستدعي الصفحة المدموجة
    render_sidebar() ثم تستدعيها render_invoice_page() مرة ثانية.
    """
    global _sidebar_last_run_id
    try:
        from streamlit.runtime.scriptrunner import get_script_run_ctx
        ctx = get_script_run_ctx()
        run_id = id(ctx) if ctx else 0
    except Exception:
        run_id = 0

    # script run جديد؟ أعد ضبط العلم
    if run_id != _sidebar_last_run_id:
        _sidebar_last_run_id = run_id
        st.session_state["_sidebar_rendered"] = False

    # سبق رسمها في هذا الـ run؟ تخطّى
    if st.session_state.get("_sidebar_rendered", False):
        return True

    st.session_state["_sidebar_rendered"] = True
    return False


'''

# ─────────────────────────────────────────────────────────
# 2) إدراج استدعاء الحارس داخل render_sidebar()
# ─────────────────────────────────────────────────────────

OLD_BLOCK = """def render_sidebar():
    caller = Path(inspect.currentframe().f_back.f_code.co_filename).resolve()

    apply_pending_clears()
    restore_session()
    _reset_page_state_on_navigation(caller)
    _apply_pending_state()
    st.markdown(HIDE_DEFAULT_NAV, unsafe_allow_html=True)"""

NEW_BLOCK = """def render_sidebar():
    caller = Path(inspect.currentframe().f_back.f_code.co_filename).resolve()

    apply_pending_clears()
    restore_session()
    _reset_page_state_on_navigation(caller)
    _apply_pending_state()

    # ⭐ حارس Idempotency: تخطّي إذا استُدعيت render_sidebar بالفعل
    #    في هذا الـ script run (يمنع StreamlitDuplicateElementKey)
    if _is_sidebar_already_rendered():
        return

    st.markdown(HIDE_DEFAULT_NAV, unsafe_allow_html=True)"""


def main():
    print("=" * 60)
    print("  Techtoon — إصلاح خطأ duplicate element key")
    print("=" * 60)

    if not SIDEBAR.exists():
        print(f"❌ {SIDEBAR} غير موجود!")
        return

    src = SIDEBAR.read_text(encoding="utf-8")

    # فحص idempotency — هل الإصلاح مطبّق بالفعل؟
    if "_is_sidebar_already_rendered" in src:
        print("✅ الإصلاح مُطبّق مسبقاً — لا حاجة لإعادة التطبيق.")
        return

    # 1) إضافة المتغير + الدالة المساعدة قبل render_sidebar()
    marker = "def render_sidebar():"
    if marker not in src:
        print("❌ لم يتم العثور على 'def render_sidebar():' في sidebar.py")
        return

    src = src.replace(marker, NEW_HELPER + marker, 1)
    print("✔ تم إضافة الدالة المساعدة _is_sidebar_already_rendered()")

    # 2) استبدال البلوك القديم بالجديد (مع الحارس)
    if OLD_BLOCK not in src:
        print("⚠ لم يتم العثور على البلوك المستهدف بدقة.")
        print("  محاولة بديلة: إدراج الحارس بعد _apply_pending_state()...")
        alt_old = """    _apply_pending_state()
    st.markdown(HIDE_DEFAULT_NAV, unsafe_allow_html=True)"""
        alt_new = """    _apply_pending_state()

    # ⭐ حارس Idempotency
    if _is_sidebar_already_rendered():
        return

    st.markdown(HIDE_DEFAULT_NAV, unsafe_allow_html=True)"""
        if alt_old in src:
            src = src.replace(alt_old, alt_new, 1)
            print("✔ تم إدراج الحارس (بديل)")
        else:
            print("❌ تعذّر إدراج الحارس — راجع sidebar.py يدوياً")
            return
    else:
        src = src.replace(OLD_BLOCK, NEW_BLOCK, 1)
        print("✔ تم إدراج الحارس في render_sidebar()")

    # نسخة احتياطية
    from datetime import datetime
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    bak = SIDEBAR.with_suffix(f".py.bak_{ts}")
    bak.write_bytes(SIDEBAR.read_bytes())
    print(f"✔ نسخة احتياطية: {bak.name}")

    SIDEBAR.write_text(src, encoding="utf-8")
    print(f"✔ تم تحديث {SIDEBAR.name}")

    # فحص صياغي
    import py_compile
    try:
        py_compile.compile(str(SIDEBAR), doraise=True)
        print("✔ فحص صياغي: ناجح")
    except Exception as e:
        print(f"✗ فحص صياغي: {e}")

    print()
    print("=" * 60)
    print("✅ تم الإصلاح! أعد تشغيل:  streamlit run app.py")
    print("=" * 60)


if __name__ == "__main__":
    main()