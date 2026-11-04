# -*- coding: utf-8 -*-
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
