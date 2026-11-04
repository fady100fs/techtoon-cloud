# draft_autosave.py
"""حفظ تلقائي لمسودات الفواتير — يحمي من فقدان البيانات."""
import streamlit as st
from datetime import datetime


def save_draft(draft_key, data):
    """حفظ مسودة في session_state."""
    st.session_state[f"_draft_{draft_key}"] = {
        "data": data,
        "saved_at": datetime.now().isoformat(),
    }


def load_draft(draft_key):
    """تحميل مسودة محفوظة."""
    draft = st.session_state.get(f"_draft_{draft_key}")
    if not draft:
        return None
    return draft.get("data")


def clear_draft(draft_key):
    """حذف مسودة."""
    st.session_state.pop(f"_draft_{draft_key}", None)


def show_draft_recovery(draft_key, on_restore=None):
    """عرض رسالة استرجاع المسودة إن وُجدت."""
    draft = st.session_state.get(f"_draft_{draft_key}")
    if not draft:
        return
    
    saved_at = draft.get("saved_at", "")
    try:
        dt = datetime.fromisoformat(saved_at)
        time_str = dt.strftime("%H:%M:%S")
    except Exception:
        time_str = ""
    
    st.markdown(f"""
    <div style="padding:10px;background:rgba(59,130,246,0.1);border:1px solid rgba(59,130,246,0.4);border-radius:6px;margin:8px 0;font-size:13px;">
        💾 لديك مسودة محفوظة ({time_str})
    </div>
    """, unsafe_allow_html=True)
    
    col1, col2 = st.columns(2)
    with col1:
        if st.button("♻️ استرجاع المسودة", key=f"restore_{draft_key}"):
            if on_restore:
                on_restore(draft.get("data"))
    with col2:
        if st.button("🗑️ تجاهل", key=f"discard_{draft_key}"):
            clear_draft(draft_key)
            st.rerun()


# حفظ تلقائي لنموذج الفاتورة
def autosave_invoice_form(invoice_type, items, party_id=None, notes=None):
    """حفظ تلقائي لبيانات الفاتورة كل تغيير."""
    if items:  # احفظ فقط لو فيه أصناف
        save_draft(f"invoice_{invoice_type}", {
            "items": items,
            "party_id": party_id,
            "notes": notes,
        })
