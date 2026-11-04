# theme_toggle.py
"""مفتاح تبديل الوضع الليلي/النهاري — خفيف (CSS variables فقط)."""
import streamlit as st


def render_theme_toggle():
    """زر تبديل بين الوضع الداكن والفاتح."""
    
    # قراءة الحالة الحالية
    current_theme = st.session_state.get("_theme", "dark")
    
    # زر التبديل
    col1, col2 = st.columns([4, 1])
    with col2:
        if st.button("🌙" if current_theme == "dark" else "☀️", 
                     key="theme_toggle_btn",
                     help="تبديل الوضع الليلي/النهاري"):
            new_theme = "light" if current_theme == "dark" else "dark"
            st.session_state["_theme"] = new_theme
            st.rerun()
    
    # تطبيق CSS حسب الوضع
    if current_theme == "light":
        st.markdown("""
        <style>
            :root {
                --bg-primary: #ffffff;
                --bg-secondary: #f8fafc;
                --text-primary: #1e293b;
                --text-secondary: #64748b;
                --border: #e2e8f0;
            }
            .stApp { background: var(--bg-primary) !important; color: var(--text-primary) !important; }
            .stMarkdown, .stText { color: var(--text-primary) !important; }
        </style>
        """, unsafe_allow_html=True)
    else:
        st.markdown("""
        <style>
            :root {
                --bg-primary: #0e1117;
                --bg-secondary: #1a1f2e;
                --text-primary: #ffffff;
                --text-secondary: #94a3b8;
                --border: rgba(255,255,255,0.1);
            }
        </style>
        """, unsafe_allow_html=True)
