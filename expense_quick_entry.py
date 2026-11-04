# expense_quick_entry.py
"""إدخال سريع للمصروفات الشائعة — يوفر الوقت."""
import streamlit as st
from datetime import datetime


# مصروفات شائعة جاهزة
COMMON_EXPENSES = [
    {"label": "⚡ كهرباء", "amount": 500, "category": "utilities"},
    {"label": "💧 مياه", "amount": 200, "category": "utilities"},
    {"label": "🌐 إنترنت", "amount": 400, "category": "utilities"},
    {"label": "📞 تليفون", "amount": 300, "category": "utilities"},
    {"label": "🚗 بنزين", "amount": 600, "category": "transport"},
    {"label": "🍽️ ضيافة", "amount": 250, "category": "hospitality"},
    {"label": "🧹 نظافة", "amount": 150, "category": "maintenance"},
    {"label": "📦 شحن", "amount": 200, "category": "shipping"},
]


def render_quick_expenses():
    """عرض أزرار المصروفات الشائعة."""
    st.markdown("""
    <style>
        .qe-grid {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 6px;
            margin: 10px 0;
        }
        .qe-btn {
            background: rgba(255,255,255,0.05);
            border: 1px solid rgba(255,255,255,0.1);
            border-radius: 6px;
            padding: 8px;
            text-align: center;
            cursor: pointer;
            font-size: 11px;
            color: #cbd5e1;
            transition: all 0.2s;
        }
        .qe-btn:hover {
            background: rgba(16,185,129,0.1);
            border-color: rgba(16,185,129,0.3);
            color: #10b981;
        }
        .qe-amount {
            font-size: 10px;
            color: #64748b;
            margin-top: 2px;
        }
    </style>
    """, unsafe_allow_html=True)
    
    cols = st.columns(4)
    for i, exp in enumerate(COMMON_EXPENSES):
        with cols[i % 4]:
            if st.button(exp["label"], key=f"qe_{i}", help=f"إضافة {exp['amount']} ج.م"):
                st.session_state["_quick_expense"] = {
                    "label": exp["label"],
                    "amount": exp["amount"],
                    "category": exp["category"],
                    "time": datetime.now().isoformat(),
                }
                st.toast(f"✅ تم اختيار: {exp['label']} ({exp['amount']} ج.م)")
    
    return st.session_state.get("_quick_expense")
