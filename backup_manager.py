# backup_manager.py
"""
مدير النسخ الاحتياطي — نسخة متوافقة مع Neon PostgreSQL
+ إمكانية تغيير مكان النسخ الاحتياطي من داخل البرنامج.
"""

import os
import json
import shutil
from datetime import datetime, timedelta
from pathlib import Path

import pandas as pd
from sqlalchemy import text

from database import engine


# ==========================================================
# الثوابت
# ==========================================================
BACKUP_SETTINGS = {
    'enabled': True,
    'interval_hours': 24,
    'max_backups': 14,
    'backup_folder': None,
}

CONFIG_FILE = Path(__file__).parent / "backup_config.json"
DEFAULT_BACKUP_FOLDER = Path(__file__).parent / "backups"


ALL_TABLES = [
    "currencies", "accounts", "users", "accounting_periods",
    "parties", "cash_boxes", "categories", "items",
    "invoices", "invoice_lines", "cost_history", "kit_components",
    "inventory_movements", "cost_centers", "cost_allocations",
    "cost_center_transactions", "journal_entries", "journal_lines",
    "payments", "cash_transfers", "expense_categories", "expenses",
    "warehouses", "stock_levels", "warehouse_transfers", "stock_counts",
    "fixed_assets", "depreciation_records", "loans", "loan_installments",
    "employees", "salary_records", "budgets",
]


# ==========================================================
# إعدادات مكان الحفظ (Config File)
# ==========================================================
def _load_backup_folder():
    """يقرأ المكان المحفوظ من ملف الإعدادات، أو الافتراضي."""
    try:
        if CONFIG_FILE.exists():
            data = json.loads(CONFIG_FILE.read_text(encoding="utf-8"))
            folder = data.get("backup_folder")
            if folder:
                p = Path(folder)
                p.mkdir(parents=True, exist_ok=True)
                return p
    except Exception:
        pass
    DEFAULT_BACKUP_FOLDER.mkdir(parents=True, exist_ok=True)
    return DEFAULT_BACKUP_FOLDER


def _save_backup_folder(folder_path):
    """يحفظ المكان في ملف الإعدادات."""
    try:
        CONFIG_FILE.write_text(
            json.dumps(
                {"backup_folder": str(folder_path)},
                ensure_ascii=False,
                indent=2,
            ),
            encoding="utf-8",
        )
        return True
    except Exception:
        return False


