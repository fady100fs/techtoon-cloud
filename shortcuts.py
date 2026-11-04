# shortcuts.py
"""
نظام اختصارات لوحة المفاتيح للبرنامج
يُستدعى في بداية كل صفحة.

الاختصارات (Alt بدل Ctrl — لتجنب تعارض المتصفح):
    Alt+1  ←  جديد     (تفريغ البيانات غير المحفوظة وبدء جديد)
    Alt+0  ←  حفظ      (حفظ البيان الحالي)
    Alt+2  ←  تفريغ     (تفريغ الخانات فقط)
    Alt+4  ←  حذف      (حذف بيان محفوظ)

طريقة الاستخدام في أي صفحة:
    from shortcuts import install_shortcuts, show_shortcuts_guide
    show_shortcuts_guide()          # دليل في الشريط الجانبي (اختياري)
    kb = install_shortcuts()         # في بداية الصفحة

    if kb['new']:    ...             # نفّذ "جديد"
    if kb['save']:   ...             # نفّذ "حفظ"
    if kb['clear']:  ...             # نفّذ "تفريغ"
    if kb['delete']: ...             # نفّذ "حذف"
"""

import streamlit as st
import streamlit.components.v1 as components


# ==========================================================
# CSS لإخفاء الأزرار المخفية
# ==========================================================
_HIDE_BUTTONS_CSS = """
<style>
    /* إخفاء أزرار الاختصارات الأربعة تمامًا */
    .st-key-kb_shortcut_new,
    .st-key-kb_shortcut_save,
    .st-key-kb_shortcut_clear,
    .st-key-kb_shortcut_delete {
        display: none !important;
        height: 0 !important;
        overflow: hidden !important;
        position: absolute !important;
        left: -99999px !important;
        top: -99999px !important;
    }

    /* ستايل تلميح الاختصارات */
    .kb-hint {
        position: fixed;
        bottom: 16px;
        inset-inline-end: 16px;
        background: rgba(0,0,0,0.75);
        color: #fff;
        padding: 8px 12px;
        border-radius: 10px;
        font-size: 11px;
        line-height: 1.6;
        z-index: 9999;
        opacity: 0.55;
        transition: opacity .2s;
        pointer-events: none;
        direction: rtl;
    }
    .kb-hint:hover { opacity: 1; }
    .kb-key {
        background: #2a2a3a;
        padding: 1px 6px;
        border-radius: 4px;
        font-family: monospace;
        margin: 0 2px;
    }
</style>
"""


# ==========================================================
# JavaScript: يربط اختصارات Alt بالأزرار المخفية
# ==========================================================
_BIND_JS = """
<script>
(function() {
    // منع ربط مزدوج
    if (window.__kbShortcutsBound) return;
    window.__kbShortcutsBound = true;

    // خريطة: مفتاح الرقم ← key الزر المخفي في Streamlit
    const KEY_MAP = {
        '1': 'kb_shortcut_new',     // Alt+1
        '0': 'kb_shortcut_save',    // Alt+0
        '2': 'kb_shortcut_clear',   // Alt+2
        '4': 'kb_shortcut_delete',  // Alt+4
    };

    function clickShortcut(elemKey) {
        try {
            const doc = window.parent.document;
            // Streamlit يضيف كلاس st-key-<key> على الحاوية
            const wrap = doc.querySelector('.st-key-' + elemKey);
            if (!wrap) {
                console.warn('[shortcuts] wrapper not found for', elemKey);
                return false;
            }
            const btn = wrap.querySelector('button');
            if (!btn) {
                console.warn('[shortcuts] button not found in', elemKey);
                return false;
            }
            btn.click();
            return true;
        } catch (err) {
            console.warn('[shortcuts] click failed:', err);
            return false;
        }
    }

    try {
        const doc = window.parent.document;
        doc.addEventListener('keydown', function(e) {
            // لازم Alt (بدون Ctrl/Meta/Shift لتفادي التعارض)
            if (!e.altKey) return;
            if (e.ctrlKey || e.metaKey || e.shiftKey) return;

            // بعض المتصفحات ترجع e.key = '¡' أو رمز مشابه عند Alt+1 على لوحة أرقام
            // نعتمد على e.code لأنه أكثر ثباتًا
            let digit = null;
            const code = String(e.code || '');
            if (code.startsWith('Digit')) {
                digit = code.slice(5);   // 'Digit1' → '1'
            } else if (code.startsWith('Numpad')) {
                digit = code.slice(6);   // 'Numpad1' → '1'
            } else {
                // fallback: e.key مباشرة
                const k = String(e.key || '');
                if (k >= '0' && k <= '9') digit = k;
            }

            if (!digit || !(digit in KEY_MAP)) return;

            // منع سلوك المتصفح الافتراضي (لو مسموح)
            try { e.preventDefault(); e.stopPropagation(); } catch (_) {}

            clickShortcut(KEY_MAP[digit]);
        }, true);

        console.log('[shortcuts] Alt shortcuts bound successfully');
    } catch (err) {
        console.warn('[shortcuts] bind failed:', err);
    }
})();
</script>
"""


