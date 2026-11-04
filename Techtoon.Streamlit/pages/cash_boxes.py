"""
Cash Boxes page for Techtoon Accounting Streamlit app.
Features:
  - Dynamic balance calculation (opening + receipts - payments - expenses + loans + transfers_in - transfers_out)
  - Add / edit / deactivate cash boxes
  - Money transfers between boxes
  - Transactions log per box
  - Arithmetic expression evaluation in all numeric inputs (e.g. type "3+3" -> 6)
"""
import streamlit as st
import pandas as pd
from datetime import datetime
import sys
import os

# Make sure utils is importable
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from utils.math_eval import safe_eval

# ------------------------------------------------------------------------------
# Database connection (tries to reuse project database.py, falls back to env)
# ------------------------------------------------------------------------------
try:
    from database import get_connection as _get_connection  # type: ignore
except Exception:
    import os
    import psycopg3

    def _get_connection():
        return psycopg2.connect(
            host=os.environ.get("PGHOST", "ep-xxx.neon.tech"),
            dbname=os.environ.get("PGDATABASE", "neondb"),
            user=os.environ.get("PGUSER", "neondb_owner"),
            password=os.environ.get("PGPASSWORD", ""),
            sslmode=os.environ.get("PGSSLMODE", "require"),
            port=os.environ.get("PGPORT", "5432"),
        )


def run_query(sql, params=None, fetch=True):
    conn = _get_connection()
    try:
        cur = conn.cursor()
        cur.execute(sql, params or ())
        if fetch:
            rows = cur.fetchall()
            cols = [d[0] for d in cur.description] if cur.description else []
            conn.commit()
            return rows, cols
        conn.commit()
        return None, None
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()

# -------------------------------------------------------------------------------
# Schema helpers - detect which columns/tables exist so the page is robust
# -------------------------------------------------------------------------------
@st.cache_data(ttl=60)
def _table_columns(table):
    try:
        rows, _ = run_query(
            """
            SELECT column_name FROM information_schema.columns
            WHERE table_name = %s
            ORDER BY ordinal_position
            """,
            (table,),
        )
        return {r[0] for r in rows}
    except Exception:
        return set()

@@st.cache_data(ttl=60)
def _table_exists(table):
    try:
        rows, _ = run_query(
            "SELECT to_regclass(%s)", (table,)
        )
        return bool(rows and rows[0][0])
    except Exception:
        return False


