# -*- coding: utf-8 -*-
"""input_helpers.py - حقول إدخال ذكية تدعم المعادلات."""
import ast
import operator
import streamlit as st

_ALLOWED_OPS = {
    ast.Add: operator.add, ast.Sub: operator.sub,
    ast.Mult: operator.mul, ast.Div: operator.truediv,
    ast.Pow: operator.pow, ast.Mod: operator.mod,
    ast.USub: operator.neg, ast.UAdd: operator.pos,
}

def safe_eval_math(expr):
    if expr is None:
        return 0.0
    s = str(expr).strip()
    if not s:
        return 0.0
    s = s.replace("*", "*").replace("/", "/").replace(",", "").replace(" ", "")
    try:
        node = ast.parse(s, mode="eval").body
    except Exception:
        try:
            return float(s)
        except Exception:
            return 0.0
    def _eval(n):
        if isinstance(n, ast.Constant):
            if isinstance(n.value, (int, float)):
                return n.value
            raise ValueError("non-numeric")
        if isinstance(n, ast.BinOp) and type(n.op) in _ALLOWED_OPS:
            return _ALLOWED_OPS[type(n.op)](_eval(n.left), _eval(n.right))
        if isinstance(n, ast.UnaryOp) and type(n.op) in _ALLOWED_OPS:
            return _ALLOWED_OPS[type(n.op)](_eval(n.operand))
        raise ValueError("unsupported")
    try:
        return float(_eval(node))
    except Exception:
        return 0.0

def price_input(label, key, default=0.0, help_text=None):
    state_key = "_raw_" + key
    val_key = "_val_" + key
    if val_key not in st.session_state:
        st.session_state[val_key] = float(default or 0.0)
    if state_key not in st.session_state:
        st.session_state[state_key] = ("%.2f" % default) if default and default != 0 else ""
    raw = st.text_input(
        label, key=state_key,
        help=help_text or "رقم أو معادلة: 150 | 650/5 | 500*1.14 | 200+50",
        placeholder="مثال: 150  أو  650/5",
    )
    computed = safe_eval_math(raw)
    st.session_state[val_key] = computed
    raw_clean = str(raw or "").strip()
    is_simple = False
    try:
        float(raw_clean)
        is_simple = True
    except Exception:
        pass
    if raw_clean and not is_simple and computed > 0:
        st.caption("💰 الناتج: **{:,.2f}**".format(computed))
    return computed

def qty_input(label, key, default=1, help_text=None):
    state_key = "_rawq_" + key
    val_key = "_valq_" + key
    if val_key not in st.session_state:
        st.session_state[val_key] = float(default or 1)
    if state_key not in st.session_state:
        st.session_state[state_key] = str(default or 1)
    raw = st.text_input(
        label, key=state_key,
        help=help_text or "رقم أو معادلة: 10 | 100/5 | 2*3",
    )
    computed = safe_eval_math(raw)
    if computed <= 0:
        computed = 0.0
    st.session_state[val_key] = computed
    raw_clean = str(raw or "").strip()
    try:
        float(raw_clean)
    except Exception:
        if raw_clean and computed > 0:
            st.caption("📦 الناتج: **{:,.2f}**".format(computed))
    return computed
