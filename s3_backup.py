# s3_backup.py
"""
مدير النسخ الاحتياطي السحابي (S3-compatible)
يدعم: AWS S3, Backblaze B2, Supabase, Wasabi, MinIO
"""

import io
import json
import zipfile
from datetime import datetime, timedelta
from typing import Optional

import pandas as pd
from sqlalchemy import text, inspect

try:
    import boto3
    from botocore.exceptions import ClientError, BotoCoreError
    BOTO3_AVAILABLE = True
except ImportError:
    BOTO3_AVAILABLE = False
    boto3 = None
    ClientError = Exception
    BotoCoreError = Exception

from database import engine, SessionLocal
from settings_manager import get_setting, set_setting


# ==========================================
# الإعدادات الافتراضية
# ==========================================
S3_DEFAULTS = {
    "s3_enabled": ("false", "bool", "backup", "تفعيل النسخ السحابي"),
    "s3_endpoint_url": ("", "text", "backup", "Endpoint URL"),
    "s3_region": ("us-east-1", "text", "backup", "المنطقة (Region)"),
    "s3_access_key": ("", "text", "backup", "Access Key ID"),
    "s3_secret_key": ("", "text", "backup", "Secret Access Key"),
    "s3_bucket": ("", "text", "backup", "اسم الـ Bucket"),
    "s3_prefix": ("techtoon-backups/", "text", "backup", "بادئة المسار"),
    "s3_max_backups": ("30", "number", "backup", "أقصى عدد نسخ"),
    "s3_auto_cleanup": ("true", "bool", "backup", "حذف تلقائي"),
    "s3_last_backup_at": ("", "text", "backup", "آخر نسخة ناجحة"),
}


def init_s3_defaults():
    """ينشئ مفاتيح S3 في الإعدادات إن لم تكن موجودة."""
    db = SessionLocal()
    try:
        from models import AppSetting
        existing = {s.key for s in db.query(AppSetting).all()}
        added = 0
        for key, (value, s_type, cat, desc) in S3_DEFAULTS.items():
            if key not in existing:
                db.add(AppSetting(
                    key=key,
                    value=str(value),
                    setting_type=s_type,
                    category=cat,
                    description=desc,
                ))
                added += 1
        if added:
            db.commit()
        return added
    except Exception:
        db.rollback()
        return 0
    finally:
        db.close()


# ==========================================
# الجداول
# ==========================================
def get_all_tables():
    """يرجع أسماء كل الجداول في قاعدة البيانات."""
    insp = inspect(engine)
    tables = insp.get_table_names()
    return [t for t in tables if not t.startswith("sqlite_")]


# ==========================================
# S3 Client
# ==========================================
def _get_s3_client():
    """ينشئ عميل S3 بناءً على الإعدادات."""
    if not BOTO3_AVAILABLE:
        raise RuntimeError("مكتبة boto3 غير مثبتة! شغّل: pip install boto3")

    endpoint = get_setting("s3_endpoint_url", "")
    region = get_setting("s3_region", "us-east-1")
    access_key = get_setting("s3_access_key", "")
    secret_key = get_setting("s3_secret_key", "")

    if not access_key or not secret_key:
        raise ValueError("بيانات S3 غير مكتملة")

    kwargs = {
        "aws_access_key_id": access_key,
        "aws_secret_access_key": secret_key,
        "region_name": region or "us-east-1",
    }
    if endpoint:
        kwargs["endpoint_url"] = endpoint.strip()

    return boto3.client("s3", **kwargs)


def _get_bucket():
    bucket = get_setting("s3_bucket", "").strip()
    if not bucket:
        raise ValueError("اسم الـ Bucket غير محدد")
    return bucket


def _get_prefix():
    prefix = get_setting("s3_prefix", "techtoon-backups/").strip()
    if prefix and not prefix.endswith("/"):
        prefix += "/"
    return prefix


# ==========================================
# اختبار الاتصال
# ==========================================
def test_connection():
    """يفحص الاتصال بـ S3."""
    if not BOTO3_AVAILABLE:
        return False, "❌ مكتبة boto3 غير مثبتة"

    try:
        client = _get_s3_client()
        bucket = _get_bucket()

        client.head_bucket(Bucket=bucket)

        test_key = _get_prefix() + ".connection_test"
        client.put_object(Bucket=bucket, Key=test_key, Body=b"Techtoon test")
        client.head_object(Bucket=bucket, Key=test_key)
        client.delete_object(Bucket=bucket, Key=test_key)

        return True, f"✅ الاتصال ناجح — Bucket: `{bucket}`"
    except ClientError as e:
        code = e.response.get("Error", {}).get("Code", "")
        if code in ("404", "NoSuchBucket"):
            return False, f"❌ الـ Bucket غير موجود: `{_get_bucket()}`"
        elif code == "403":
            return False, "❌ مفتاح الوصول مرفوض"
        else:
            return False, f"❌ خطأ S3: {code or e}"
    except (BotoCoreError, Exception) as e:
        return False, f"❌ خطأ في الاتصال: {str(e)[:200]}"


