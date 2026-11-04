# ui_enhancements.py
import streamlit as st

def render_enhancements():
    """تحسينات بصرية خفيفة (CSS فقط - لا ثقل على البرنامج)."""
    st.markdown("""
    <style>
    /* 1) ألوان ذكية للحالات */
    .stDataFrame td[data-testid*='stTableCell'] {
        font-size: 13px;
    }
    
    /* تلوين الصفوف حسب الحالة */
    [data-testid='stDataFrame'] [title*='paid'] {
        background-color: rgba(16, 185, 129, 0.08) !important;
    }
    [data-testid='stDataFrame'] [title*='pending'] {
        background-color: rgba(245, 158, 11, 0.08) !important;
    }
    [data-testid='stDataFrame'] [title*='cancelled'] {
        background-color: rgba(239, 68, 68, 0.08) !important;
        text-decoration: line-through;
    }
    
    /* 2) عداد السلة - badge */
    .cart-badge {
        display: inline-block;
        background: #10b981;
        color: white;
        border-radius: 50%;
        padding: 2px 7px;
        font-size: 11px;
        font-weight: bold;
        margin-right: 4px;
        min-width: 18px;
        text-align: center;
    }
    
    /* 3) Toast notifications */
    .techtoon-toast {
        position: fixed;
        top: 70px;
        right: 20px;
        background: rgba(16, 185, 129, 0.95);
        color: white;
        padding: 10px 16px;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.3);
        z-index: 9999;
        font-size: 13px;
        animation: slideIn 0.3s ease-out;
    }
    @keyframes slideIn {
        from { transform: translateX(100%); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
    }
    
    /* 4) Breadcrumbs */
    .breadcrumb {
        background: rgba(255,255,255,0.03);
        padding: 6px 12px;
        border-radius: 6px;
        font-size: 12px;
        color: #94a3b8;
        margin-bottom: 12px;
    }
    .breadcrumb a {
        color: #10b981;
        text-decoration: none;
    }
    .breadcrumb a:hover {
        text-decoration: underline;
    }
    .breadcrumb .sep {
        margin: 0 6px;
        color: #475569;
    }
    
    /* 5) Skeleton loader */
    .skeleton {
        background: linear-gradient(90deg, rgba(255,255,255,0.05) 25%, rgba(255,255,255,0.1) 50%, rgba(255,255,255,0.05) 75%);
        background-size: 200% 100%;
        animation: shimmer 1.5s infinite;
        border-radius: 4px;
        height: 20px;
        margin: 4px 0;
    }
    @keyframes shimmer {
        0% { background-position: 200% 0; }
        100% { background-position: -200% 0; }
    }
    
    /* 6) أزرار أكشن محسّنة */
    .stButton > button {
        transition: all 0.2s;
        font-weight: 500;
    }
    .stButton > button:hover {
        transform: translateY(-1px);
        box-shadow: 0 4px 8px rgba(0,0,0,0.2);
    }
    
    /* 7] مؤشر الحالة في الأعلى */
    .status-pill {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        padding: 3px 10px;
        border-radius: 12px;
        font-size: 11px;
        font-weight: 500;
    }
    .status-pill.ok { background: rgba(16,185,129,0.15); color: #10b981; }
    .status-pill.warn { background: rgba(245,158,11,0.15); color: #f59e0b; }
    .status-pill.err { background: rgba(239,68,68,0.15); color: #ef4444; }

    /* أرقام بعرض ثابت (monospace) */
    .mono, [data-testid='stDataFrame'] td {
        font-family: 'Consolas', 'Courier New', monospace !important;
        font-variant-numeric: tabular-nums;
    }
    
    /* مؤشر "حفظ تلقائي" */
    .autosave-indicator {
        position: fixed;
        top: 12px;
        right: 20px;
        background: rgba(16, 185, 129, 0.9);
        color: white;
        padding: 4px 10px;
        border-radius: 12px;
        font-size: 11px;
        z-index: 9999;
        opacity: 0;
        transition: opacity 0.3s;
    }
    .autosave-indicator.show {
        opacity: 1;
    }
    
    /* تحسين حقول الإدخال */
    .stTextInput > div > div > input,
    .stNumberInput > div > div > input {
        transition: border-color 0.2s, box-shadow 0.2s;
    }
    .stTextInput > div > div > input:focus,
    .stNumberInput > div > div > input:focus {
        border-color: #10b981 !important;
        box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2) !important;
    }
    </style>
    """, unsafe_allow_html=True)


def show_toast(message, type='success'):
    """عرض toast notification."""
    colors = {
        'success': 'rgba(16, 185, 129, 0.95)',
        'warning': 'rgba(245, 158, 11, 0.95)',
        'error': 'rgba(239, 68, 68, 0.95)',
        'info': 'rgba(59, 130, 246, 0.95)',
    }
    color = colors.get(type, colors['success'])
    st.markdown(f"""
    <div class="techtoon-toast" style="background:{color};">
        {message}
    </div>
    <script>
        setTimeout(function() {{
            const t = document.querySelector('.techtoon-toast');
            if (t) {{
                t.style.transition = 'opacity 0.3s';
                t.style.opacity = '0';
                setTimeout(() => t.remove(), 300);
            }}
        }}, 3000);
    </script>
    """, unsafe_allow_html=True)


def show_breadcrumb(items):
    """عرض breadcrumbs. items = list of (label, url_or_None)."""
    parts = []
    for i, (label, url) in enumerate(items):
        if i > 0:
            parts.append('<span class="sep">/</span>')
        if url:
            parts.append(f'<a href="{url}">{label}</a>')
        else:
            parts.append(f'<span>{label}</span>')
    st.markdown(f'<div class="breadcrumb">{"".join(parts)}</div>', unsafe_allow_html=True)


def show_skeleton(rows=3):
    """عرض skeleton loader أثناء التحميل."""
    html = '<div>'
    for _ in range(rows):
        html += '<div class="skeleton"></div>'
    html += '</div>'
    st.markdown(html, unsafe_allow_html=True)


def show_cart_badge(count):
    """عرض badge بعدد الأصناف في السلة."""
    if count > 0:
        st.markdown(f'<span class="cart-badge">{count}</span>', unsafe_allow_html=True)


def show_autosave(message="💾 تم الحفظ التلقائي"):
    """عرض مؤشر حفظ تلقائي يختفي بعد 2 ثانية."""
    st.markdown(f"""
    <div class="autosave-indicator" id="autosave-ind">{message}</div>
    <script>
        const el = document.getElementById('autosave-ind');
        if (el) {{
            el.classList.add('show');
            setTimeout(() => el.classList.remove('show'), 2000);
        }}
    </script>
    """, unsafe_allow_html=True)