# ------------------------------------------------------------------------------
# Balance calculation - mirrors desktop CashBoxesViewModel
# -------------------------------------------------------------------------------
def compute_box_balance(box_id, opening_balance=0.0):
    """Dynamic balance = opening + receipts - payments - expenses + loans + transfers_in - transfers_out"""
    bal = float(opening_balance or 0)

    # receipts / payments from payments table
    if _table_exists("payments"):
        cols = _table_columns("payments")
        amount_col = "amount"
        box_col = "cash_box_id" if "cash_box_id" in cols else ("box_id" if "box_id" in cols else None)
        type_col = "payment_type" if "payment_type" in cols else ("type" if "type" in cols else None)
        if box_col and type_col and amount_col in cols:
            # receipts (+)
            r, _ = run_query(
                f"SELECT COALESCE(SUM({amount_col}),0) FROM payments WHERE {box_col}=%s AND {type_col}='receipt'",
                (box_id,),
            )
            bal += float(r[0][0] or 0)
            # payments (-)
            r, _ = run_query(
                f"SELECT COALESCE(SUM({amount_col}),0) FROM payments WHERE {box_col}=%s AND {type_col}='payment'",
                (box_id,),
            )
            bal -= float(r[0][0] or 0)

    # expenses (-)
    if _table_exists("expenses"):
        cols = _table_columns("expenses")
        box_col = "cash_box_id" if "cash_box_id" in cols else ("box_id" if "box_id" in cols else None)
        amount_col = "amount" if "amount" in cols else None
        if box_col and amount_col:
            r, _ = run_query(
                f"SELECT COALESCE(SUM({amount_col}),0) FROM expenses WHERE {box_col}=%s",
                (box_id,),
            )
            bal -= float(r[0][0] or 0)

    # loans (+) - loan disbursements increase box balance
    if _table_exists("loans"):
        cols = _table_columns("loans")
        box_col = "cash_box_id" if "cash_box_id" in cols else ("box_id" if "box_id" in cols else None)
        amount_col = "amount" if "amount" in cols else ("principal" if "principal" in cols else None)
        if box_col and amount_col:
            r, _ = run_query(
                f"SELECT COALESCE(SUM({amount_col}),0) FROM loans WHERE {box_col}=%s",
                (box_id,),
            )
            bal += float(r[0][0] or 0)

    # transfers
    if _table_exists("cash_transfers"):
        cols = _table_columns("cash_transfers")
        from_col = "from_box_id" if "from_box_id" in cols else ("from_cash_box_id" if "from_cash_box_id" in cols else None)
        to_col = "to_box_id" if "to_box_id" in cols else ("to_cash_box_id" if "to_cash_box_id" in cols else None)
        amount_col = "amount" if "amount" in cols else None
        if amount_col:
            if to_col:
                r, _ = run_query(
                    f"SELECT COALESCE(SUM({amount_col}),0) FROM cash_transfers WHERE {to_col}=%s",
                    (box_id,),
                )
                bal += float(r[0][0] or 0)
            if from_col:
                r, _ = run_query(
                    f"SELECT COALESCE(SUM({amount_col}),0) FROM cash_transfers WHERE {from_col}=%s",
                    (box_id,),
                )
                bal -= float(r[0][0] or 0)

    return bal


# -------------------------------------------------------------------------------
# CRUD
# ------------------------------------------------------------------------------
def list_boxes(active_only=True):
    if not _table_exists("cash_boxes"):
        return []
    cols = _table_columns("cash_boxes")
    has_active = "is_active" in cols
    sql = "SELECT id, name, opening_balance, currency FROM cash_boxes"
    if has_active and active_only:
        sql += " WHERE is_active = TRUE"
    sql += " ORDER BY name"
    rows, _ = run_query(sql)
    out = []
    for r in rows:
        bid, name, opening, currency = r
        bal = compute_box_balance(bid, opening)
        out.append({
            "id": bid,
            "name": name,
            "opening_balance": float(opening or 0),
            "currency": currency or "EGP",
            "current_balance": bal,
        })
    return out


def add_box(name, opening, currency):
    cols = _table_columns("cash_boxes")
    has_active = "is_active" in cols
    if has_active:
        run_query(
            "INSERT INTO cash_boxes (name, opening_balance, currency, is_active, created_at) VALUES (%s,%s,%s,TRUE,NOW())",
            (name, opening, currency),
            fetch=False,
        )
    else:
        run_query(
            "INSERT INTO cash_boxes (name, opening_balance, currency, created_at) VALUES (%s,%s,%s,NOW())",
            (name, opening, currency),
            fetch=False,
        )


def transfer_amount(from_id, to_id, amount, note=""):
    if not _table_exists("cash_transfers"):
        # create on the fly
        run_query(
            """
            CREATE TABLE IF NOT EXISTS cash_transfers (
                id SERIAL PRIMARY KEY,
                from_box_id INTEGER NOT NULL,
                to_box_id INTEGER NOT NULL,
                amount NUMERIC(14,2) NOT NULL,
                note TEXT,
                transfer_date TIMESTAMP DEFAULT NOW()
            )
            """,
            fetch=False,
        )
    run_query(
        "INSERT INTO cash_transfers (from_box_id, to_box_id, amount, note, transfer_date) VALUES (%s,%s,%s,%s,NOW())",
        (from_id, to_id, amount, note),
        fetch=False,
    )


