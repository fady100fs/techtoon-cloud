# quick_stats.py
"""بطاقة إحصائيات سريعة في الأعلى — خفيفة (تستخدم الكاش)."""
import streamlit as st


def render_quick_stats():
    """يعرض 4 إحصائيات سريعة في صف واحد."""
    try:
        from cache_helpers import get_dashboard_kpis
        
        kpis = get_dashboard_kpis()
        
        sales_today = kpis.get("monthly_sales", 0)
        invoices_count = kpis.get("invoices_count", 0)
        items_count = kpis.get("items_count", 0)
        parties_count = kpis.get("parties_count", 0)
        
        st.markdown("""
        <style>
            .qs-grid {
                display: grid;
                grid-template-columns: repeat(4, 1fr);
                gap: 12px;
                margin-bottom: 16px;
            }
            .qs-card {
                background: linear-gradient(135deg, rgba(16,185,129,0.08) 0%, rgba(16,185,129,0.02) 100%);
                border: 1px solid rgba(16,185,129,0.2);
                border-radius: 10px;
                padding: 14px 16px;
                transition: all 0.2s;
            }
            .qs-card:hover {
                border-color: rgba(16,185,129,0.5);
                transform: translateY(-2px);
            }
            .qs-label {
                color: #94a3b8;
                font-size: 11px;
                margin-bottom: 4px;
            }
            .qs-value {
                color: #10b981;
                font-size: 20px;
                font-weight: bold;
            }
            .qs-icon { font-size: 22px; float: left; opacity: 0.7; }
        </style>
        """, unsafe_allow_html=True)
        
        st.markdown(f"""
        <div class="qs-grid">
            <div class="qs-card">
                <span class="qs-icon">💰</span>
                <div class="qs-label">مبيعات الشهر</div>
                <div class="qs-value">{sales_today:,.0f} ج.م</div>
            </div>
            <div class="qs-card">
                <span class="qs-icon">🧾</span>
                <div class="qs-label">إجمالي الفواتير</div>
                <div class="qs-value">{invoices_count}</div>
            </div>
            <div class="qs-card">
                <span class="qs-icon">📦</span>
                <div class="qs-label">عدد الأصناف</div>
                <div class="qs-value">{items_count}</div>
            </div>
            <div class="qs-card">
                <span class="qs-icon">👥</span>
                <div class="qs-label">العملاء والموردين</div>
                <div class="qs-value">{parties_count}</div>
            </div>
        </div>
        """, unsafe_allow_html=True)
    except Exception:
        pass
