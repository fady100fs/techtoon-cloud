# add_expcat_functions.py
# يضيف دوال إدارة تصنيفات المصروفات لـ services.py
import shutil
from pathlib import Path

SERVICES = Path(__file__).parent / "services.py"
BACKUP = SERVICES.with_suffix(".py.before_expcat")

if not SERVICES.exists():
    print("ERROR: services.py not found")
    exit(1)

shutil.copy(SERVICES, BACKUP)
print(f"Backup created: {BACKUP.name}")

content = SERVICES.read_text(encoding="utf-8")

NEW_FUNCTIONS = '''

# ==========================================
# 25. دوال إدارة تصنيفات المصروفات (جديد)
# ==========================================
def get_expense_categories(include_inactive=False):
    """يرجع كل تصنيفات المصروفات مرتبة بالاسم."""
    db = SessionLocal()
    try:
        q = db.query(ExpenseCategory)
        if not include_inactive:
            q = q.filter(ExpenseCategory.is_active == True)
        return q.order_by(ExpenseCategory.name).all()
    finally:
        db.close()


def get_expense_category_by_id(category_id):
    """يرجع تصنيف واحد بالمعرف."""
    db = SessionLocal()
    try:
        return db.query(ExpenseCategory).filter(
            ExpenseCategory.id == category_id
        ).first()
    finally:
        db.close()


def update_expense_category(category_id, name=None, description=None,
                             account_id=None, is_active=None):
    """يعدل تصنيف مصروف موجود."""
    db = SessionLocal()
    try:
        cat = db.query(ExpenseCategory).filter(ExpenseCategory.id == category_id).first()
        if not cat:
            raise ValueError("تصنيف المصروف غير موجود!")

        if name is not None:
            name = name.strip()
            if not name:
                raise ValueError("اسم التصنيف مطلوب!")
            existing = db.query(ExpenseCategory).filter(
                ExpenseCategory.name == name,
                ExpenseCategory.id != category_id,
            ).first()
            if existing:
                raise ValueError(f"يوجد تصنيف آخر باسم '{name}'!")
            cat.name = name

            # تحديث اسم الحساب المرتبط
            if cat.account_id:
                acc = db.query(models.Account).filter(
                    models.Account.id == cat.account_id
                ).first()
                if acc:
                    acc.name = name

        if description is not None:
            cat.description = description.strip() if description else None

        if account_id is not None:
            acc = db.query(models.Account).filter(
                models.Account.id == account_id
            ).first()
            if not acc:
                raise ValueError("الحساب غير موجود!")
            cat.account_id = account_id

        if is_active is not None:
            cat.is_active = bool(is_active)

        db.commit()
        return cat
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def delete_expense_category(category_id, force=False):
    """يحذف تصنيف مصروف.
    - لو فيه مصروفات مرتبطة يرفض إلا لو force=True.
    """
    db = SessionLocal()
    try:
        cat = db.query(ExpenseCategory).filter(
            ExpenseCategory.id == category_id
        ).first()
        if not cat:
            raise ValueError("تصنيف المصروف غير موجود!")

        # نحفظ account_id قبل الحذف
        account_id = cat.account_id

        expenses_count = db.query(Expense).filter(
            Expense.category_id == category_id
        ).count()

        if expenses_count > 0:
            if not force:
                raise ValueError(
                    f"لا يمكن الحذف: يوجد {expenses_count} مصروف مرتبط."
                )
            # حذف قسري: نحذف المصروفات المرتبطة + القيود المحاسبية
            expenses_list = db.query(Expense).filter(
                Expense.category_id == category_id
            ).all()
            for exp in expenses_list:
                # نحذف القيود المحاسبية المرتبطة
                je = db.query(models.JournalEntry).filter(
                    models.JournalEntry.reference_type == "expense",
                    models.JournalEntry.description.contains(exp.description or ""),
                ).first()
                if je:
                    db.query(models.JournalLine).filter(
                        models.JournalLine.entry_id == je.id
                    ).delete(synchronize_session=False)
                    db.delete(je)
                db.delete(exp)

        # حذف الحساب المرتبط لو مفيش حساب آخر يستخدمه
        db.delete(cat)
        db.flush()

        if account_id:
            # نتأكد إن الحساب مش مستخدم في مكان آخر
            other_usage = db.query(ExpenseCategory).filter(
                ExpenseCategory.account_id == account_id
            ).first()
            if not other_usage:
                acc = db.query(models.Account).filter(
                    models.Account.id == account_id
                ).first()
                if acc:
                    db.delete(acc)

        db.commit()
        return True
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def deactivate_expense_category(category_id):
    """يعطل تصنيف بدل حذفه."""
    return update_expense_category(category_id, is_active=False)


def activate_expense_category(category_id):
    """ينشط تصنيف معطل."""
    return update_expense_category(category_id, is_active=True)


def get_expense_category_usage(category_id):
    """إحصائيات استخدام تصنيف: عدد المصروفات + الإجمالي."""
    db = SessionLocal()
    try:
        count = db.query(Expense).filter(
            Expense.category_id == category_id
        ).count()
        total = db.query(func.sum(Expense.amount)).filter(
            Expense.category_id == category_id
        ).scalar() or 0.0
        return {"count": count, "total": float(total)}
    finally:
        db.close()
'''

if "def update_expense_category" in content:
    print("INFO: Functions already exist - no changes needed")
else:
    content = content.rstrip() + "\n" + NEW_FUNCTIONS + "\n"
    SERVICES.write_text(content, encoding="utf-8")
    print("OK: Added 6 functions for expense category management")

print("DONE!")