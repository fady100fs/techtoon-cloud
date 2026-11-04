# pdf_reports.py
"""
مولّد تقارير PDF احترافي بالعربية
"""

import base64
import io
import os
import platform
from datetime import datetime
from pathlib import Path
from typing import Optional

from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import cm, mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    SimpleDocTemplate, Table, TableStyle, Paragraph, Spacer, Image,
)
from reportlab.lib.enums import TA_CENTER, TA_RIGHT, TA_LEFT

try:
    import arabic_reshaper
    from bidi.algorithm import get_display
    ARABIC_SUPPORT = True
except ImportError:
    ARABIC_SUPPORT = False

from settings_manager import get_company_info, get_financial_defaults


_FONTS_DIR = Path(__file__).parent / "fonts"
_REGISTERED_FONT = None
_FONT_NAME = "ArabicFont"


def _find_font_file():
    candidates = [
        _FONTS_DIR / "Amiri-Regular.ttf",
        _FONTS_DIR / "Cairo-Regular.ttf",
        _FONTS_DIR / "NotoSansArabic-Regular.ttf",
        _FONTS_DIR / "arial.ttf",
        _FONTS_DIR / "tahoma.ttf",
    ]
    for c in candidates:
        if c.exists():
            return str(c)

    system = platform.system()
    if system == "Windows":
        for f in [r"C:\Windows\Fonts\arial.ttf", r"C:\Windows\Fonts\tahoma.ttf"]:
            if os.path.exists(f):
                return f
    elif system == "Linux":
        for f in [
            "/usr/share/fonts/truetype/noto/NotoSansArabic-Regular.ttf",
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        ]:
            if os.path.exists(f):
                return f
    return None


def _register_font():
    global _REGISTERED_FONT
    if _REGISTERED_FONT is not None:
        return _REGISTERED_FONT
    font_path = _find_font_file()
    if not font_path:
        _REGISTERED_FONT = "Helvetica"
        return _REGISTERED_FONT
    try:
        pdfmetrics.registerFont(TTFont(_FONT_NAME, font_path))
        _REGISTERED_FONT = _FONT_NAME
    except Exception:
        _REGISTERED_FONT = "Helvetica"
    return _REGISTERED_FONT


def ar(text):
    """يعالج النص العربي."""
    if text is None:
        return ""
    text = str(text)
    if not text.strip():
        return ""
    if not ARABIC_SUPPORT:
        return text
    try:
        reshaped = arabic_reshaper.reshape(text)
        return get_display(reshaped)
    except Exception:
        return text


def _get_styles():
    font = _register_font()
    company = get_company_info()
    primary = colors.HexColor(company.get("primary_color", "#2563eb"))
    styles = getSampleStyleSheet()

    return {
        "title": ParagraphStyle("T", parent=styles["Heading1"], fontName=font,
                                 fontSize=22, textColor=primary, alignment=TA_CENTER,
                                 spaceAfter=8, leading=28),
        "subtitle": ParagraphStyle("ST", parent=styles["Heading2"], fontName=font,
                                    fontSize=14, textColor=colors.HexColor("#444"),
                                    alignment=TA_CENTER, spaceAfter=6),
        "section": ParagraphStyle("S", parent=styles["Heading3"], fontName=font,
                                   fontSize=13, textColor=primary, alignment=TA_RIGHT,
                                   spaceBefore=12, spaceAfter=8),
        "body": ParagraphStyle("B", parent=styles["Normal"], fontName=font,
                                fontSize=10, alignment=TA_RIGHT, leading=15, spaceAfter=4),
        "body_ltr": ParagraphStyle("BL", parent=styles["Normal"], fontName=font,
                                    fontSize=10, alignment=TA_LEFT, leading=15),
        "small": ParagraphStyle("SM", parent=styles["Normal"], fontName=font,
                                 fontSize=8, textColor=colors.grey, alignment=TA_CENTER),
        "font": font,
        "primary": primary,
    }


