# recently_viewed.py
"""تتبع آخر ما تم فتحه — وصول سريع للفواتير/الأصناف الأخيرة."""
import streamlit as st
from datetime import datetime


def track_view(item_type, item_id, item_label, url=None):
    """تسجيل عنصر تم فتحه."""
    key = "_recently_viewed"
    if key not in st.session_state:
        st.session_state[key] = []
    
    # احذف المكرر
    items = [i for i in st.session_state[key] if not (i["type"] == item_type and i["id"] == item_id)]
    
    # أضف في المقدمة
    items.insert(0, {
        "type": item_type,
        "id": item_id,
        "label": item_label,
        "url": url,
        "time": datetime.now().strftime("%H:%M"),
    })
    
    # احتفظ بآخر 8 فقط
    st.session_state[key] = items[:8]


def render_recently_viewed():
    """عرض قائمة آخر العناصر المفتوحة في الشريط الجانبي."""
    items = st.session_state.get("_recently_viewed", [])
    if not items:
        return
    
    icons = {
        "invoice": "🧾",
        "item": "📦",
        "party": "👤",
        "payment": "💰",
        "report": "📊",
    }
    
    st.markdown("""
    <style>
        .rv-list {
            margin: 8px 0;
            padding: 8px;
            background: rgba(255,255,255,0.03);
            border-radius: 8px;
        }
        .rv-item {
            display: flex;
            align-items: center;
            gap: 6px;
            padding: 5px 8px;
            font-size: 12px;
            color: #cbd5e1;
            border-radius: 4px;
            text-decoration: none;
            transition: background 0.2s;
        }
        .rv-item:hover { background: rgba(16,185,129,0.1); color: #10b981; }
        .rv-time { color: #64748b; font-size: 10px; margin-right: auto; }
        .rv-title { font-size: 11px; color: #94a3b8; margin-bottom: 4px; padding: 0 8px; }
    </style>
    """, unsafe_allow_html=True)
    
    html = '<div class="rv-list">'
    html += '<div class="rv-title">⏱️ آخر ما تم فتحه</div>'
    
    for item in items[:5]:
        icon = icons.get(item["type"], "📄")
        url = item.get("url", "")
        label = item["label"][:25]
        time = item["time"]
        
        if url:
            html += f'<a href="{url}" class="rv-item">{icon} {label} <span class="rv-time">{time}</span></a>'
        else:
            html += f'<div class="rv-item">{icon} {label} <span class="rv-time">{time}</span></div>'
    
    html += '</div>'
    st.markdown(html, unsafe_allow_html=True)
