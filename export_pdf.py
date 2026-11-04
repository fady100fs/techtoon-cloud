# export_pdf.py
"""
تصدير الفواتير إلى PDF — نسخة محدّثة
- يستخدم pdf_reports.py الجديد
- يتكامل مع الإعدادات المركزية
- يحافظ على التوافق مع الكود القديم
"""

from datetime import datetime
from pathlib import Path

from database import SessionLocal
import models
from pdf_reports import build_invoice_pdf, ar


def export_invoice_to_pdf(invoice_id: int, output_path: str = None) -> str:
    """تصدير فاتورة إلى PDF احترافي.

    Args:
        invoice_id: معرف الفاتورة
        output_path: مسار الملف (اختياري)

    Returns:
        مسار الملف الناتج
    """
    db = SessionLocal()
    try:
        invoice = db.query(models.Invoice).filter(
            models.Invoice.id == invoice_id
        ).first()
        if not invoice:
            raise ValueError("الفاتورة غير موجودة!")

        party = db.query(models.Party).filter(
            models.Party.id == invoice.party_id
        ).first()

        # جلب الأسطر
        lines_db = db.query(models.InvoiceLine).filter(
            models.InvoiceLine.invoice_id == invoice.id
        ).all()

        # خريطة الأصناف
        item_ids = [l.item_id for l in lines_db]
        items_map = {}
        if item_ids:
            items_map = {
                it.id: it for it in db.query(models.Item).filter(
                    models.Item.id.in_(item_ids)
                ).all()
            }

        lines = []
        for l in lines_db:
            item = items_map.get(l.item_id)
            lines.append({
                "item_name": item.name if item else "—",
                "quantity": float(l.quantity or 0),
                "price": float(l.price or 0),
                "total": float(l.total or 0),
            })

        subtotal = sum(l["total"] for l in lines)
        discount = float(invoice.discount_amount or 0)
        tax = float(invoice.tax_amount or 0)
        net_amount = float(invoice.net_amount or 0)

        # بيانات التقرير
        invoice_data = {
            "invoice_number": invoice.invoice_number,
            "invoice_date": invoice.date.strftime("%Y-%m-%d") if invoice.date else "—",
            "invoice_type": invoice.type,
            "party_name": party.name if party else "—",
            "party_phone": party.phone if party else "",
            "party_address": party.address if party else "",
            "lines": lines,
            "subtotal": subtotal,
            "discount": discount,
            "tax": tax,
            "net_amount": net_amount,
            "status": invoice.status,
        }

        # مسار الملف
        if not output_path:
            output_path = f"Invoice_{invoice.invoice_number}.pdf"

        build_invoice_pdf(invoice_data, output_path)
        print(f"✅ تم تصدير الفاتورة إلى: {output_path}")
        return output_path

    finally:
        db.close()