def _build_header(styles, document_title, extra_info=None):
    company = get_company_info()
    font = styles["font"]
    primary = styles["primary"]
    elements = []

    logo_data = company.get("logo_base64", "")
    logo_image = None
    if logo_data:
        try:
            logo_bytes = base64.b64decode(logo_data)
            logo_image = Image(io.BytesIO(logo_bytes), width=3*cm, height=3*cm)
        except Exception:
            logo_image = None

    info_lines = [
        Paragraph(ar(company["name"]), ParagraphStyle(
            "CN", fontName=font, fontSize=16,
            textColor=primary, alignment=TA_RIGHT, leading=20,
        )),
    ]
    if company.get("tagline"):
        info_lines.append(Paragraph(ar(company["tagline"]), styles["small"]))

    contact = []
    if company.get("phone"):
        contact.append(f"Tel: {company['phone']}")
    if company.get("email"):
        contact.append(f"Email: {company['email']}")
    if company.get("tax_id"):
        contact.append(f"Tax: {company['tax_id']}")
    if contact:
        info_lines.append(Paragraph(ar(" | ".join(contact)), styles["small"]))

    if company.get("address"):
        info_lines.append(Paragraph(ar(company["address"]), styles["small"]))

    if logo_image:
        header_data = [[logo_image, info_lines]]
        col_widths = [3.5*cm, 13.5*cm]
    else:
        header_data = [[info_lines]]
        col_widths = [17*cm]

    header_table = Table(header_data, colWidths=col_widths)
    header_table.setStyle(TableStyle([
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ("ALIGN", (0, 0), (-1, -1), "RIGHT"),
    ]))
    elements.append(header_table)

    sep = Table([[""]], colWidths=[17*cm], rowHeights=[3])
    sep.setStyle(TableStyle([("BACKGROUND", (0, 0), (-1, -1), primary)]))
    elements.append(Spacer(1, 4))
    elements.append(sep)
    elements.append(Spacer(1, 12))

    elements.append(Paragraph(ar(document_title), styles["title"]))

    if extra_info:
        parts = [f"<b>{ar(k)}:</b> {ar(v)}" for k, v in extra_info.items() if v]
        if parts:
            elements.append(Paragraph(" • ".join(parts), styles["body"]))

    elements.append(Spacer(1, 14))
    return elements


def _build_footer(styles):
    company = get_company_info()
    return [
        Spacer(1, 20),
        Paragraph(
            ar(f"{company['name']} — {datetime.now().strftime('%Y-%m-%d %H:%M')}"),
            styles["small"],
        ),
    ]


