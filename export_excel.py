# export_excel.py
"""
أدوات التصدير إلى Excel
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
- export_balance_sheet_to_excel  → الميزانية العمومية (صفحة 12)
- export_profit_loss_to_excel    → الأرباح والخسائر (صفحة 5)
- export_inventory_to_excel      → تقرير المخزون (صفحة 5)
- export_statement_to_excel      → كشف الحساب (صفحة 10)

⭐ الإصلاح: كان الملف بلا استيراد pandas وكانت دالتان ناقصتان
   فكان كل تصدير من الصفحات يفشل برسالة "خطأ في التصدير".
"""
from datetime import datetime

import pandas as pd


def _default_path(prefix):
    return f"{prefix}_{datetime.now().strftime('%Y%m%d_%H%M%S')}.xlsx"


def _add_title(ws, title, title_format):
    ws.write(0, 0, title, title_format)


# ═══════════════════════════════════════════════════════════
#  الميزانية العمومية
# ═══════════════════════════════════════════════════════════
def export_balance_sheet_to_excel(
    assets_data: list,
    liabilities_data: list,
    equity_data: list,
    total_assets: float,
    total_liabilities: float,
    total_equity: float,
    net_profit: float,
    output_path: str = None
):
    """تصدير الميزانية العمومية إلى Excel"""
    if not output_path:
        output_path = _default_path("Balance_Sheet")

    with pd.ExcelWriter(output_path, engine='xlsxwriter') as writer:
        workbook = writer.book

        title_format = workbook.add_format({
            'bold': True,
            'font_size': 16,
            'align': 'center',
            'bg_color': '#1F4E78',
            'font_color': 'white'
        })

        total_format = workbook.add_format({
            'bold': True,
            'bg_color': '#D9E1F2',
            'border': 1,
            'font_size': 12
        })

        # الأصول
        assets_df = pd.DataFrame(assets_data)
        assets_df.to_excel(writer, sheet_name='الأصول', index=False, startrow=2)
        worksheet_assets = writer.sheets['الأصول']
        _add_title(worksheet_assets, "الأصول (Assets)", title_format)

        for i in range(len(assets_df.columns)):
            worksheet_assets.set_column(i, i, 20)

        worksheet_assets.write(len(assets_df) + 3, 0, "إجمالي الأصول:", total_format)
        worksheet_assets.write(len(assets_df) + 3, 3, total_assets, total_format)

        # الخصوم
        liabilities_df = pd.DataFrame(liabilities_data)
        liabilities_df.to_excel(writer, sheet_name='الخصوم', index=False, startrow=2)
        worksheet_liabilities = writer.sheets['الخصوم']
        _add_title(worksheet_liabilities, "الخصوم (Liabilities)", title_format)

        for i in range(len(liabilities_df.columns)):
            worksheet_liabilities.set_column(i, i, 20)

        worksheet_liabilities.write(len(liabilities_df) + 3, 0, "إجمالي الخصوم:", total_format)
        worksheet_liabilities.write(len(liabilities_df) + 3, 3, total_liabilities, total_format)

        # حقوق الملكية
        equity_df = pd.DataFrame(equity_data)
        equity_df.to_excel(writer, sheet_name='حقوق الملكية', index=False, startrow=2)
        worksheet_equity = writer.sheets['حقوق الملكية']
        _add_title(worksheet_equity, "حقوق الملكية (Equity)", title_format)

        for i in range(len(equity_df.columns)):
            worksheet_equity.set_column(i, i, 20)

        worksheet_equity.write(len(equity_df) + 3, 0, "إجمالي حقوق الملكية:", total_format)
        worksheet_equity.write(len(equity_df) + 3, 3, total_equity, total_format)

        # ملخص
        summary_df = pd.DataFrame({
            'البند': ['إجمالي الأصول', 'إجمالي الخصوم', 'إجمالي حقوق الملكية', 'صافي الربح/الخسارة', 'الخصوم + حقوق الملكية'],
            'المبلغ': [total_assets, total_liabilities, total_equity, net_profit, total_liabilities + total_equity]
        })
        summary_df.to_excel(writer, sheet_name='الملخص', index=False, startrow=2)
        worksheet_summary = writer.sheets['الملخص']
        _add_title(worksheet_summary, "ملخص الميزانية العمومية", title_format)

        for i in range(len(summary_df.columns)):
            worksheet_summary.set_column(i, i, 25)

    print(f"✅ تم تصدير الميزانية العمومية إلى: {output_path}")
    return output_path