# ==========================================
# إنشاء ZIP
# ==========================================
def create_backup_zip(reason="manual"):
    """ينشئ ZIP في الذاكرة يحتوي كل الجداول CSV + manifest."""
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f"backup_{timestamp}_{reason}.zip"

    buffer = io.BytesIO()
    tables_info = {}
    total_rows = 0

    with zipfile.ZipFile(buffer, "w", zipfile.ZIP_DEFLATED) as zf:
        with engine.connect() as conn:
            for table in get_all_tables():
                try:
                    df = pd.read_sql(text(f'SELECT * FROM "{table}"'), conn)
                    csv_bytes = df.to_csv(index=False).encode("utf-8-sig")
                    zf.writestr(f"{table}.csv", csv_bytes)
                    tables_info[table] = len(df)
                    total_rows += len(df)
                except Exception as e:
                    tables_info[table] = f"ERROR: {str(e)[:80]}"

        manifest = {
            "created_at": datetime.now().isoformat(),
            "reason": reason,
            "total_rows": total_rows,
            "tables_count": len(tables_info),
            "tables": tables_info,
            "app_version": "1.0",
            "backup_format": "zip-csv-v1",
        }
        zf.writestr(
            "manifest.json",
            json.dumps(manifest, ensure_ascii=False, indent=2).encode("utf-8"),
        )

    buffer.seek(0)
    return filename, buffer.getvalue(), manifest


# ==========================================
# رفع نسخة
# ==========================================
def upload_backup(reason="manual", created_by=None):
    """ينشئ نسخة ويرفعها إلى S3."""
    try:
        filename, zip_bytes, manifest = create_backup_zip(reason=reason)

        client = _get_s3_client()
        bucket = _get_bucket()
        key = _get_prefix() + filename

        size_kb = len(zip_bytes) / 1024

        client.put_object(
            Bucket=bucket,
            Key=key,
            Body=zip_bytes,
            ContentType="application/zip",
            Metadata={
                "created-at": manifest["created_at"],
                "total-rows": str(manifest["total_rows"]),
                "reason": reason,
            },
        )

        set_setting("s3_last_backup_at", datetime.now().isoformat())

        info = {
            "filename": filename,
            "key": key,
            "size_kb": size_kb,
            "total_rows": manifest["total_rows"],
            "tables_count": manifest["tables_count"],
        }

        if get_setting("s3_auto_cleanup", True):
            try:
                cleanup_old_backups()
            except Exception:
                pass

        return True, f"✅ تم رفع النسخة: {filename} ({size_kb:,.1f} KB)", info

    except Exception as e:
        return False, f"❌ فشل الرفع: {str(e)[:300]}", {}


# ==========================================
# قائمة النسخ
# ==========================================
def list_backups():
    """يرجع قائمة بالنسخ السحابية."""
    if not BOTO3_AVAILABLE:
        return []

    try:
        client = _get_s3_client()
        bucket = _get_bucket()
        prefix = _get_prefix()

        response = client.list_objects_v2(Bucket=bucket, Prefix=prefix)

        if "Contents" not in response:
            return []

        backups = []
        for obj in response["Contents"]:
            key = obj["Key"]
            if key.endswith(".zip"):
                filename = key.replace(prefix, "")
                backups.append({
                    "key": key,
                    "filename": filename,
                    "size": obj["Size"],
                    "size_kb": obj["Size"] / 1024,
                    "last_modified": obj["LastModified"],
                })

        backups.sort(key=lambda x: x["last_modified"], reverse=True)
        return backups

    except Exception as e:
        print(f"⚠️ خطأ في قائمة النسخ: {e}")
        return []


# ==========================================
# تنزيل نسخة
# ==========================================
def download_backup(key):
    """ينزّل نسخة من S3."""
    try:
        client = _get_s3_client()
        bucket = _get_bucket()
        response = client.get_object(Bucket=bucket, Key=key)
        data = response["Body"].read()
        return True, "✅ تم التنزيل", data
    except Exception as e:
        return False, f"❌ فشل التنزيل: {str(e)[:200]}", b""


# ==========================================
# حذف نسخة
# ==========================================
def delete_backup(key):
    """يحذف نسخة من S3."""
    try:
        client = _get_s3_client()
        bucket = _get_bucket()
        client.delete_object(Bucket=bucket, Key=key)
        return True, f"✅ تم حذف: {key.split('/')[-1]}"
    except Exception as e:
        return False, f"❌ فشل الحذف: {str(e)[:200]}"


# ==========================================
# تنظيف
# ==========================================
def cleanup_old_backups():
    """يحذف النسخ الأقدم من الحد المسموح."""
    try:
        max_backups = int(get_setting("s3_max_backups", 30))
        backups = list_backups()

        if len(backups) <= max_backups:
            return 0, "لا حاجة للتنظيف"

        to_delete = backups[max_backups:]
        deleted = 0
        for b in to_delete:
            ok, _ = delete_backup(b["key"])
            if ok:
                deleted += 1

        return deleted, f"✅ تم حذف {deleted} نسخة قديمة"
    except Exception as e:
        return 0, f"⚠️ خطأ في التنظيف: {str(e)[:150]}"


