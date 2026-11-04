# ui_widgets.py
import streamlit as st
from database import SessionLocal
import models

def render_io_cards(key_prefix="io"):
    """بطاقات مربعة لاختيار الوارد/الصادر."""
    st.markdown("""
    <style>
        .io-card {
            border: 2px solid rgba(255,255,255,0.1);
            border-radius: 12px;
            padding: 20px;
            text-align: center;
            cursor: pointer;
            transition: all 0.2s;
            background: rgba(255,255,255,0.03);
        }
        .io-card.selected-in {
            border-color: #10b981;
            background: rgba(16, 185, 129, 0.1);
        }
        .io-card.selected-out {
            border-color: #ef4444;
            background: rgba(239, 68, 68, 0.1);
        }
        .io-icon { font-size: 30px; margin-bottom: 8px; }
        .io-label { font-size: 16px; font-weight: bold; }
        .io-desc { font-size: 12px; color: #94a3b8; margin-top: 4px; }
    </style>
    """, unsafe_allow_html=True)
    
    col1, col2 = st.columns(2)
    
    with col1:
        if st.button("⬇️\n**وارد (قبض)**\nاستلام نقدية", key=f"{key_prefix}_in", use_container_width=True):
            st.session_state[f"{key_prefix}_type"] = "receipt"
            st.rerun()
    
    with col2:
        if st.button("⬆️\n**صادر (صرف)**\nدفع نقدية", key=f"{key_prefix}_out", use_container_width=True):
            st.session_state[f"{key_prefix}_type"] = "payment"
            st.rerun()
    
    return st.session_state.get(f"{key_prefix}_type", "receipt")


def render_hierarchical_account_selectbox(label="الحساب:", key="acc_sel", exclude_types=None):
    """قائمة منسدلة للحسابات مع تجميعها تحت حساب الأب."""
    db = SessionLocal()
    try:
        accounts = db.query(models.Account).order_by(models.Account.code).all()
        
        # بناء شجرة الحسابات
        parents = [a for a in accounts if a.parent_id is None]
        children_map = {}
        for acc in accounts:
            if acc.parent_id:
                children_map.setdefault(acc.parent_id, []).append(acc)
        
        # بناء الخيارات
        options = []
        acc_map = {}
        
        for parent in parents:
            if exclude_types and parent.type in exclude_types:
                continue
            # أضف الأب
            p_label = f"📁 {parent.code} - {parent.name}"
            options.append(p_label)
            acc_map[p_label] = parent.id
            
            # أضف الأبناء
            children = children_map.get(parent.id, [])
            for child in children:
                c_label = f"   └─ {child.code} - {child.name}"
                options.append(c_label)
                acc_map[c_label] = child.id
        
        selected = st.selectbox(label, options, key=key)
        return acc_map.get(selected)
    finally:
        db.close()