def box_transactions(box_id, limit=200):
    """Return a unified list of transactions for a box."""
    txns = []
    if _table_exists("payments"):
        cols = _table_columns("payments")
        box_col = "cash_box_id" if "cash_box_id" in cols else ("box_id" if "box_id" in cols else None)
        type_col = "payment_type" if "payment_type" in cols else ("type" if "type" in cols else None)
        date_col = "payment_date" if "payment_date" in cols else "created_at"
        if box_col and type_col:
            r, _ = run_query(
                f"SELECT {date_col}, {type_col}, amount, note FROM payments WHERE {box_col}=%s ORDER BY {date_col} DESC LIMIT %s",
                (box_id, limit),
            )
            for row in r:
                d, t, amt, note = row
                txns.append({
                    "date": d,
                    "type": "قبض" if t == "receipt" else "صرف",
                    "amount": float(amt or 0),
                    "note": note or "",
                    "source": "مدفوعات",
                })
    if _table_exists("expenses"):
        cols = _table_columns("expenses")
        box_col = "cash_box_id" if "cash_box_id" in cols else ("box_id" if "box_id" in cols else None)
        date_col = "expense_date" if "expense_date" in cols else "created_at"
        if box_col:
            r, _ = run_query(
                f"SELECT {date_col}, amount, description FROM expenses WHERE {box_col}=%s ORDER BY {date_col} DESC LIMIT %s",
                (box_id, limit),
            )
            for row in r:
                d, amt, desc = row
                txns.append({
                    "date": d,
                    "type": "مصروف",
                    "amount": -float(amt or 0),
                    "note": desc or "",
                    "source": "مصروفات",
                })
    if _table_exists("cash_transfers"):
        cols = _table_columns("cash_transfers")
        from_col = "from_box_id" if "from_box_id" in cols else None
        to_col = "to_box_id" if "to_box_id" in cols else None
        if from_col and to_col:
            r, _ = run_query(
                f"SELECT transfer_date, {from_col}, {to_col}, amount, note FROM cash_transfers WHERE {from_col}=%s OR {to_col}=%s ORDER BY transfer_date DESC LIMIT %s",
                (box_id, box_id, limit),
            )
            for row in r:
                d, f_id, t_id, amt, note = row
                direction = "تحويل وارد" if t_id == box_id else "تحويل صادر"
                sign = float(amt or 0) if t_id == box_id else -float(amt or 0)
                txns.append({
                    "date": d,
                    "type": direction,
                    "amount": sign,
                    "note": note or "",
                    "source": "تحويلات",
                })
    txns.sort(key=lambda x: x["date"] or datetime.min, reverse=True)
    return txns[:limit]


