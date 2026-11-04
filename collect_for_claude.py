# collect_for_claude.py
# يجمع الملفات اللازمة لدمج البحث بالكود وترقيم الفواتير في ملف نصي واحد
# (for_claude.txt) تُرفقه في المحادثة بدل البحث عن كل ملف.
#
# الاستخدام (من مجلد المشروع، بجانب app.py):
#   python collect_for_claude.py
#
# ما يُجمع:
#   - models.py و services.py و database.py و audit_log.py (إن وُجدت)
#   - صفحات الفواتير والأصناف والمخزون وفهرس الفواتير من مجلد pages
#   - هيكل جداول قاعدة البيانات (أسماء الجداول والأعمدة فقط، بدون أي بيانات)
# تُخفى تلقائياً القيم المشبوهة (api_key / secret / token = "...").
import re
import sys
from pathlib import Path

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

ROOT = Path(__file__).resolve().parent
OUT = ROOT / "for_claude.txt"
MAX_BYTES = 400_000  # حد أقصى لكل ملف

TOP_FILES = ["models.py", "services.py", "database.py", "audit_log.py"]
PAGE_KEYWORDS = ("الفواتير", "الاصناف", "المخزون")

SECRET_RE = re.compile(
    r"""(?i)((?:api[_-]?key|secret|token|password)\w*\s*=\s*)(["'])[^"']+\2"""
)


def pick_files():
    files = [ROOT / n for n in TOP_FILES if (ROOT / n).exists()]
    pages = ROOT / "pages"
    if pages.exists():
        for p in sorted(pages.glob("*.py")):
            if any(k in p.stem for k in PAGE_KEYWORDS):
                files.append(p)
    return files


def read_clean(path):
    data = path.read_bytes()[:MAX_BYTES]
    text = data.decode("utf-8", errors="replace")
    return SECRET_RE.sub(r"\1\2***\2", text)


def schema_dump():
    try:
        from sqlalchemy import inspect

        from database import SessionLocal

        db = SessionLocal()
        try:
            insp = inspect(db.get_bind())
            lines = []
            for table in insp.get_table_names():
                lines.append(f"[{table}]")
                for col in insp.get_columns(table):
                    flag = "" if col.get("nullable", True) else " NOT NULL"
                    lines.append(f"    {col['name']}: {col['type']}{flag}")
            return "\n".join(lines)
        finally:
            db.close()
    except Exception as e:
        return f"(تعذّر قراءة الهيكل: {e})"


def main():
    files = pick_files()
    if not files:
        print("لم أجد أي ملفات. شغّل السكربت من مجلد المشروع (بجانب app.py).")
        return

    parts = []
    for p in files:
        rel = p.relative_to(ROOT)
        parts.append(f"\n\n{'=' * 70}\n===== FILE: {rel} =====\n{'=' * 70}\n")
        parts.append(read_clean(p))

    parts.append(f"\n\n{'=' * 70}\n===== DATABASE SCHEMA =====\n{'=' * 70}\n")
    parts.append(schema_dump())

    OUT.write_text("".join(parts), encoding="utf-8")

    print("تم تجميع الملفات التالية:")
    for p in files:
        print(f"  - {p.relative_to(ROOT)}  ({p.stat().st_size / 1024:.1f} KB)")
    print(f"\nالملف الناتج: {OUT}")
    print(f"حجمه: {OUT.stat().st_size / 1024:.1f} KB")
    print("أرفقه في المحادثة (زر + ثم اختر الملف).")


if __name__ == "__main__":
    main()