def build_invoice_pdf(invoice_data, output_path):
    """يبني PDF لفاتورة."""
    styles = _get_styles()
    font = styles["font"]
    primary = styles["primary"]

    inv_type_label = "فاتورة مبيعات" if invoice_data.get("invoice_type") == "sale" else "فاتورة مشتريات"

    extra = {
        "رقم": invoice_data.get("invoice_number", "—"),
        "التاريخ": str(invoice_data.get("invoice_date", "—"))[:10],
        "الحالة": invoice_data.get("status", "—"),
    }
    elements = _build_header(styles, inv_type_label, extra)

    party_label = "العميل" if invoice_data.get("invoice_type") == "sale" else "المورد"
    info = f"<b>{ar(party_label)}:</b> {ar(invoice_data.get('party_name', '—'))}"
    if invoice_data.get("party_phone"):
        info += f" | <b>{ar('هاتف')}:</b> {ar(invoice_data['party_phone'])}"
    elements.append(Paragraph(info, styles["body"]))
    elements.append(Spacer(1, 14))

    lines = invoice_data.get("lines", [])
    if lines:
        headers = ["#", "الصنف", "الكمية", "السعر", "الإجمالي"]
        data = [[Paragraph(ar(h), styles["body_ltr"]) for h in headers]]
        for idx, line in enumerate(lines, 1):
            data.append([
                Paragraph(str(idx), styles["body_ltr"]),
                Paragraph(ar(line.get("item_name", "—")), styles["body"]),
                Paragraph(f"{float(line.get('quantity', 0)):,.2f}", styles["body_ltr"]),
                Paragraph(f"{float(line.get('price', 0)):,.2f}", styles["body_ltr"]),
                Paragraph(f"{float(line.get('total', 0)):,.2f}", styles["body_ltr"]),
            ])

        table = Table(data, colWidths=[1*cm, 7*cm, 2.5*cm, 3*cm, 3.5*cm], repeatRows=1)
        table.setStyle(TableStyle([
            ("BACKGROUND", (0, 0), (-1, 0), primary),
            ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
            ("FONTNAME", (0, 0), (-1, -1), font),
            ("FONTSIZE", (0, 0), (-1, 0), 10),
            ("FONTSIZE", (0, 1), (-1, -1), 9),
            ("ALIGN", (0, 0), (0, -1), "CENTER"),
            ("ALIGN", (1, 0), (1, -1), "RIGHT"),
            ("ALIGN", (2, 0), (-1, -1), "CENTER"),
            ("GRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#e0e0e0")),
            ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#f8f9fa")]),
            ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ]))
        elements.append(table)

    elements.append(Spacer(1, 14))
    fin_data = [
        [Paragraph(ar("المجموع الفرعي"), styles["body"]),
         Paragraph(f"{float(invoice_data.get('subtotal', 0)):,.2f}", styles["body_ltr"])],
    ]
    if invoice_data.get("discount", 0) > 0:
        fin_data.append([
            Paragraph(ar("الخصم"), styles["body"]),
            Paragraph(f"- {float(invoice_data['discount']):,.2f}", styles["body_ltr"]),
        ])
    if invoice_data.get("tax", 0) > 0:
        fin_data.append([
            Paragraph(ar("الضريبة"), styles["body"]),
            Paragraph(f"+ {float(invoice_data['tax']):,.2f}", styles["body_ltr"]),
        ])
    fin_data.append([
        Paragraph(f"<b>{ar('الصافي')}</b>", styles["body"]),
        Paragraph(f"<b>{float(invoice_data.get('net_amount', 0)):,.2f}</b>", styles["body_ltr"]),
    ])

    summary = Table(fin_data, colWidths=[13*cm, 4*cm])
    summary.setStyle(TableStyle([
        ("ALIGN", (0, 0), (0, -1), "RIGHT"),
        ("ALIGN", (1, 0), (1, -1), "LEFT"),
        ("FONTNAME", (0, 0), (-1, -1), font),
        ("FONTSIZE", (0, -1), (-1, -1), 12),
        ("TEXTCOLOR", (0, -1), (-1, -1), primary),
        ("LINEABOVE", (0, -1), (-1, -1), 1.5, primary),
        ("TOPPADDING", (0, -1), (-1, -1), 8),
    ]))
    elements.append(summary)

    try:
        from settings_manager import get_setting
        footer = get_setting("invoice_footer_text", "")
        terms = get_setting("invoice_terms", "")
    except Exception:
        footer, terms = "", ""

    if terms:
        elements.append(Spacer(1, 18))
        elements.append(Paragraph(ar("الشروط والأحكام:"), styles["section"]))
        elements.append(Paragraph(ar(terms), styles["body"]))

    if footer:
        elements.append(Spacer(1, 18))
        elements.append(Paragraph(ar(footer), styles["small"]))

    elements.extend(_build_footer(styles))
    _build_pdf(output_path, elements)
    return output_path


