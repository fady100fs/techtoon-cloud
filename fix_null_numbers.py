# fix_null_numbers.py
# يفحص قاعدة البيانات عن الأعمدة الرقمية التي فيها NULL (سبب الخطأ
# '>' not supported between instances of 'NoneType' and 'int') ويصلحها.
#
# الاستخدام (من مجلد المشروع، بجانب database.py):
#   python fix_null_numbers.py          # فحص وعرض تقرير فقط (لا يغيّر شيئاً)
#   python fix_null_numbers.py --fix    # نسخة احتياطية ثم تحويل NULL إلى 0
#
# يتخطى المفاتيح (id و *_id) لأن NULL فيها طبيعي (علاقات اختيارية).
# راجع التقرير قبل --fix: بعض الأعمدة قد يكون NULL فيها مقصوداً.
import shutil
import sys
from datetime import datetime
from pathlib import Path

from sqlalchemy import inspect, text

from database import SessionLocal

NUMERIC_HINTS = ("INT", "FLOAT", "REAL", "NUMERIC", "DECIMAL", "DOUBLE")


def find_null_columns(db):
    engine = db.get_bind()
    insp = inspect(engine)
    findings = []

    for table in insp.get_table_names():
        fk_cols = {
            c for fk in insp.get_foreign_keys(table) for c in fk["constrained_columns"]
        }
        pk_cols = set(insp.get_pk_constraint(table).get("constrained_columns") or [])

        for col in insp.get_columns(table):
            name = col["name"]
            type_name = str(col["type"]).upper()

            if not any(h in type_name for h in NUMERIC_HINTS):
                continue
            if name in fk_cols or name in pk_cols or name == "id" or name.endswith("_id"):
                continue

            count = db.execute(
                text(f'SELECT COUNT(*) FROM "{table}" WHERE "{name}" IS NULL')
            ).scalar()
            if count:
                findings.append((table, name, type_name, count))

    return findings


def backup_sqlite(engine):
    url = engine.url
    if url.get_backend_name() != "sqlite" or not url.database:
        return None
    src = Path(url.database)
    if not src.exists():
        return None
    dst = src.with_name(f"{src.stem}.backup_{datetime.now():%Y%m%d_%H%M%S}{src.suffix}")
    shutil.copy2(src, dst)
    return dst


def main():
    fix = "--fix" in sys.argv
    db = SessionLocal()
    try:
        findings = find_null_columns(db)

        if not findings:
            print("✅ لا توجد قيم NULL في الأعمدة الرقمية.")
            return

        print("الأعمدة الرقمية التي فيها قيم فارغة (NULL):\n")
        print(f"{'الجدول':<28}{'العمود':<28}{'النوع':<14}العدد")
        print("-" * 80)
        for table, col, type_name, count in findings:
            print(f"{table:<28}{col:<28}{type_name:<14}{count}")

        if not fix:
            print("\nهذا فحص فقط. لتحويل NULL إلى 0 شغّل:  python fix_null_numbers.py --fix")
            return

        backup = backup_sqlite(db.get_bind())
        if backup is None:
            print("\n⚠️ القاعدة ليست SQLite أو تعذّر العثور على ملفها، لذلك لم أُنشئ نسخة احتياطية.")
            print("   خُذ نسخة احتياطية بنفسك ثم أعد التشغيل. لم يُغيَّر شيء.")
            return
        print(f"\n💾 نسخة احتياطية: {backup}")

        for table, col, _, _ in findings:
            db.execute(text(f'UPDATE "{table}" SET "{col}" = 0 WHERE "{col}" IS NULL'))
        db.commit()
        print("✅ تم تحويل كل قيم NULL أعلاه إلى 0.")
    finally:
        db.close()


if __name__ == "__main__":
    main()
