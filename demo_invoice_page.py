# demo_invoice_page.py
# صفحة فواتير تجريبية كاملة تُظهر النمط الصحيح لثلاثة أشياء معاً:
#   1) البحث بالكود / الباركود  (CodeSearch)
#   2) تفريغ كل الخانات والسلة بعد الحفظ (FormState)
#   3) تجنّب خطأ  '>' not supported between 'NoneType' and 'int'  (دالة num)
#
# للتجربة المنفصلة:   streamlit run demo_invoice_page.py
# بيانات الأصناف هنا وهمية: الخدمات رصيدها None عمداً لإظهار الخطأ وحلّه.
# لدمج النمط في صفحتك الحقيقية انسخ نفس الأسلوب (أو أرسل لي ملف صفحتك).
import sys
import traceback
from pathlib import Path

import streamlit as st

sys.path.append(str(Path(__file__).resolve().parent))
from code_search import CodeSearch, FormState

st.set_page_config(page_title="فاتورة تجريبية", page_icon="🧾", layout="wide")


# ----------------------------------------------------------
# بيانات وهمية (استبدلها بالاستعلام من قاعدة بياناتك)
# ----------------------------------------------------------
ITEMS = [
    {"code": "1001", "name": "فاتورة الكترونية", "sale_price": 50.0,  "purchase_price": 30.0, "stock": None, "is_service": True},
    {"code": "1002", "name": "إقرار قيمة مضافة",  "sale_price": 20.0,  "purchase_price": 10.0, "stock": None, "is_service": True},
    {"code": "1003", "name": "طباعة",             "sale_price": 2.0,   "purchase_price": 1.0,  "stock": None, "is_service": True},
    {"code": "1004", "name": "تقديم اسكان",       "sale_price": 200.0, "purchase_price": 120.0, "stock": None, "is_service": True},
    {"code": "2001", "name": "ورق A4 (دستة)",     "sale_price": 120.0, "purchase_price": 90.0, "stock": 15,   "is_service": False},
]
CUSTOMERS = ["فردى", "شركة النور", "مؤسسة الأمل"]
TYPES = {"sale (بيع)": "sale", "purchase (شراء)": "purchase"}


def num(value, default=0):
    """يحوّل None إلى رقم، فلا تنهار المقارنات:  num(item['stock']) > 0 """
    return default if value is None else value


# ----------------------------------------------------------
# حفظ الفاتورة (استبدل جسمها باستدعاء دالة الحفظ عندك)
# ----------------------------------------------------------
def save_invoice(invoice_type, customer, lines, discount_pct, tax_pct):
    by_name = {it["name"]: it for it in ITEMS}
    for ln in lines:
        item = by_name[ln["item_name"]]
        # الخدمات لا رصيد لها، وفحص المخزون للبضاعة فقط
        if invoice_type == "sale" and not item["is_service"]:
            if num(item["stock"]) < ln["quantity"]:
                raise ValueError(f"الرصيد غير كافٍ للصنف: {item['name']}")
    # ... هنا ينفَّذ الحفظ الفعلي في قاعدة البيانات ...


# ----------------------------------------------------------
# الصفحة
# ----------------------------------------------------------
fs = FormState("inv", cart_keys=("invoice_items",))
st.session_state.setdefault("invoice_items", [])

st.title("🧾 إنشاء الفواتير")

type_label = st.radio("نوع الفاتورة:", list(TYPES), key=fs.key("type"))
invoice_type = TYPES[type_label]
is_purchase = invoice_type == "purchase"
customer = st.selectbox("اختر العميل:", CUSTOMERS, key=fs.key("customer"))

st.divider()
st.subheader("أصناف الفاتورة")

cs = CodeSearch(
    ITEMS,
    prefix=fs.prefix(),
    code_field="code",
    name_field="name",
    price_field=(lambda it: it["purchase_price"]) if is_purchase else (lambda it: it["sale_price"]),
    qty_key=fs.key("qty"),
)
cs.render_input(autofocus=False)

col_item, col_qty, col_price = st.columns([3, 1, 1])
with col_item:
    label = st.selectbox("الصنف:", cs.labels, key=cs.item_key, on_change=cs.on_item_change)
cs.init_price(label)
with col_qty:
    qty = st.number_input("الكمية:", min_value=1, step=1, key=fs.key("qty"))
with col_price:
    price = st.number_input("السعر:", min_value=0.0, step=0.5, key=cs.price_key)

if st.button("➕ إضافة للفاتورة"):
    item = cs.by_label(label)
    if item is not None:
        st.session_state.invoice_items.append(
            {"item_name": item["name"], "quantity": int(qty), "price": float(price)}
        )
        st.rerun()

# ----------------------------------------------------------
# تفاصيل الفاتورة والإجماليات
# ----------------------------------------------------------
cart = st.session_state.invoice_items
if not cart:
    st.info("السلة فارغة. أضف أصنافاً لبدء الفاتورة.")
else:
    st.divider()
    st.subheader("تفاصيل الفاتورة")
    rows = [{**ln, "الإجمالي": ln["quantity"] * ln["price"]} for ln in cart]
    st.dataframe(rows)

    c1, c2 = st.columns(2)
    discount_pct = c1.number_input("خصم %:", min_value=0.0, max_value=100.0, key=fs.key("discount"))
    tax_pct = c2.number_input("ضريبة %:", min_value=0.0, max_value=100.0, key=fs.key("tax"))

    subtotal = sum(r["الإجمالي"] for r in rows)
    discount = subtotal * discount_pct / 100
    tax = (subtotal - discount) * tax_pct / 100
    net = subtotal - discount + tax

    m1, m2, m3, m4 = st.columns(4)
    m1.metric("المجموع", f"{subtotal:,.2f}")
    m2.metric("الخصم", f"{discount:,.2f}")
    m3.metric("الضريبة", f"{tax:,.2f}")
    m4.metric("الصافي", f"{net:,.2f}")

    st.divider()
    if st.button("💾 حفظ الفاتورة", type="primary"):
        try:
            save_invoice(invoice_type, customer, cart, discount_pct, tax_pct)
        except Exception as e:
            st.error(f"خطأ: {e}")
            st.code(traceback.format_exc())  # يُظهر الملف والسطر عند أي خطأ
        else:
            st.toast("✅ تم حفظ الفاتورة")
            fs.reset()  # يفرّغ السلة وكل الخانات ثم يعيد التشغيل
