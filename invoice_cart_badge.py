# invoice_cart_badge.py
"""عداد أصناف الفاتورة — يعرض عدد الأصناف في الأعلى."""
import streamlit as st


def render_cart_badge(invoice_type):
    """عرض عداد الأصناف في الفاتورة الحالية."""
    items = st.session_state.get("invoice_items", [])
    count = len(items)
    
    if count > 0:
        total = sum(float(i.get("quantity", 0)) * float(i.get("price", 0)) for i in items)
        
        st.markdown(f"""
        <div style="display:flex;justify-content:space-between;align-items:center;
                    background:rgba(16,185,129,0.1);border:1px solid rgba(16,185,129,0.3);
                    border-radius:8px;padding:8px 14px;margin:8px 0;">
            <div>
                <span style="background:#10b981;color:white;border-radius:50%;
                             padding:3px 9px;font-size:12px;font-weight:bold;">
                    {count}
                </span>
                <span style="color:#10b981;font-weight:500;margin-right:8px;">
                    صنف في الفاتورة
                </span>
            </div>
            <div style="color:#10b981;font-weight:bold;font-size:15px;">
                {total:,.2f} ج.م
            </div>
        </div>
        """, unsafe_allow_html=True)
    else:
        st.markdown("""
        <div style="display:flex;justify-content:center;align-items:center;
                    background:rgba(100,116,139,0.05);border:1px dashed rgba(100,116,139,0.3);
                    border-radius:8px;padding:12px;margin:8px 0;">
            <span style="color:#64748b;font-size:13px;">
                🛒 الفاتورة فارغة — أضف أصنافاً للبدء
            </span>
        </div>
        """, unsafe_allow_html=True)