def build_statement_pdf(statement_data, output_path):
    """يبني PDF لكشف حساب."""
    styles = _get_styles()
    font = styles["font"]
    primary = styles["primary"]

    party_type_label = "كشف حساب عميل" if statement_data.get("party_type") == "customer" else "كشف حساب مورد"
    extra = {
        "من": str(statement_data.get("from_date", "—"))[:10],
        "إلى": str(statement_data.get("to_date", "—"))[:10],
    }
    elements = _build_header(styles, party_type_label, extra)

    info = f"<b>{ar('الاسم')}:</b> {ar(statement_data.get('party_name', '—'))}"
    if statement_data.get("party_phone"):
        info += f" | <b>{ar('هاتف')}:</b> {ar(statement_data['party_phone'])}"
    elements.append(Paragraph(info, styles["body"]))
    elements.append(Spacer(1, 14))

    rows = statement_data.get("rows", [])
    if rows:
        headers = ["التاريخ", "البيان", "مدين", "دائن", "الرصيد"]
        data = [[Paragraph(ar(h), styles["body_ltr"]) for h in headers]]
        for r in rows:
            data.append([
                Paragraph(str(r.get("date", "—"))[:10], styles["body_ltr"]),
                Paragraph(ar(r.get("description", "—")), styles["body"]),
                Paragraph(f"{float(r.get('debit', 0)):,.2f}" if r.get("debit") else "—", styles["body_ltr"]),
                Paragraph(f"{float(r.get('credit', 0)):,.2f}" if r.get("credit") else "—", styles["body_ltr"]),
                Paragraph(f"{float(r.get('balance', 0)):,.2f}", styles["body_ltr"]),
            ])

        data.append([
            Paragraph(f"<b>{ar('الإجمالي')}</b>", styles["body"]),
            Paragraph("", styles["body"]),
            Paragraph(f"<b>{float(statement_data.get('total_debit', 0)):,.2f}</b>", styles["body_ltr"]),
            Paragraph(f"<b>{float(statement_data.get('total_credit', 0)):,.2f}</b>", styles["body_ltr"]),
            Paragraph(f"<b>{float(statement_data.get('running_balance', 0)):,.2f}</b>", styles["body_ltr"]),
        ])

        table = Table(data, colWidths=[2.5*cm, 6*cm, 2.8*cm, 2.8*cm, 2.9*cm], repeatRows=1)
        table.setStyle(TableStyle([
            ("BACKGROUND", (0, 0), (-1, 0), primary),
            ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
            ("FONTNAME", (0, 0), (-1, -1), font),
            ("FONTSIZE", (0, 0), (-1, 0), 10),
            ("FONTSIZE", (0, 1), (-1, -1), 9),
            ("ALIGN", (0, 0), (-1, 0), "CENTER"),
            ("GRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#e0e0e0")),
            ("ROWBACKGROUNDS", (0, 1), (-1, -2), [colors.white, colors.HexColor("#f8f9fa")]),
            ("BACKGROUND", (0, -1), (-1, -1), colors.HexColor("#e8f4f8")),
            ("TEXTCOLOR", (0, -1), (-1, -1), primary),
            ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ]))
        elements.append(table)
    else:
        elements.append(Paragraph(ar("لا توجد حركات"), styles["body"]))

    elements.append(Spacer(1, 14))
    balance = float(statement_data.get("running_balance", 0))
    label = "له" if balance > 0 else "عليه"
    elements.append(Paragraph(
        f"<b>{ar('الرصيد النهائي')}:</b> {abs(balance):,.2f} ج.م ({ar(label)})",
        styles["body"],
    ))

    elements.extend(_build_footer(styles))
    _build_pdf(output_path, elements)
    return output_path


def build_balance_sheet_pdf(balance_data, output_path):
    """يبني PDF للميزانية العمومية."""
    styles = _get_styles()
    font = styles["font"]
    primary = styles["primary"]

    extra = {"التاريخ": datetime.now().strftime("%Y-%m-%d")}
    elements = _build_header(styles, "الميزانية العمومية", extra)

    def _table(accounts_dict, title, total, color):
        header = Paragraph(ar(title), ParagraphStyle(
            "SecT", fontName=font, fontSize=12, textColor=color,
            alignment=TA_RIGHT, spaceAfter=6,
        ))
        rows = [[header, ""]]
        for name, amount in accounts_dict.items():
            rows.append([
                Paragraph(ar(name), styles["body"]),
                Paragraph(f"{float(amount):,.2f}", styles["body_ltr"]),
            ])
        rows.append([
            Paragraph(f"<b>{ar('الإجمالي')}</b>", styles["body"]),
            Paragraph(f"<b>{float(total):,.2f}</b>", styles["body_ltr"]),
        ])
        t = Table(rows, colWidths=[12*cm, 5*cm])
        t.setStyle(TableStyle([
            ("FONTNAME", (0, 0), (-1, -1), font),
            ("SPAN", (0, 0), (1, 0)),
            ("BACKGROUND", (0, 0), (-1, 0), color),
            ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
            ("BACKGROUND", (0, -1), (-1, -1), colors.HexColor("#f0f0f0")),
            ("LINEABOVE", (0, -1), (-1, -1), 1, color),
            ("ALIGN", (0, 0), (0, -1), "RIGHT"),
            ("ALIGN", (1, 0), (1, -1), "LEFT"),
            ("TOPPADDING", (0, 0), (-1, -1), 6),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
        ]))
        return t

    elements.append(_table(balance_data.get("assets", {}), "الأصول",
                            balance_data.get("total_assets", 0),
                            colors.HexColor("#10b981")))
    elements.append(Spacer(1, 14))
    elements.append(_table(balance_data.get("liabilities", {}), "الخصوم",
                            balance_data.get("total_liabilities", 0),
                            colors.HexColor("#ef4444")))
    elements.append(Spacer(1, 14))
    elements.append(_table(balance_data.get("equity", {}), "حقوق الملكية",
                            balance_data.get("total_equity", 0),
                            colors.HexColor("#3b82f6")))

    elements.append(Spacer(1, 18))
    assets = float(balance_data.get("total_assets", 0))
    liab_eq = float(balance_data.get("total_liabilities", 0)) + float(balance_data.get("total_equity", 0))
    diff = assets - liab_eq
    if abs(diff) < 0.01:
        msg, color = f"OK - {ar('الميزانية متوازنة')}", colors.HexColor("#10b981")
    else:
        msg, color = f"WARN - {ar('فرق')}: {diff:,.2f}", colors.HexColor("#ef4444")

    elements.append(Paragraph(msg, ParagraphStyle(
        "BM", fontName=font, fontSize=11, textColor=color, alignment=TA_CENTER,
    )))

    elements.extend(_build_footer(styles))
    _build_pdf(output_path, elements)
    return output_path


