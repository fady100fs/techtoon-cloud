# loading_indicator.py
"""مؤشرات تحميل أنيقة — skeleton + spinner محسّن."""
import streamlit as st
from contextlib import contextmanager


@contextmanager
def loading_state(message="جاري التحميل..."):
    """عرض مؤشر تحميل أثناء تنفيذ عملية."""
    placeholder = st.empty()
    
    with placeholder.container():
        st.markdown(f"""
        <div style="display:flex;align-items:center;gap:10px;padding:12px;
                    background:rgba(59,130,246,0.1);border-radius:8px;margin:8px 0;">
            <div style="width:20px;height:20px;border:2px solid #3b82f6;
                        border-top:transparent;border-radius:50%;
                        animation:spin 0.8s linear infinite;"></div>
            <span style="color:#3b82f6;font-size:13px;">{message}</span>
        </div>
        <style>
            @keyframes spin {{
                0% {{ transform: rotate(0deg); }}
                100% {{ transform: rotate(360deg); }}
            }}
        </style>
        """, unsafe_allow_html=True)
    
    try:
        yield
    finally:
        placeholder.empty()


def skeleton_table(rows=5, cols=4):
    """عرض skeleton لجدول أثناء التحميل."""
    st.markdown("""
    <style>
        .skeleton-cell {
            background: linear-gradient(90deg, rgba(255,255,255,0.05) 25%, 
                                       rgba(255,255,255,0.1) 50%, 
                                       rgba(255,255,255,0.05) 75%);
            background-size: 200% 100%;
            animation: shimmer 1.5s infinite;
            border-radius: 4px;
            height: 20px;
            margin: 4px;
        }
        @keyframes shimmer {
            0% { background-position: 200% 0; }
            100% { background-position: -200% 0; }
        }
    </style>
    """, unsafe_allow_html=True)
    
    html = '<div>'
    for _ in range(rows):
        html += '<div style="display:flex;gap:8px;margin-bottom:8px;">'
        for _ in range(cols):
            html += '<div class="skeleton-cell" style="flex:1;"></div>'
        html += '</div>'
    html += '</div>'
    
    st.markdown(html, unsafe_allow_html=True)


def progress_bar(value, max_value=100, label=""):
    """شريط تقدم أنيق."""
    pct = min(100, int((value / max_value) * 100)) if max_value > 0 else 0
    
    color = "#10b981" if pct >= 80 else "#f59e0b" if pct >= 50 else "#ef4444"
    
    st.markdown(f"""
    <div style="margin:8px 0;">
        <div style="display:flex;justify-content:space-between;margin-bottom:4px;">
            <span style="color:#94a3b8;font-size:12px;">{label}</span>
            <span style="color:{color};font-size:12px;font-weight:bold;">{pct}%</span>
        </div>
        <div style="background:rgba(255,255,255,0.1);border-radius:6px;height:8px;overflow:hidden;">
            <div style="background:{color};height:100%;width:{pct}%;
                        border-radius:6px;transition:width 0.3s;"></div>
        </div>
    </div>
    """, unsafe_allow_html=True)
