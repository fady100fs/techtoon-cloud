# modern_ui.py
"""
نظام التصميم العصري الموحد لبرنامج Techtoon Accounting
يحتوي على مكونات UI قابلة لإعادة الاستخدام
"""

import streamlit as st
from datetime import datetime

def set_modern_theme():
    """تطبيق الثيم العصري على التطبيق"""
    st.markdown("""
    <style>
    /* ===== الألوان الأساسية ===== */
    :root {
        --primary-color: #2563eb;
        --secondary-color: #1e40af;
        --success-color: #10b981;
        --warning-color: #f59e0b;
        --danger-color: #ef4444;
        --bg-color: #0f172a;
        --card-bg: #1e293b;
        --text-color: #f1f5f9;
        --text-muted: #94a3b8;
        --border-color: #334155;
    }
    
    /* ===== الخلفية العامة ===== */
    .stApp {
        background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
        color: #f1f5f9;
    }
    
    /* ===== الشريط الجانبي ===== */
    section[data-testid="stSidebar"] {
        background: linear-gradient(180deg, #1e293b 0%, #0f172a 100%);
        border-right: 1px solid #334155;
    }
    
    /* ===== العناوين ===== */
    h1, h2, h3 {
        color: #f1f5f9;
        font-weight: 700;
    }
    
    /* ===== البطاقات ===== */
    .modern-card {
        background: linear-gradient(135deg, #1e293b 0%, #334155 100%);
        border-radius: 16px;
        padding: 24px;
        margin: 16px 0;
        border: 1px solid #475569;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        transition: all 0.3s ease;
    }
    
    .modern-card:hover {
        transform: translateY(-2px);
        box-shadow: 0 8px 12px rgba(0, 0, 0, 0.2);
    }
    
    /* ===== بطاقات الإحصائيات ===== */
    .stat-card {
        background: linear-gradient(135deg, #2563eb 0%, #1e40af 100%);
        border-radius: 16px;
        padding: 24px;
        color: white;
        text-align: center;
        box-shadow: 0 4px 6px rgba(37, 99, 235, 0.3);
    }
    
    .stat-card.success {
        background: linear-gradient(135deg, #10b981 0%, #059669 100%);
        box-shadow: 0 4px 6px rgba(16, 185, 129, 0.3);
    }
    
    .stat-card.warning {
        background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
        box-shadow: 0 4px 6px rgba(245, 158, 11, 0.3);
    }
    
    .stat-card.danger {
        background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
        box-shadow: 0 4px 6px rgba(239, 68, 68, 0.3);
    }
    
    .stat-value {
        font-size: 32px;
        font-weight: bold;
        margin: 8px 0;
    }
    
    .stat-label {
        font-size: 14px;
        opacity: 0.9;
    }
    
    /* ===== أزرار المربعات (Grid Icons) ===== */
    .icon-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
        gap: 16px;
        padding: 20px;
    }
    
    .icon-box {
        background: linear-gradient(135deg, #1e293b 0%, #334155 100%);
        border: 2px solid #475569;
        border-radius: 16px;
        padding: 24px;
        text-align: center;
        cursor: pointer;
        transition: all 0.3s ease;
        text-decoration: none;
        color: #f1f5f9;
    }
    
    .icon-box:hover {
        border-color: #2563eb;
        transform: translateY(-4px);
        box-shadow: 0 8px 16px rgba(37, 99, 235, 0.3);
    }
    
    .icon-box .icon {
        font-size: 48px;
        margin-bottom: 12px;
    }
    
    .icon-box .label {
        font-size: 14px;
        font-weight: 600;
    }
    
    /* ===== الشريط العلوي ===== */
    .top-bar {
        background: linear-gradient(90deg, #2563eb 0%, #1e40af 100%);
        color: white;
        padding: 16px 24px;
        border-radius: 12px;
        margin-bottom: 24px;
        display: flex;
        justify-content: space-between;
        align-items: center;
        box-shadow: 0 4px 6px rgba(37, 99, 235, 0.3);
    }
    
    .top-bar .user-info {
        font-weight: 600;
    }
    
    .top-bar .backup-info {
        font-size: 14px;
        opacity: 0.9;
    }
    
    /* ===== الجداول ===== */
    .dataframe {
        background: #1e293b;
        border-radius: 12px;
        overflow: hidden;
    }
    
    /* ===== الأزرار ===== */
    .stButton > button {
        border-radius: 8px;
        font-weight: 600;
        transition: all 0.3s ease;
    }
    
    .stButton > button[kind="primary"] {
        background: linear-gradient(135deg, #2563eb 0%, #1e40af 100%);
        border: none;
    }
    
    .stButton > button[kind="primary"]:hover {
        background: linear-gradient(135deg, #1e40af 0%, #1e3a8a 100%);
        transform: translateY(-2px);
    }
    
    /* ===== حقول الإدخال ===== */
    .stTextInput > div > div > input,
    .stNumberInput > div > div > input,
    .stTextArea > div > div > textarea {
        background: #1e293b;
        border: 1px solid #475569;
        border-radius: 8px;
        color: #f1f5f9;
    }
    
    .stTextInput > div > div > input:focus,
    .stNumberInput > div > div > input:focus {
        border-color: #2563eb;
        box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.2);
    }
    
    /* ===== التبويبات ===== */
    .stTabs [data-baseweb="tab-list"] {
        background: #1e293b;
        border-radius: 12px;
        padding: 4px;
    }
    
    .stTabs [data-baseweb="tab"] {
        color: #94a3b8;
        border-radius: 8px;
    }
    
    .stTabs [aria-selected="true"] {
        background: #2563eb;
        color: white;
    }
    
    /* ===== التنبيهات ===== */
    .stAlert {
        border-radius: 12px;
        border: none;
    }
    
    /* ===== الفاصل ===== */
    .modern-divider {
        height: 2px;
        background: linear-gradient(90deg, transparent, #2563eb, transparent);
        margin: 24px 0;
    }
    </style>
    """, unsafe_allow_html=True)