# ═══════════════════════════════════════════════════════════
#  الأرباح والخسائر (⭐ دالة كانت ناقصة — تستدعيها صفحة 5)
# ═══════════════════════════════════════════════════════════
def export_profit_loss_to_excel(revenues=None, expenses=None, net_profit=None, output_path=None):
    """تصدير تقرير الأرباح والخسائر — إن لم تُمرَّر القيم تُحسب من القيود المحاسبية."""
    if revenues is None or expenses is None:
        from database import SessionLocal
        import models
        from sqlalchemy import func

        db = SessionLocal()
        try:
            if revenues is None:
                revenues = db.query(func.sum(models.JournalLine.credit)).join(
                    models.Account
                ).filter(
                    models.Account.type == 'REVENUE',
                    models.JournalLine.credit > 0
                ).scalar() or 0
            if expenses is None:
                expenses = db.query(func.sum(models.JournalLine.debit)).join(
                    models.Account
                ).filter(
                    models.Account.type == 'EXPENSE',
                    models.JournalLine.debit > 0
                ).scalar() or 0
        finally:
            db.close()

    revenues = float(revenues or 0)
    expenses = float(expenses or 0)
    if net_profit is None:
        net_profit = revenues - expenses
    net_profit = float(net_profit or 0)

    if not output_path:
        output_path = _default_path("Profit_Loss")

    rows = pd.DataFrame([
        {"البند": "إجمالي الإيرادات", "المبلغ": revenues},
        {"البند": "إجمالي المصروفات", "المبلغ": expenses},
        {"البند": "صافي الربح/الخسارة", "المبلغ": net_profit},
    ])

    with pd.ExcelWriter(output_path, engine='xlsxwriter') as writer:
        workbook = writer.book
        title_format = workbook.add_format({
            'bold': True, 'font_size': 16, 'align': 'center',
            'bg_color': '#1F4E78', 'font_color': 'white'
        })
        rows.to_excel(writer, sheet_name='الأرباح والخسائر', index=False, startrow=2)
        ws = writer.sheets['الأرباح والخسائر']
        _add_title(ws, "تقرير الأرباح والخسائر (Profit & Loss)", title_format)
        ws.set_column(0, 0, 30)
        ws.set_column(1, 1, 18)

    print(f"✅ تم تصدير تقرير الأرباح والخسائر إلى: {output_path}")
    return output_path


# ═══════════════════════════════════════════════════════════
#  تقرير المخزون (⭐ دالة كانت ناقصة — تستدعيها صفحة 5)
# ═══════════════════════════════════════════════════════════
def export_inventory_to_excel(output_path=None):
    """تصدير تقرير حالة المخزون (نفس بيانات صفحة التقارير)."""
    from cache_helpers import get_items_with_stock

    items = [it for it in get_items_with_stock() if not it["is_kit"]]

    data = []
    for it in items:
        data.append({
            "الصنف": it["name"],
            "الباركود": it["barcode"] or "-",
            "سعر التكلفة": it["cost_price"],
            "سعر البيع": it["sell_price"],
            "الرصيد الحالي": it["current_stock"],
            "الحد الأدنى": it["min_stock"],
            "الحالة": "⚠️ منخفض" if it["current_stock"] <= it["min_stock"] else "✅ جيد",
            "قيمة المخزون": it["current_stock"] * it["cost_price"],
        })

    if not output_path:
        output_path = _default_path("Inventory_Report")

    df = pd.DataFrame(data)
    with pd.ExcelWriter(output_path, engine='xlsxwriter') as writer:
        workbook = writer.book
        title_format = workbook.add_format({
            'bold': True, 'font_size': 16, 'align': 'center',
            'bg_color': '#1F4E78', 'font_color': 'white'
        })
        df.to_excel(writer, sheet_name='تقرير المخزون', index=False, startrow=2)
        ws = writer.sheets['تقرير المخزون']
        _add_title(ws, "تقرير حالة المخزون", title_format)
        ws.set_column(0, len(df.columns) - 1, 16)

    print(f"✅ تم تصدير تقرير المخزون إلى: {output_path}")
    return output_path


# ═══════════════════════════════════════════════════════════
#  كشف الحساب (⭐ دالة كانت ناقصة — تستدعيها صفحة 10)
# ═══════════════════════════════════════════════════════════
def export_statement_to_excel(
    party_name: str,
    party_type: str,
    statement_data: list,
    total_debit: float,
    total_credit: float,
    running_balance: float,
    output_path: str = None,
):
    """تصدير كشف حساب عميل/مورد إلى Excel."""
    if not output_path:
        output_path = _default_path(f"Account_Statement_{str(party_name)[:20].strip()}")

    df = pd.DataFrame(statement_data)

    with pd.ExcelWriter(output_path, engine='xlsxwriter') as writer:
        workbook = writer.book
        title_format = workbook.add_format({
            'bold': True, 'font_size': 16, 'align': 'center',
            'bg_color': '#1F4E78', 'font_color': 'white'
        })
        total_format = workbook.add_format({
            'bold': True, 'bg_color': '#D9E1F2', 'border': 1, 'font_size': 12
        })

        type_ar = "عميل" if party_type == "customer" else "مورد"
        start_row = 2
        if not df.empty:
            df.to_excel(writer, sheet_name='كشف الحساب', index=False, startrow=start_row)
        else:
            pd.DataFrame({"البيان": ["لا توجد حركات"]}).to_excel(
                writer, sheet_name='كشف الحساب', index=False, startrow=start_row)

        ws = writer.sheets['كشف الحساب']
        _add_title(ws, f"كشف حساب {type_ar}: {party_name}", title_format)
        ws.set_column(0, 5, 18)

        end_row = start_row + max(len(df), 1) + 2
        ws.write(end_row, 0, "إجمالي المدين:", total_format)
        ws.write(end_row, 2, total_debit, total_format)
        ws.write(end_row + 1, 0, "إجمالي الدائن:", total_format)
        ws.write(end_row + 1, 2, total_credit, total_format)
        ws.write(end_row + 2, 0, "الرصيد الحالي:", total_format)
        ws.write(end_row + 2, 2, running_balance, total_format)

    print(f"✅ تم تصدير كشف الحساب إلى: {output_path}")
    return output_path
