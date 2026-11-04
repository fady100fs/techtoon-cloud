# -*- coding: utf-8 -*-
"""
fix_quick_actions.py
1. يزيل شريط الإجراءات السريعة العلوي (sticky_topbar)
2. يزيل الإجراءات السريعة من الشريط الجانبي (quick_actions)
3. يبقي المربعات الملونة في الصفحة الرئيسية (render_home) فقط
4. يجعل النصوص داخل المربعات بيضاء وواضحة
"""
from pathlib import Path
import py_compile, re

# === 1. إزالة sticky_topbar من app.py ===
print("1. Removing sticky_topbar from app.py...")
p = Path("app.py")
if p.exists():
    src = p.read_text(encoding="utf-8")
    
    # احذف أي استدعاء لـ render_topbar
    src = re.sub(r'try:\s*\n\s*from sticky_topbar import render_topbar\s*\n\s*render_topbar\(\)\s*\nexcept Exception:\s*\n\s*pass\s*\n', '', src)
    src = re.sub(r'from sticky_topbar import render_topbar\s*\nrender_topbar\(\)\s*\n', '', src)
    
    # === 4. تحسين النصوص داخل المربعات ===
    # ابحث عن qt-box وحدث color و font-size
    if 'qt-box' in src or 'qt-tile' in src:
        src = src.replace('color: white;', 'color: #ffffff !important; text-shadow: 0 1px 3px rgba(0,0,0,0.5);')
        src = src.replace('font-size: 14px;', 'font-size: 16px;')
        src = src.replace('font-size: 13px;', 'font-size: 16px;')
        src = src.replace('font-size: 12px;', 'font-size: 16px;')
        print("   OK: Updated tile text colors to white")

    p.write_text(src, encoding="utf-8")
    try:
        py_compile.compile(str(p), doraise=True)
        print("   OK: app.py updated")
    except Exception as e:
        print(f"   FAIL: {e}")

# === 2. إزالة quick_actions من sidebar.py ===
print("2. Removing quick_actions from sidebar.py...")
p = Path("sidebar.py")
if p.exists():
    src = p.read_text(encoding="utf-8")
    
    # احذف أي استدعاء لـ render_quick_actions
    src = re.sub(r'try:\s*\n\s*from quick_actions import render_quick_actions\s*\n\s*render_quick_actions\(\)\s*\nexcept Exception:\s*\n\s*pass\s*\n', '', src)
    src = re.sub(r'from quick_actions import render_quick_actions\s*\nrender_quick_actions\(\)\s*\n', '', src)
    # احذف التعليق
    src = src.replace('# أزرار سريعة\n', '')
    src = src.replace('# أزرار سريعة\n        try:\n            from quick_actions import render_quick_actions\n            render_quick_actions()\n        except Exception:\n            pass\n', '')
    
    p.write_text(src, encoding="utf-8")
    try:
        py_compile.compile(str(p), doraise=True)
        print("   OK: sidebar.py updated")
    except Exception as e:
        print(f"   FAIL: {e}")

print("\nDone! Run: streamlit run app.py")
