# -*- coding: utf-8 -*-
"""
remove_topbar_final.py
يزيل شريط الإجراءات السريعة العلوي بالكامل
"""
from pathlib import Path
import py_compile, re

# 1. Empty sticky_topbar.py completely
print("1. Emptying sticky_topbar.py...")
p = Path("sticky_topbar.py")
if p.exists():
    p.write_text("# This file is intentionally empty\n# Quick actions are now in the home page\n", encoding="utf-8")
    print("   OK: sticky_topbar.py emptied")

# 2. Remove all render_topbar() calls from app.py
print("2. Removing render_topbar from app.py...")
p = Path("app.py")
if p.exists():
    src = p.read_text(encoding="utf-8")
    
    # Remove any try/except blocks that call render_topbar
    src = re.sub(r"try:\s*\n\s*from sticky_topbar import render_topbar\s*\n\s*render_topbar\(\)\s*\nexcept Exception:\s*\n\s*pass\s*\n", "", src)
    src = re.sub(r"from sticky_topbar import render_topbar\s*\nrender_topbar\(\)\s*\n", "", src)
    src = re.sub(r"from sticky_topbar import render_topbar\s*\n", "", src)
    src = re.sub(r"render_topbar\(\)\s*\n", "", src)
    
    p.write_text(src, encoding="utf-8")
    try:
        py_compile.compile(str(p), doraise=True)
        print("   OK: app.py cleaned")
    except Exception as e:
        print(f"   FAIL: {e}")

# 3. Remove all quick_actions from sidebar.py
print("3. Removing quick_actions from sidebar.py...")
p = Path("sidebar.py")
if p.exists():
    src = p.read_text(encoding="utf-8")
    
    # Remove any try/except blocks that call render_quick_actions
    src = re.sub(r"try:\s*\n\s*from quick_actions import render_quick_actions\s*\n\s*render_quick_actions\(\)\s*\nexcept Exception:\s*\n\s*pass\s*\n", "", src)
    src = re.sub(r"from quick_actions import render_quick_actions\s*\nrender_quick_actions\(\)\s*\n", "", src)
    src = re.sub(r"from quick_actions import render_quick_actions\s*\n", "", src)
    src = re.sub(r"render_quick_actions\(\)\s*\n", "", src)
    
    p.write_text(src, encoding="utf-8")
    try:
        py_compile.compile(str(p), doraise=True)
        print("   OK: sidebar.py cleaned")
    except Exception as e:
        print(f"   FAIL: {e}")

print("\nDone! Run: streamlit run app.py")
