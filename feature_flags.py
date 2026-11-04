"""
feature_flags.py — مفاتيح تشغيل الميزات لكل عميل
يمكن التحكم بها من st.secrets أو من القيمة الافتراضية
"""

import streamlit as st


def is_enabled(flag: str, default: bool = True) -> bool:
    """تحقق من تفعيل ميزة معينة."""
    try:
        return bool(st.secrets.get(flag, default))
    except Exception:
        return default


# ============ اختصارات الميزات ============
def s3_backup_enabled() -> bool:
    return is_enabled("ENABLE_S3_BACKUP", True)


def barcode_enabled() -> bool:
    return is_enabled("ENABLE_BARCODE", True)


def pdf_reports_enabled() -> bool:
    return is_enabled("ENABLE_PDF_REPORTS", True)


def recurring_invoices_enabled() -> bool:
    return is_enabled("ENABLE_RECURRING_INVOICES", False)


def fixed_assets_enabled() -> bool:
    return is_enabled("ENABLE_FIXED_ASSETS", True)


def loans_enabled() -> bool:
    return is_enabled("ENABLE_LOANS", True)


def payroll_enabled() -> bool:
    return is_enabled("ENABLE_PAYROLL", True)


def mobile_hint_enabled() -> bool:
    return is_enabled("ENABLE_MOBILE_HINT", True)


def show_feature_status():
    """عرض حالة الميزات في الشريط الجانبي (للمطور فقط)."""
    with st.sidebar.expander("⚙️ حالة الميزات"):
        flags = {
            "النسخ السحابي": s3_backup_enabled(),
            "قارئ الباركود": barcode_enabled(),
            "تقارير PDF": pdf_reports_enabled(),
            "الفواتير المتكررة": recurring_invoices_enabled(),
            "الأصول الثابتة": fixed_assets_enabled(),
            "القروض": loans_enabled(),
            "الرواتب": payroll_enabled(),
        }
        for name, state in flags.items():
            icon = "✅" if state else "❌"
            st.write(f"{icon} {name}")