class BackupManager:
    """مدير النسخ الاحتياطي المتوافق مع Neon."""

    def __init__(self, db_path=None, backup_folder=None):
        self.db_path = db_path

        # نستخدم المكان المخصص (لو موجود) أو الافتراضي
        if backup_folder:
            self.backup_folder = Path(backup_folder)
        else:
            self.backup_folder = _load_backup_folder()

        self.backup_folder.mkdir(parents=True, exist_ok=True)
        self.external_backup_folder = self.backup_folder

        self.running = False
        self.thread = None
        self.last_backup_time = None

    # ======================================================
    # إدارة مكان النسخ الاحتياطي
    # ======================================================
    def get_backup_folder(self):
        """يرجع المكان الحالي للنسخ الاحتياطي."""
        return str(self.backup_folder)

    def set_backup_folder(self, new_path):
        """يغيّر مكان النسخ الاحتياطي ويحفظه."""
        try:
            new_path_str = str(new_path or "").strip()
            if not new_path_str:
                return False, "الرجاء إدخال مسار صحيح."

            new_folder = Path(new_path_str).expanduser().resolve()
            new_folder.mkdir(parents=True, exist_ok=True)

            # اختبار الكتابة
            test_file = new_folder / ".write_test"
            try:
                test_file.write_text("ok", encoding="utf-8")
                test_file.unlink()
            except Exception as e:
                return False, f"لا يمكن الكتابة في هذا المسار: {e}"

            # الحفظ
            self.backup_folder = new_folder
            self.external_backup_folder = new_folder
            _save_backup_folder(new_folder)

            return True, f"تم تغيير المكان إلى: {new_folder}"
        except Exception as e:
            return False, f"خطأ: {e}"

    def reset_backup_folder(self):
        """يرجع المكان للافتراضي."""
        try:
            DEFAULT_BACKUP_FOLDER.mkdir(parents=True, exist_ok=True)
            self.backup_folder = DEFAULT_BACKUP_FOLDER
            self.external_backup_folder = DEFAULT_BACKUP_FOLDER
            _save_backup_folder(DEFAULT_BACKUP_FOLDER)
            return True, f"تم استعادة المكان الافتراضي: {DEFAULT_BACKUP_FOLDER}"
        except Exception as e:
            return False, f"خطأ: {e}"

    # ======================================================
    # إنشاء نسخة احتياطية
    # ======================================================
    def create_backup(self, reason="manual"):
        """يصدّر كل الجداول إلى CSV في مجلد مُؤرَّخ."""
        try:
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            folder_name = f"backup_{timestamp}_{reason}"
            backup_folder = self.backup_folder / folder_name
            backup_folder.mkdir(parents=True, exist_ok=True)

            total_rows = 0

            with engine.connect() as conn:
                for table in ALL_TABLES:
                    try:
                        df = pd.read_sql(text(f"SELECT * FROM {table}"), conn)
                        csv_path = backup_folder / f"{table}.csv"
                        df.to_csv(csv_path, index=False, encoding="utf-8-sig")
                        total_rows += len(df)
                    except Exception:
                        continue

            self.last_backup_time = datetime.now()
            self._cleanup_old_backups()

            return True, f"تم إنشاء النسخة: {folder_name} ({total_rows} صف)"
        except Exception as e:
            return False, f"خطأ: {str(e)}"

    def _cleanup_old_backups(self):
        """يحذف النسخ الأقدم من الحد الأقصى."""
        try:
            max_backups = BACKUP_SETTINGS['max_backups']
            folders = sorted(
                [f for f in self.backup_folder.glob("backup_*") if f.is_dir()],
                key=lambda p: p.stat().st_mtime,
                reverse=True,
            )
            for old in folders[max_backups:]:
                shutil.rmtree(old, ignore_errors=True)
        except Exception as e:
            print(f"تنظيف النسخ القديمة: {e}")

    # ======================================================
    # قائمة النسخ
    # ======================================================
    def get_backup_list(self):
        """يرجع قائمة النسخ الاحتياطية المتاحة."""
        backups = []
        try:
            folders = sorted(
                [f for f in self.backup_folder.glob("backup_*") if f.is_dir()],
                key=lambda p: p.stat().st_mtime,
                reverse=True,
            )
            for folder in folders:
                stat = folder.stat()
                total_size = sum(f.stat().st_size for f in folder.glob("*.csv"))
                files_count = len(list(folder.glob("*.csv")))
                backups.append({
                    'name': folder.name,
                    'path': str(folder),
                    'size': total_size,
                    'files_count': files_count,
                    'date': datetime.fromtimestamp(stat.st_mtime),
                    'location': 'محلي',
                })
        except Exception as e:
            print(f"قراءة النسخ: {e}")
        return backups

    # ======================================================
    # استعادة نسخة
    # ======================================================
    def restore_backup(self, backup_path):
        try:
            backup_folder = Path(backup_path)
            if not backup_folder.exists():
                return False, "النسخة غير موجودة!"

            csv_files = list(backup_folder.glob("*.csv"))
            if not csv_files:
                return False, "لا توجد ملفات CSV في هذه النسخة."

            return (
                True,
                f"النسخة موجودة ({len(csv_files)} ملف). "
                "للاستعادة الكاملة، استخدم Neon Console: "
                "https://console.neon.tech → مشروعك → Backups → Restore."
            )
        except Exception as e:
            return False, f"خطأ: {str(e)}"

    # ======================================================
    # حالة النظام
    # ======================================================
    def get_backup_status(self):
        """يعرض حالة النظام الحالية."""
        try:
            backups = self.get_backup_list()
            total_size = sum(b['size'] for b in backups)
            count = len(backups)
            return {
                'enabled': BACKUP_SETTINGS['enabled'],
                'interval_hours': BACKUP_SETTINGS['interval_hours'],
                'max_backups': BACKUP_SETTINGS['max_backups'],
                'last_backup': self.last_backup_time,
                'backups_count': count,
                'total_size': total_size,
                'local_backups_count': count,
                'external_backups_count': count,
            }
        except Exception as e:
            return {
                'enabled': False,
                'interval_hours': 0,
                'max_backups': 0,
                'last_backup': None,
                'backups_count': 0,
                'total_size': 0,
                'local_backups_count': 0,
                'external_backups_count': 0,
                'error': str(e),
            }

    # ======================================================
    # واجهة تغيير مكان النسخ (مع key_suffix فريد)
    # ======================================================
    def render_folder_settings(self, key_suffix="main"):
        """واجهة تغيير مكان النسخ الاحتياطي.

        Args:
            key_suffix: بادئة فريدة لكل استدعاء (لتفادي تكرار الـ keys).
        """
        import streamlit as st

        st.markdown("#### 📁 مكان النسخ الاحتياطي")
        current = self.get_backup_folder()
        st.caption(f"**المكان الحالي:** `{current}`")

        new_path = st.text_input(
            "المكان الجديد (اكتب المسار الكامل):",
            value=current,
            key=f"backup_folder_input_{key_suffix}",
            help=r"مثال: D:\Backups  أو  C:\Users\fady\Documents\Techtoon_Backups",
        )

        col1, col2 = st.columns(2)
        with col1:
            if st.button(
                "✅ تعيين المكان",
                type="primary",
                use_container_width=True,
                key=f"backup_folder_set_{key_suffix}",
            ):
                ok, msg = self.set_backup_folder(new_path)
                if ok:
                    st.success(msg)
                    st.rerun()
                else:
                    st.error(msg)
        with col2:
            if st.button(
                "🔄 استعادة الافتراضي",
                use_container_width=True,
                key=f"backup_folder_reset_{key_suffix}",
            ):
                ok, msg = self.reset_backup_folder()
                if ok:
                    st.success(msg)
                    st.rerun()
                else:
                    st.error(msg)

        st.caption(
            "💡 **ملاحظة:** المكان الجديد لازم يكون موجود أو البرنامج هينشئه. "
            "لو مش عارف المسار، جرّب: `D:\\Backups` أو `C:\\Users\\<اسمك>\\Documents\\Techtoon_Backups`"
        )


# ==========================================================
# نسخة عامة
# ==========================================================
backup_manager = BackupManager()