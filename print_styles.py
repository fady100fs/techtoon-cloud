# print_styles.py
"""تنسيق طباعة احترافي للفواتير والتقارير."""
import streamlit as st


def render_print_styles():
    """يضيف CSS للطباعة — يخفي القوائم ويعرض المحتوى فقط."""
    st.markdown("""
    <style>
        @media print {
            /* إخفاء كل شيء ما عدا المحتوى */
            [data-testid="stSidebar"], 
            [data-testid="stSidebarNav"],
            .stButton,
            .stRadio,
            .stSelectbox,
            .stTextInput,
            .stNumberInput,
            .techtoon-topbar,
            .kb-hint,
            .gs-shortcut,
            .breadcrumb {
                display: none !important;
            }
            
            /* تنسيق الصفحة للطباعة */
            body {
                background: white !important;
                color: black !important;
                font-size: 12pt;
            }
            
            .stApp {
                background: white !important;
                padding: 0 !important;
            }
            
            /* تنسيق الفاتورة */
            .invoice-print {
                width: 100%;
                max-width: 800px;
                margin: 0 auto;
                padding: 20px;
            }
            
            /* رأس الفاتورة */
            .invoice-header {
                text-align: center;
                border-bottom: 2px solid #000;
                padding-bottom: 10px;
                margin-bottom: 20px;
            }
            
            .invoice-header h1 {
                font-size: 18pt;
                margin: 0;
            }
            
            .invoice-header .subtitle {
                font-size: 11pt;
                color: #555;
                margin-top: 5px;
            }
            
            /* جدول الفاتورة */
            .invoice-table {
                width: 100%;
                border-collapse: collapse;
                margin: 15px 0;
            }
            
            .invoice-table th {
                background: #f0f0f0 !important;
                color: black !important;
                border: 1px solid #000;
                padding: 8px;
                text-align: right;
                font-size: 11pt;
            }
            
            .invoice-table td {
                border: 1px solid #000;
                padding: 8px;
                text-align: right;
                font-size: 11pt;
            }
            
            /* تذييل الفاتورة */
            .invoice-footer {
                margin-top: 30px;
                border-top: 2px solid #000;
                padding-top: 10px;
                display: flex;
                justify-content: space-between;
            }
            
            .invoice-total {
                font-size: 14pt;
                font-weight: bold;
            }
            
            /* منع قطع الصفوف */
            tr { page-break-inside: avoid; }
        }
        
        /* زر طباعة في الشاشة */
        .print-btn-wrapper {
            position: fixed;
            bottom: 60px;
            left: 20px;
            z-index: 999;
        }
    </style>
    """, unsafe_allow_html=True)
    
    # زر طباعة عائم
    st.markdown("""
    <div class="print-btn-wrapper">
        <button onclick="window.print()" 
                style="background:rgba(16,185,129,0.9);color:white;border:none;padding:8px 14px;border-radius:6px;cursor:pointer;font-size:13px;box-shadow:0 2px 8px rgba(0,0,0,0.3);">
            🖨️ طباعة
        </button>
    </div>
    """, unsafe_allow_html=True)


def render_printable_invoice(invoice_data):
    """عرض فاتورة بتنسيق قابل للطباعة."""
    st.markdown(f"""
    <div class="invoice-print" style="background:white;color:black;padding:20px;border-radius:8px;margin:10px 0;">
        <div class="invoice-header">
            <h1>Techtoon Accounting</h1>
            <div class="subtitle">
                {invoice_data.get('type_label', 'فاتورة')} رقم: {invoice_data.get('number', '')}<br>
                التاريخ: {invoice_data.get('date', '')}
            </div>
        </div>
        
        <div style="display:flex;justify-content:space-between;margin:15px 0;">
            <div>
                <strong>العميل/المورد:</strong> {invoice_data.get('party_name', '')}<br>
                <strong>الحالة:</strong> {invoice_data.get('status', '')}
            </div>
        </div>
        
        <table class="invoice-table" style="width:100%;border-collapse:collapse;">
            <thead>
                <tr style="background:#f0f0f0;">
                    <th style="border:1px solid #000;padding:8px;">الصنف</th>
                    <th style="border:1px solid #000;padding:8px;">الكمية</th>
                    <th style="border:1px solid #000;padding:8px;">السعر</th>
                    <th style="border:1px solid #000;padding:8px;">الإجمالي</th>
                </tr>
            </thead>
            <tbody>
                {invoice_data.get('lines_html', '')}
            </tbody>
        </table>
        
        <div class="invoice-footer" style="margin-top:20px;border-top:2px solid #000;padding-top:10px;display:flex;justify-content:space-between;">
            <div>
                <strong>المبلغ الإجمالي:</strong>
                <div class="invoice-total" style="font-size:18px;font-weight:bold;">
                    {invoice_data.get('total', 0):,.2f} ج.م
                </div>
            </div>
            <div style="text-align:left;font-size:11px;color:#555;">
                تم الإنشاء بواسطة: {invoice_data.get('created_by', '')}<br>
                Techtoon Accounting
            </div>
        </div>
    </div>
    """, unsafe_allow_html=True)