# ==========================================
# حالة S3
# ==========================================
def get_s3_status():
    """يرجع حالة S3 الكاملة."""
    return {
        "boto3_available": BOTO3_AVAILABLE,
        "enabled": get_setting("s3_enabled", False),
        "bucket": get_setting("s3_bucket", ""),
        "endpoint": get_setting("s3_endpoint_url", ""),
        "region": get_setting("s3_region", ""),
        "prefix": get_setting("s3_prefix", ""),
        "max_backups": int(get_setting("s3_max_backups", 30)),
        "auto_cleanup": get_setting("s3_auto_cleanup", True),
        "last_backup_at": get_setting("s3_last_backup_at", ""),
        "credentials_set": bool(
            get_setting("s3_access_key", "") and
            get_setting("s3_secret_key", "")
        ),
    }


# ==========================================
# استعادة
# ==========================================
def restore_from_zip(zip_bytes, tables=None, dry_run=False):
    """يستعيد البيانات من ZIP."""
    report = {
        "tables_restored": [],
        "tables_skipped": [],
        "rows_restored": 0,
        "errors": [],
        "dry_run": dry_run,
    }

    try:
        with zipfile.ZipFile(io.BytesIO(zip_bytes), "r") as zf:
            csv_files = [n for n in zf.namelist() if n.endswith(".csv")]
            existing_tables = set(get_all_tables())

            for csv_name in csv_files:
                table_name = csv_name.replace(".csv", "")

                if tables and table_name not in tables:
                    report["tables_skipped"].append(table_name)
                    continue

                if table_name not in existing_tables:
                    report["errors"].append(f"الجدول {table_name} غير موجود")
                    continue

                try:
                    df = pd.read_csv(io.BytesIO(zf.read(csv_name)))

                    if dry_run:
                        report["tables_restored"].append(table_name)
                        report["rows_restored"] += len(df)
                        continue

                    with engine.connect() as conn:
                        df.to_sql(
                            table_name,
                            conn,
                            if_exists="append",
                            index=False,
                            method="multi",
                        )

                    report["tables_restored"].append(table_name)
                    report["rows_restored"] += len(df)

                except Exception as e:
                    report["errors"].append(f"{table_name}: {str(e)[:100]}")

        return report

    except Exception as e:
        report["errors"].append(f"فشل فك الـ ZIP: {str(e)[:200]}")
        return report

# ═══════════════════════════════════════════════════
#  تصدير كل الجداول إلى ZIP حقيقي (نسخة احتياطية)
# ═══════════════════════════════════════════════════
def export_all_tables_to_zip() -> bytes:
    """تصدير كل جداول قاعدة البيانات كـ CSV داخل ZIP."""
    from database import SessionLocal
    import models

    buf = io.BytesIO()
    db = SessionLocal()  # Neon - المصدر الرسمي

    try:
        with zipfile.ZipFile(buf, "w", zipfile.ZIP_DEFLATED) as zf:
            model_names = [
                "User", "Account", "Currency", "AccountingPeriod",
                "Party", "CashBox", "Category", "Item", "KitComponent",
                "Invoice", "InvoiceLine", "CostHistory",
                "JournalEntry", "JournalLine",
                "Payment", "CashTransfer", "ExpenseCategory", "Expense",
                "Warehouse", "StockLevel", "WarehouseTransfer", "StockCount",
                "FixedAsset", "DepreciationRecord",
                "Loan", "LoanInstallment",
                "Employee", "SalaryRecord",
                "Budget", "CostCenter", "CostAllocation", "CostCenterTransaction",
                "AppSetting",
            ]

            exported = 0
            total_rows = 0
            errors = []

            for name in model_names:
                model = getattr(models, name, None)
                if model is None:
                    continue
                try:
                    items = db.query(model).all()
                    if not items:
                        continue
                    cols = [c.name for c in model.__table__.columns]
                    data = [{c: getattr(item, c, None) for c in cols} for item in items]
                    df = pd.DataFrame(data)
                    csv_text = df.to_csv(index=False)
                    zf.writestr(f"{model.__tablename__}.csv", csv_text)
                    exported += 1
                    total_rows += len(data)
                except Exception as e:
                    errors.append(f"{name}: {e}")

            info = (
                f"Techtoon Accounting Backup\n"
                f"Created: {datetime.now().isoformat()}\n"
                f"Tables: {exported}\n"
                f"Rows: {total_rows}\n"
                f"Errors: {len(errors)}\n"
            )
            if errors:
                info += "\nErrors:\n" + "\n".join(errors)
            zf.writestr("_backup_info.txt", info)

        buf.seek(0)
        return buf.getvalue()
    finally:
        db.close()
