# -*- coding: utf-8 -*-
"""
fix_tiles_native.py
يستبدل المربعات الملونة (HTML) بـ st.page_link (تعمل داخل webview)
"""
from pathlib import Path
import py_compile, re

p = Path("app.py")
src = p.read_text(encoding="utf-8")

# ابحث عن أي كتلة تحتوي على qt-grid أو qt-container أو QUICK_CSS أو QUICK_TILES
# واحذفها كاملة
patterns_to_remove = [
    r'# ⭐ اختصارات سريعة بمربعات ملونة\nQUICK_CSS = """[\s\S]*?unsafe_allow_html=True\)\n',
    r'# ⭐ اختصارات سريعة بمربعات ملونة\nQUICK_TILES_CSS = """[\s\S]*?unsafe_allow_html=True\)\n',
    r'QUICK_CSS = """[\s\S]*?"""\n\nif "current_user" in st\.session_state:\n\s*st\.markdown\(QUICK_CSS, unsafe_allow_html=True\)\n\s*st\.markdown\("""[\s\S]*?""", unsafe_allow_html=True\)\n',
    r'QUICK_TILES_CSS = """[\s\S]*?"""\n\nif "current_user" in st\.session_state:\n\s*st\.markdown\(QUICK_TILES_CSS, unsafe_allow_html=True\)\n\s*st\.markdown\("""[\s\S]*?""", unsafe_allow_html=True\)\n',
    r'st\.markdown\("""[\s\S]*?qt-grid[\s\S]*?""", unsafe_allow_html=True\)\n',
    r'st\.markdown\("""[\s\S]*?qt-container[\s\S]*?""", unsafe_allow_html=True\)\n',
    r'st\.markdown\("""[\s\S]*?qt-box[\s\S]*?""", unsafe_allow_html=True\)\n',
    r'st\.markdown\("""[\s\S]*?qa-grid[\s\S]*?""", unsafe_allow_html=True\)\n',
    r'st\.markdown\("""[\s\S]*?qa-tile[\s\S]*?""", unsafe_allow_html=True\)\n',
    r'# Quick Tiles\nQUICK_TILES_CSS = """[\s\S]*?""", unsafe_allow_html=True\)\n',
    r'# Quick Tiles\n[\s\S]*?unsafe_allow_html=True\)\n',
]

removed = False
for pattern in patterns_to_remove:
    new_src = re.sub(pattern, '', src)
    if new_src != src:
        src = new_src
        removed = True

# أيضاً احذف أي page_link قديم في الأعلى (ليس داخل render_home)
# ابحث عن "### ⚡ إجراءات سريعة" واحذف الكتلة
src = re.sub(r'# ⭐ أزرار سندات القبض والصرف[\s\S]*?st\.markdown\("---"\)\n', '', src)
src = re.sub(r'st\.markdown\("### ⚡ إجراءات سريعة"\)[\s\S]*?st\.markdown\("---"\)\n', '', src)

# ابحث عن "if not groups:" وأضف الكود الجديد قبله
old = '    if not groups:'
new = '''    # ⭐ اختصارات سريعة بمربعات ملونة (تعمل داخل webview)
    st.markdown(
        "<style>"
        ".st-key-qt1 button, .st-key-qt2 button, .st-key-qt3 button, .st-key-qt4 button {"
        "height: 80px !important; border-radius: 10px !important; border: none !important;"
        "color: white !important; font-size: 14px !important; font-weight: 600 !important;"
        "display: flex !important; flex-direction: column !important;"
        "align-items: center !important; justify-content: center !important; gap: 4px !important;"
        "}"
        ".st-key-qt1 button { background: linear-gradient(135deg, #27ae60, #1e8449) !important; }"
        ".st-key-qt2 button { background: linear-gradient(135deg, #2d9cdb, #1a5276) !important; }"
        ".st-key-qt3 button { background: linear-gradient(135deg, #e67e22, #d35400) !important; }"
        ".st-key-qt4 button { background: linear-gradient(135deg, #c0392b, #922b21) !important; }"
        ".st-key-qt1 button p, .st-key-qt2 button p, .st-key-qt3 button p, .st-key-qt4 button p {"
        "color: white !important; font-size: 14px !important; font-weight: 600 !important;"
        "}"
        ".st-key-qt1 button span, .st-key-qt2 button span, .st-key-qt3 button span, .st-key-qt4 button span {"
        "font-size: 1.6rem !important;"
        "}"
        "</style>",
        unsafe_allow_html=True,
    )

    _qc1, _qc2, _qc3, _qc4 = st.columns(4)
    with _qc1:
        st.page_link("pages/4A_🛒_فواتير_البيع.py", label="فاتورة بيع", icon="🛒", key="qt1")
    with _qc2:
        st.page_link("pages/4B_🛍️_فواتير_الشراء.py", label="فاتورة شراء", icon="🛍️", key="qt2")
    with _qc3:
        st.page_link("pages/41_📥_سند_قبض.py", label="سند قبض", icon="📥", key="qt3")
    with _qc4:
        st.page_link("pages/42_📤_سند_صرف.py", label="سند صرف", icon="📤", key="qt4")

    if not groups:'''

if old in src and "st-key-qt1" not in src:
    src = src.replace(old, new, 1)
    print("   OK: Added native page_link tiles")
elif "st-key-qt1" in src:
    print("   OK: Native tiles already present")
else:
    print("   WARN: Could not find insertion point")

p.write_text(src, encoding="utf-8")
try:
    py_compile.compile(str(p), doraise=True)
    print("   OK: Syntax valid")
except Exception as e:
    print(f"   FAIL: {e}")

print("\nDone! Run: streamlit run app.py")
