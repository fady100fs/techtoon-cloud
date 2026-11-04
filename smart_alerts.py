# smart_alerts.py
"""تنبيهات ذكية — مخزون منخفض + فواتير معلقة + ديون متأخرة.
يستخدم الكاش الموجود — لا استعلامات إضافية."""
import streamlit as st
from datetime import datetime, timedelta


def render_smart_alerts():
    """يعرض تنبيهات ذكية في أعلى الصفحة."""
    try:
        from cache_helpers import get_items_with_stock
        items = get_items_with_stock()
        
        # 1) مخزون منخفض
        low_stock = [
            it for it in items
            if it.get("current_stock", 0) <= it.get("min_stock", 0)
            and not it.get("is_kit", False)
        ][:5]  # أول 5 فقط
        
        if low_stock:
            alerts_html = '<div style="margin-bottom:12px;">'
            alerts_html += '<div style="background:rgba(245,158,11,0.1);border:1px solid rgba(245,158,11,0.4);border-radius:8px;padding:10px 14px;margin-bottom:8px;">'
            alerts_html += '<span style="color:#f59e0b;font-weight:bold;">⚠️ مخزون منخفض</span><br>'
            alerts_html += '<div style="font-size:12px;color:#94a3b8;margin-top:4px;">'
            for it in low_stock:
                alerts_html += f'• {it["name"]} — المتاح: {it["current_stock"]:.0f} (الحد الأدنى: {it["min_stock"]:.0f})<br>'
            alerts_html += '</div></div>'
            alerts_html += '</div>'
            
            st.markdown(alerts_html, unsafe_allow_html=True)
        
        # 2) فواتير معلقة (لو في الكاش)
        try:
            from cache_helpers import get_invoices_index
            invoices = get_invoices_index(limit=100)
            pending = [i for i in invoices if i.get("status") == "pending"][:3]
            
            if pending:
                alerts_html = '<div style="background:rgba(59,130,246,0.1);border:1px solid rgba(59,130,246,0.4);border-radius:8px;padding:10px 14px;margin-bottom:12px;">'
                alerts_html += '<span style="color:#3b82f6;font-weight:bold;">📋 فواتير معلقة</span><br>'
                alerts_html += '<div style="font-size:12px;color:#94a3b8;margin-top:4px;">'
                for inv in pending:
                    alerts_html += f'• {inv["invoice_number"]} — {inv["party_name"]} — {inv["net_amount"]:,.0f} ج.م<br>'
                alerts_html += '</div></div>'
                
                st.markdown(alerts_html, unsafe_allow_html=True)
        except Exception:
            pass
        
    except Exception:
        pass
