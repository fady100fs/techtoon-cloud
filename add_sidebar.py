# add_sidebar.py
# التشغيل من مجلد المشروع (بجانب app.py):  python add_sidebar.py
import ast
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent
PAGES = ROOT / "pages"
BACKUP = ROOT / "pages_backup"

SNIPPET = """import sys
from pathlib import Path
sys.path.append(str(Path(__file__).resolve().parent.parent))

from sidebar import render_sidebar
render_sidebar()
"""


def insertion_line(tree):
    """رقم السطر (عدد الأسطر) الذي يُدرج بعده الكود."""
    last = 0
    for node in tree.body:
        # بعد st.set_page_config(...) مباشرة
        if (
            isinstance(node, ast.Expr)
            and isinstance(node.value, ast.Call)
            and getattr(node.value.func, "attr", "") == "set_page_config"
        ):
            return node.end_lineno
        # وإلا بعد docstring و from __future__
        is_doc = (
            isinstance(node, ast.Expr)
            and isinstance(node.value, ast.Constant)
            and isinstance(node.value.value, str)
        )
        is_future = isinstance(node, ast.ImportFrom) and node.module == "__future__"
        if is_doc or is_future:
            last = node.end_lineno
        else:
            break
    return last


def main():
    if not PAGES.exists():
        print("لم أجد مجلد pages بجانب هذا الملف")
        return

    BACKUP.mkdir(exist_ok=True)
    done = skipped = failed = 0

    for f in sorted(PAGES.glob("*.py")):
        text = f.read_text(encoding="utf-8")

        if "render_sidebar" in text:
            print(f"تخطي (موجود مسبقاً): {f.name}")
            skipped += 1
            continue

        try:
            tree = ast.parse(text)
        except SyntaxError as e:
            print(f"خطأ في الصياغة، تم تخطيه: {f.name} ({e})")
            failed += 1
            continue

        shutil.copy2(f, BACKUP / f.name)  # نسخة احتياطية

        nl = "\r\n" if "\r\n" in text else "\n"
        lines = text.splitlines(keepends=True)
        idx = insertion_line(tree)

        # تأكد أن السطر السابق ينتهي بسطر جديد
        if idx and not lines[idx - 1].endswith(("\n", "\r")):
            lines[idx - 1] += nl

        snippet = nl + SNIPPET.replace("\n", nl) + nl
        lines.insert(idx, snippet)

        f.write_text("".join(lines), encoding="utf-8")
        print(f"تم التعديل: {f.name}")
        done += 1

    print(f"\nتم: {done} | متخطى: {skipped} | فشل: {failed}")
    print(f"النسخ الاحتياطية في: {BACKUP}")


if __name__ == "__main__":
    main()