# ==========================================================
# الدالة الرئيسية: تركيب الاختصارات
# ==========================================================
def install_shortcuts():
    """
    يستدعى في بداية كل صفحة.
    يرجّع dict فيه flags للأحداث اللي اتنفذت في هذا الـ run:

        {
            'new':    bool,   # Alt+1
            'save':   bool,   # Alt+0
            'clear':  bool,   # Alt+2
            'delete': bool,   # Alt+4
        }

    ملاحظة: كل flag = True لمدة تشغيل واحدة فقط.
    """
    # CSS لإخفاء الأزرار
    st.markdown(_HIDE_BUTTONS_CSS, unsafe_allow_html=True)

    # الأزرار المخفية الأربعة
    new_clicked    = st.button("kb_new",    key="kb_shortcut_new",    help="Alt+1")
    save_clicked   = st.button("kb_save",   key="kb_shortcut_save",   help="Alt+0")
    clear_clicked  = st.button("kb_clear",  key="kb_shortcut_clear",  help="Alt+2")
    delete_clicked = st.button("kb_delete", key="kb_shortcut_delete", help="Alt+4")

    # JavaScript لربط الاختصارات بالأزرار
    components.html(_BIND_JS, height=0, width=0)

    return {
        'new':    new_clicked,
        'save':   save_clicked,
        'clear':  clear_clicked,
        'delete': delete_clicked,
    }


# ==========================================================
# دليل الاختصارات في الشريط الجانبي
# ==========================================================
def show_shortcuts_guide():
    """عرض دليل الاختصارات في الشريط الجانبي (اختياري)."""
    st.sidebar.markdown("---")
    st.sidebar.markdown("### ⌨️ اختصارات لوحة المفاتيح")
    st.sidebar.markdown(
        """
        <div style="font-size: 12px; line-height: 1.9;">
            <div>🆕 <b>Alt + 1</b> — جديد</div>
            <div>💾 <b>Alt + 0</b> — حفظ</div>
            <div>🧹 <b>Alt + 2</b> — تفريغ الخانات</div>
            <div>🗑 <b>Alt + 4</b> — حذف محفوظ</div>
        </div>
        """,
        unsafe_allow_html=True,
    )


# ==========================================================
# تلميح عائم صغير في أسفل الشاشة (اختياري)
# ==========================================================
def show_floating_hint():
    """تلميح عائم صغير في أسفل الصفحة يذكّر بالاختصارات."""
    st.markdown(
        """
        <div class="kb-hint">
            <span class="kb-key">Alt+1</span> جديد
            &nbsp;|&nbsp;
            <span class="kb-key">Alt+0</span> حفظ
            &nbsp;|&nbsp;
            <span class="kb-key">Alt+2</span> تفريغ
            &nbsp;|&nbsp;
            <span class="kb-key">Alt+4</span> حذف
        </div>
        """,
        unsafe_allow_html=True,
    )