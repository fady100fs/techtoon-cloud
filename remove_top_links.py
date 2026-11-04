# -*- coding: utf-8 -*-
"""
remove_top_links.py
1. يحذف الروابط النصية من أعلى الصفحة (التي تظهر كنصوص وليست مربعات)
2. يزيل السطر الفاصل تحت المربعات الملونة في الصفحة الرئيسية
"""
from pathlib import Path
import py_compile, re

# === 1. حذف الروابط النصية من أعلى الصفحة ===
print("1. Removing text links from top of app.py...")
p = Path("app.py")
if p.exists():
    src = p.read_text(encoding="utf-8")
    
    # احذف أي كتلة تحتوي على page_link مع label (الروابط النصية)
    # ابحث عن أنماط مثل: st.page_link("pages/...", label="...")
    # واحذفها إذا كانت في الأعلى (ليس داخل render_home)
    
    # ابحث عن أي كود يرسم روابط في الأعلى (قبل render_home)
    # عادة يكون بعد الـ imports مباشرة
    
    # احذف أي كود يحتوي على st.page_link خارج render_home
    # ابحث عن "st.markdown("---")" التي تأتي بعد الروابط واحذفها
    
    # نمط 1: كتلة page_link كاملة
    src = re.sub(
        r'# ⭐ اختصارات سريعة\nst\.markdown\("### ⚡ إجراءات سريعة"\)\n_qcols = st\.columns\(\d+\)\nwith _qcols\[\d+\]:\n\s*st\.page_link\([^)]+\)\nwith _qcols\[\d+\]:\n\s*st\.page_link\([^)]+\)\nwith _qcols\[\d+\]:\n\s*st\.page_link\([^)]+\)\nwith _qcols\[\d+\]:\n\s*st\.page_link\([^)]+\)\nst\.markdown\("---"\)\n',
        '',
        src
    )
    
    # نمط 2: أي استدعاء لـ render_topbar
    src = re.sub(r'try:\s*\n\s*from sticky_topbar import render_topbar\s*\n\s*render_topbar\(\)\s*\nexcept Exception:\s*\n\s*pass\s*\n', '', src)
    src = re.sub(r'from sticky_topbar import render_topbar\s*\nrender_topbar\(\)\s*\n', '', src)
    src = re.sub(r'from sticky_topbar import render_topbar\s*\n', '', src)
    src = re.sub(r'render_topbar\(\)\s*\n', '', src)
    
    # نمط 3: أي استدعاء لـ render_quick_actions
    src = re.sub(r'try:\s*\n\s*from quick_actions import render_quick_actions\s*\n\s*render_quick_actions\(\)\s*\nexcept Exception:\s*\n\s*pass\s*\n', '', src)
    src = re.sub(r'from quick_actions import render_quick_actions\s*\nrender_quick_actions\(\)\s*\n', '', src)
    src = re.sub(r'from quick_actions import render_quick_actions\s*\n', '', src)
    src = re.sub(r'render_quick_actions\(\)\s*\n', '', src)
    
    p.write_text(src, encoding="utf-8")
    try:
        py_compile.compile(str(p), doraise=True)
        print("   OK: app.py cleaned")
    except Exception as e:
        print(f"   FAIL: {e}")

# === 2. حذف السطر الفاصل تحت المربعات الملونة ===
print("2. Removing divider line under tiles...")
p = Path("app.py")
if p.exists():
    src = p.read_text(encoding="utf-8")
    
    # احذف st.markdown("---") التي تأتي بعد المربعات الملونة
    # ابحث عن النمط: بعد </div>\n    """, unsafe_allow_html=True)\n\n    st.markdown("---")
    src = src.replace('    """, unsafe_allow_html=True)\n\n    st.markdown("---")', '    """, unsafe_allow_html=True)')
    
    # أي نمط آخر للفاصل
    src = src.replace('</div>\n    """, unsafe_allow_html=True)\n\n---\n', '</div>\n    """, unsafe_allow_html=True)\n')
    
    p.write_text(src, encoding="utf-8")
    try:
        py_compile.compile(str(p), doraise=True)
        print("   OK: Divider removed")
    except Exception as e:
        print(f"   FAIL: {e}")

# === 3. تنظيف sidebar.py من أي quick_actions ===
print("3. Cleaning sidebar.py...")
p = Path("sidebar.py")
if p.exists():
    src = p.read_text(encoding="utf-8")
    
    src = re.sub(r'try:\s*\n\s*from quick_actions import render_quick_actions\s*\n\s*render_quick_actions\(\)\s*\nexcept Exception:\s*\n\s*pass\s*\n', '', src)
    src = re.sub(r'from quick_actions import render_quick_actions\s*\nrender_quick_actions\(\)\s*\n', '', src)
    src = re.sub(r'from quick_actions import render_quick_actions\s*\n', '', src)
    src = re.sub(r'render_quick_actions\(\)\s*\n', '', src)
    
    p.write_text(src, encoding="utf-8")
    try:
        py_compile.compile(str(p), doraise=True)
        print("   OK: sidebar.py cleaned")
    except Exception as e:
        print(f"   FAIL: {e}")

# === 4. تفريغ sticky_topbar.py و quick_actions.py ===
print("4. Emptying sticky_topbar.py and quick_actions.py...")
for fname in ["sticky_topbar.py", "quick_actions.py"]:
    p = Path(fname)
    if p.exists():
        p.write_text("# This file is intentionally empty\n", encoding="utf-8")
        print(f"   OK: {fname} emptied")

print("\nDone! Run: streamlit run app.py")