def build_profit_loss_pdf(pl_data, output_path):
    """يبني PDF لتقرير أرباح وخسائر."""
    styles = _get_styles()
    font = styles["font"]

    extra = {"التاريخ": datetime.now().strftime("%Y-%m-%d")}
    elements = _build_header(styles, "تقرير الأرباح والخسائر", extra)

    def _row(label, value, bold=False, is_total=False):
        label_text = f"<b>{ar(label)}</b>" if (bold or is_total) else ar(label)
        value_text = f"<b>{float(value):,.2f}</b>" if (bold or is_total) else f"{float(value):,.2f}"
        return [Paragraph(label_text, styles["body"]),
                Paragraph(value_text, styles["body_ltr"])]

    rows = [
        _row("الإيرادات", pl_data.get("revenue", 0), bold=True),
        _row("تكلفة البضاعة المباعة", pl_data.get("cogs", 0)),
        _row("الربح الإجمالي", pl_data.get("gross_profit", 0), is_total=True),
        _row("المصروفات التشغيلية", pl_data.get("operating_expenses", 0)),
        _row("مصروفات الرواتب", pl_data.get("salary_expenses", 0)),
        _row("مصروفات الإهلاك", pl_data.get("depreciation_expenses", 0)),
        _row("إجمالي المصروفات", pl_data.get("total_expenses", 0), bold=True),
        _row("صافي الربح/الخسارة", pl_data.get("net_profit", 0), is_total=True),
    ]

    table = Table(rows, colWidths=[12*cm, 5*cm])
    table.setStyle(TableStyle([
        ("FONTNAME", (0, 0), (-1, -1), font),
        ("FONTSIZE", (0, 0), (-1, -1), 11),
        ("ALIGN", (0, 0), (0, -1), "RIGHT"),
        ("ALIGN", (1, 0), (1, -1), "LEFT"),
        ("TOPPADDING", (0, 0), (-1, -1), 8),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 8),
        ("LINEBELOW", (0, 0), (-1, -2), 0.5, colors.HexColor("#e0e0e0")),
        ("BACKGROUND", (0, 2), (-1, 2), colors.HexColor("#f0f9ff")),
        ("BACKGROUND", (0, 6), (-1, 6), colors.HexColor("#fef2f2")),
        ("BACKGROUND", (0, 7), (-1, 7),
         colors.HexColor("#d1fae5") if pl_data.get("net_profit", 0) > 0 else colors.HexColor("#fee2e2")),
    ]))
    elements.append(table)

    elements.append(Spacer(1, 18))
    elements.append(Paragraph(
        f"<b>{ar('هامش الربح الإجمالي')}:</b> {pl_data.get('gross_margin', 0):.1f}% | "
        f"<b>{ar('هامش الربح الصافي')}:</b> {pl_data.get('net_margin', 0):.1f}%",
        styles["body"],
    ))

    elements.extend(_build_footer(styles))
    _build_pdf(output_path, elements)
    return output_path


def _build_pdf(output_path, elements):
    doc = SimpleDocTemplate(
        output_path, pagesize=A4,
        rightMargin=1.5*cm, leftMargin=1.5*cm,
        topMargin=1.5*cm, bottomMargin=1.5*cm,
    )
    doc.build(elements)


def test_pdf_generation():
    """يولّد PDF تجريبي."""
    return {
        "font_found": _find_font_file() is not None,
        "font_path": _find_font_file(),
        "arabic_support": ARABIC_SUPPORT,
        "registered_font": _register_font(),
    }