# ------------------------------------------------------------------------------
# UI
# ---------------------------------------------------------------------------
def show():
    st.set_page_config(page_title="الخزائن", page_icon="🏦", layout="wide")
    # RTL
    st.markdown(
        "<style>section.main, div.block-container{direction:rtl;} "
        "table, th, td{text-align:right;}</style>",
        unsafe_allow_html=True,
    )

    st.title("🏦 إدارة الخزائن")
    st.caption("الأرصدة محسوبة ديناميكياً من كل العمليات (رصيد افتتاحي + قبض - صرف - مصروفات + قروض + تحويلات واردة - تحويلات صادرة)")

    tabs = st.tabs(["📋 قائمة الخزائن", "➕ إضافة خزينة", "🔄 تحويل بين الخزائن", "🧮 حاسبة الخلايا"])

    # ---- Tab 1: list ----
    with tabs[0]:
        try:
            boxes = list_boxes(active_only=False)
        except Exception as e:
            st.error(f"تعذر تحميل الخزائن: {e}")
            boxes = []
        if not boxes:
            st.info("لا توجد خزائن بعد. أضف خزينة من تبويب 'إضافة خزينة'.")
        else:
            total = sum(b["current_balance"] for b in boxes)
            c1, c2, c3 = st.columns(3)
            c1.metric("عدد الخزائن", len(boxes))
            c2.metric("إجمالي الأرصدة", f"{total:,.2f}")
            c3.metric("أكبر خزينة", max((b["current_balance"] for b in boxes), default=0))
            df = pd.DataFrame(boxes)
            df = df[["id", "name", "opening_balance", "currency", "current_balance"]]
            df.columns = ["م", "الاسم", "الرصيد الافتتاحي", "العملة", "الرصيد الحالي"]
            st.dataframe(df, use_container_width=True, hide_index=True)

            st.subheader("حركات خزينة")
            sel = st.selectbox("اختر خزينة", [f"{b['id']} - {b['name']}" for b in boxes])
            if sel:
                bid = int(sel.split(" - ")[0])
                txns = box_transactions(bid)
                if not txns:
                    st.info("لا توجد حركات لهذه الخزينة.")
                else:
                    tdf = pd.DataFrame(txns)
                    tdf = tdf[["date", "type", "amount", "note", "source"]]
                    tdf.columns = ["التاريخ", "النوع", "المبلغ", "ملاحظات", "المصدر"]
                    st.dataframe(tdf, use_container_width=True, hide_index=True)

    # ---- Tab 2: add ----
    with tabs[1]:
        with st.form("add_box_form"):
            name = st.text_input("اسم الخزينة", placeholder="الخزينة الرئيسية")
            opening_raw = st.text_input("الرصيد الافتتاحي (يمكن كتابة معادلة مثل 1000+250*2)", value="0")
            currency = st.text_input("العملة", value="EGP")
            submitted = st.form_submit_button("💾 حفظ")
            if submitted:
                if not name.strip():
                    st.error("اسم الخزينة مطلوب")
                else:
                    opening = safe_eval(opening_raw, default=0.0)
                    try:
                        add_box(name.strip(), opening, currency.strip() or "EGP")
                        st.success(f"تم حفظ الخزينة '{name}' برصيد افتتاحي {opening:,.2f}")
                        st.rerun()
                    except Exception as e:
                        st.error(f"خطأ: {e}")

    # ---- Tab 3: transfer ----
    with tabs[2]:
        try:
            boxes = list_boxes(active_only=True)
        except Exception:
            boxes = []
        if len(boxes) < 2:
            st.info("تحتاج خزينتين على الأقل لإجراء تحويل.")
        else:
            with st.form("transfer_form"):
                names = {b["id"]: b["name"] for b in boxes}
                from_id = st.selectbox("من خزينة", list(names.keys()),
                                             format_func=lambda x: names[x])
                to_id = st.selectbox("إلى خزينة", list(names.keys()),
                                            format_func=lambda x: names[x])
                amt_raw = st.text_input("المبلغ (يدعم المعادلات مثل 500+50)", value="0")
                note = st.text_input("ملاحظات")
                submitted = st.form_submit_button("🔄 تحنفيذ التحويل")
                if submitted:
                    if from_id == to_id:
                        st.error("لا يمكن التحويل لنفس الخزينة")
                    else:
                        amt = safe_eval(amt_raw, default=0.0)
                        if amt <= 0:
                           st.error("المبلغ يجة أن يدعول أكبر من ص؁ر%)")
                        else:
                            try:
                               transfer_amount(from_id, to_id, amt, note)
                                st.success(f"�حويل {amt:,.2f} من '{names[from_id]}' إلى '{names[to_id]}'")
                               st.rerun()
                          except Exception as e:
                               st.error(f"خطأ: {e}")

    # ---- Tab 4: cell calculator demo ----
    with tabs[3]:
        st.markdown("#### 🧮 حاسبة المعادلات الحسابية")
        st.caption("اكتب أي معادلة حسابية في الخلية وسيتم حسابها تلقائياً عند الحفظ.")
        c1, c2 = st.columns(2)
        with c1:
            expr = st.text_input("معادلة", value="(100+200)*2 - 50")
            if st.button("احسب"):
                val = safe_eval(expr, default=None)
                if val is None:
                    st.error("معادلة غير صحيحة")
                else:
                    st.success(f"النتيجة: {val:,.2f}")
        with c2:
            st.markdown("**أمثلة مدعومة:**")
            st.code("3+3\n100*2.5\n(500-100)/4\n1000+250*2\n50%3")


if __name__ == "__main__":
    show()