def show_top_bar(user_name, user_role, last_backup=""):
    """عرض الشريط العلوي"""
    st.markdown(f"""
    <div class="top-bar">
        <div class="user-info">
            👤 {user_name} | 🎭 {user_role}
        </div>
        <div class="backup-info">
            💾 آخر نسخة: {last_backup or 'لم تُنشأ بعد'}
        </div>
    </div>
    """, unsafe_allow_html=True)


def show_stat_card(label, value, card_type="primary", icon="📊"):
    """عرض بطاقة إحصائية"""
    st.markdown(f"""
    <div class="stat-card {card_type}">
        <div style="font-size: 32px;">{icon}</div>
        <div class="stat-value">{value}</div>
        <div class="stat-label">{label}</div>
    </div>
    """, unsafe_allow_html=True)


def show_icon_box(icon, label, page_path):
    """عرض مربع أيقونة للصفحة"""
    st.markdown(f"""
    <a href="{page_path}" style="text-decoration: none;">
        <div class="icon-box">
            <div class="icon">{icon}</div>
            <div class="label">{label}</div>
        </div>
    </a>
    """, unsafe_allow_html=True)


def show_modern_card(title, content):
    """عرض بطاقة حديثة"""
    st.markdown(f"""
    <div class="modern-card">
        <h3>{title}</h3>
        {content}
    </div>
    """, unsafe_allow_html=True)


def show_divider():
    """عرض فاصل عصري"""
    st.markdown('<div class="modern-divider"></div>', unsafe_allow_html=True)


def show_page_header(title, icon, subtitle=""):
    """عرض رأس الصفحة"""
    st.markdown(f"""
    <div style="text-align: center; margin: 32px 0;">
        <div style="font-size: 64px;">{icon}</div>
        <h1 style="margin: 16px 0;">{title}</h1>
        {f'<p style="color: #94a3b8; font-size: 18px;">{subtitle}</p>' if subtitle else ''}
    </div>
    """, unsafe_allow_html=True)