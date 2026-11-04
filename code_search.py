# code_search.py
# بحث بالكود / الباركود في الفواتير والصفحات التي تختار صنفاً + تفريغ النماذج بعد الحفظ.
#
# طريقة العمل:
#   - اكتب جزءاً من الكود (أو امسح الباركود) ثم Enter.
#   - صنف واحد مطابق  → يُختار تلقائياً ويُملأ سعره.
#   - عدة أصناف مطابقة → تظهر قائمة بكل الأصناف التي يحتوي كودها على ما كتبته
#                         (المطابق تماماً أولاً)، وتختار منها بنقرة.
#   - لا نتائج في الأكواد → يُبحث في الأسماء.
#   - قائمة الأصناف نفسها تصبح "الكود — الاسم" فيعمل بحث الكتابة داخلها بالكود أيضاً.
import streamlit as st
import streamlit.components.v1 as components


def _get(obj, field, default=None):
    """يقرأ حقلاً من كائن أو قاموس، أو ينفّذ دالة إن كان field دالة."""
    if callable(field):
        try:
            return field(obj)
        except Exception:
            return default
    if isinstance(obj, dict):
        return obj.get(field, default)
    return getattr(obj, field, default)


class CodeSearch:
    def __init__(
        self,
        items,
        *,
        prefix="inv",
        code_field="code",
        name_field="name",
        price_field="price",
        label_fn=None,
        qty_key=None,
        on_add=None,
        scroll_after=8,
    ):
        self.items = list(items)
        # code_field: اسم حقل، أو قائمة حقول (مثل code و barcode)، أو فارغ
        if isinstance(code_field, (list, tuple)):
            self.code_fields = list(code_field)
        elif code_field:
            self.code_fields = [code_field]
        else:
            self.code_fields = []
        self.name_field = name_field
        self.price_field = price_field
        self.label_fn = label_fn
        self.qty_key = qty_key
        self.on_add = on_add  # دالة (item, qty) لإضافة الصنف للسلة مباشرة
        self.scroll_after = scroll_after  # عدد النتائج الذي بعده تصبح القائمة قابلة للتمرير

        # مفاتيح الأدوات (مفاتيح session_state)
        self.item_key = f"{prefix}_item"
        self.price_key = f"{prefix}_price"
        self.code_key = f"{prefix}_code"
        self.msg_key = f"{prefix}_code_msg"
        self.query_key = f"{prefix}_query"
        self.pick_key = f"{prefix}_pick"
        self._sig_key = f"{prefix}_price_sig"

    # ------------------------------------------------------
    # الأكواد ونص الخيار
    # ------------------------------------------------------
    def codes(self, item):
        out = []
        for f in self.code_fields:
            v = _get(item, f, None)
            if v is not None and str(v).strip():
                out.append(str(v).strip())
        return out

    def _name(self, item):
        return str(_get(item, self.name_field, "") or "")

    def label(self, item):
        """نص الخيار: 'الكود — الاسم' (أو الاسم فقط إن لم يكن للصنف كود)."""
        if self.label_fn:
            return self.label_fn(item)
        codes = self.codes(item)
        return f"{codes[0]} — {self._name(item)}" if codes else self._name(item)

    @property
    def labels(self):
        """خيارات القائمة. استخدمها كما هي في st.selectbox حتى تتطابق."""
        return [self.label(it) for it in self.items]

    def by_label(self, label):
        """يرجع الصنف المقابل لنص الخيار المختار (أو None)."""
        for it in self.items:
            if self.label(it) == label:
                return it
        return None

    def _price(self, item):
        try:
            return float(_get(item, self.price_field, 0) or 0)
        except (TypeError, ValueError):
            return 0.0

    # ------------------------------------------------------
    # البحث: كل الأصناف التي يحتوي كودها على النص
    # ------------------------------------------------------
    def matches(self, text):
        """قائمة الأصناف المطابقة، مرتبة: تطابق تام ← يبدأ بالنص ← يحتويه.
        إن لم يوجد أي كود مطابق يُبحث في الأسماء."""
        text = (text or "").strip().lower()
        if not text:
            return []

        ranked = []  # (الترتيب، الصنف)
        for it in self.items:
            codes = [c.lower() for c in self.codes(it)]
            if any(c == text for c in codes):
                ranked.append((0, it))
            elif any(c.startswith(text) for c in codes):
                ranked.append((1, it))
            elif any(text in c for c in codes):
                ranked.append((2, it))

        if ranked:
            ranked.sort(key=lambda r: r[0])  # الترتيب مستقر: يحافظ على ترتيب الأصناف الأصلي
            return [it for _, it in ranked]

        return [it for it in self.items if text in self._name(it).lower()]

    def find(self, text):
        """توافق مع الاستخدام القديم: (صنف وحيد أو None، رسالة)."""
        found = self.matches(text)
        if len(found) == 1:
            return found[0], ""
        if len(found) > 1:
            return None, f"⚠️ يوجد {len(found)} أصناف مطابقة"
        return None, "❌ الكود غير موجود" if (text or "").strip() else ""

    # ------------------------------------------------------
    # اختيار صنف (مشترك بين البحث والقائمة)
    # ------------------------------------------------------
    def _choose(self, item):
        if self.on_add:  # إضافة مباشرة للسلة بكمية 1
            self.on_add(item, 1)
            msg = f"✅ تمت إضافة: {self.label(item)}"
        else:  # اختيار الصنف في القائمة (السعر يُحدَّث عبر sync_price)
            st.session_state[self.item_key] = self.label(item)
            if self.qty_key:
                st.session_state[self.qty_key] = 1
            msg = f"✅ تم اختيار: {self.label(item)}"

        st.session_state[self.msg_key] = msg
        st.session_state[self.code_key] = ""   # تفريغ الحقل للصنف التالي
        st.session_state.pop(self.query_key, None)
        st.session_state[self.pick_key] = None

    # ------------------------------------------------------
    # callbacks (تُنفَّذ قبل إعادة رسم الصفحة، فيصح فيها تعديل قيم الأدوات)
    # ------------------------------------------------------
    def _on_code(self):
        text = (st.session_state.get(self.code_key, "") or "").strip()
        st.session_state[self.pick_key] = None

        if not text:
            st.session_state.pop(self.query_key, None)
            return

        found = self.matches(text)
        if len(found) == 1:
            self._choose(found[0])
        elif len(found) > 1:
            st.session_state[self.query_key] = text  # تظهر قائمة النتائج (والنص يبقى للتعديل)
        else:
            st.session_state.pop(self.query_key, None)
            st.session_state[self.msg_key] = "❌ الكود غير موجود"
            st.session_state[self.code_key] = ""

    def _on_pick(self):
        item = self.by_label(st.session_state.get(self.pick_key))
        if item is not None:
            self._choose(item)

    def _clear_query(self):
        st.session_state.pop(self.query_key, None)
        st.session_state[self.code_key] = ""
        st.session_state[self.pick_key] = None

    def sync_price(self, label, signature=None):
        """استدعِها بعد st.selectbox وقبل st.number_input للسعر.
        تملأ السعر كلما تغيّر (الصنف أو signature)، مثل نوع الفاتورة بيع/شراء،
        وتترك السعر كما عدّله المستخدم بينما لا يتغير شيء."""
        sig = (label, signature)
        if st.session_state.get(self._sig_key) != sig:
            item = self.by_label(label)
            st.session_state[self.price_key] = self._price(item) if item is not None else 0.0
            st.session_state[self._sig_key] = sig

    # ------------------------------------------------------
    # توافق مع الاستخدام القديم
    # ------------------------------------------------------
    def init_price(self, label):
        self.sync_price(label)

    def on_item_change(self):
        self.sync_price(st.session_state.get(self.item_key))

    # ------------------------------------------------------
    # الواجهة
    # ------------------------------------------------------
    def _results_box(self, n):
        if n > self.scroll_after:
            try:
                return st.container(height=300)  # قائمة قابلة للتمرير
            except TypeError:
                pass
        return st.container()

    def render_input(self, label="🔎 بحث بالكود / الباركود", autofocus=False):
        st.text_input(
            label,
            key=self.code_key,
            on_change=self._on_code,
            placeholder="اكتب الكود أو جزءاً منه ثم اضغط Enter",
        )

        msg = st.session_state.pop(self.msg_key, "")  # تظهر مرة واحدة
        if msg:
            if msg.startswith("✅"):
                st.success(msg)
            else:
                st.warning(msg)

        # قائمة كل الأصناف التي يحتوي كودها على النص المكتوب
        query = st.session_state.get(self.query_key, "")
        if query:
            found = self.matches(query)
            if found:
                head, close = st.columns([6, 1])
                head.caption(f"🔎 {len(found)} صنف يطابق «{query}» — اختر الصنف المطلوب:")
                close.button("✖ إغلاق", key=f"{self.pick_key}_close", on_click=self._clear_query)
                with self._results_box(len(found)):
                    st.radio(
                        "نتائج البحث",
                        [self.label(it) for it in found],
                        index=None,
                        key=self.pick_key,
                        on_change=self._on_pick,
                        label_visibility="collapsed",
                    )
            else:
                st.session_state.pop(self.query_key, None)

        if autofocus:  # مفيد مع قارئ الباركود (قد يسحب التركيز من حقول أخرى)
            components.html(
                f"""<script>
                const el = window.parent.document.querySelector('input[aria-label="{label}"]');
                if (el) el.focus();
                </script>""",
                height=0,
            )


# ==========================================================
# تفريغ كل خانات النموذج بعد الحفظ
# ==========================================================
class FormState:
    """يعيد كل الخانات فارغة بعد الحفظ.

    الفكرة: كل مفتاح أداة يمرّ عبر fs.key("اسم") ويحمل رقم نسخة. بعد الحفظ
    يزيد الرقم فتُرسم كل الأدوات من جديد بقيمها الافتراضية، ولا نحتاج لتعديل
    قيم الأدوات بعد رسمها (وهو ما يمنعه Streamlit).

        fs = FormState("inv", cart_keys=("invoice_items",))
        qty = st.number_input("الكمية:", key=fs.key("qty"))
        cs  = CodeSearch(items, prefix=fs.prefix(), qty_key=fs.key("qty"))
        ...
        fs.reset()      # يفرّغ السلة والخانات ثم st.rerun()
    """

    def __init__(self, name="inv", cart_keys=()):
        self.name = name
        self.cart_keys = cart_keys  # مفاتيح session_state التي تحمل سلة الفاتورة
        self._ver_key = f"_{name}_form_ver"

    @property
    def version(self):
        return st.session_state.setdefault(self._ver_key, 0)

    def key(self, field):
        """مفتاح أداة مرتبط برقم النسخة الحالية."""
        return f"{self.name}_{field}_{self.version}"

    def prefix(self):
        """بادئة تُمرَّر إلى CodeSearch(prefix=...)."""
        return f"{self.name}{self.version}"

    def reset(self, rerun=True):
        st.session_state[self._ver_key] = self.version + 1
        for k in self.cart_keys:
            val = st.session_state.get(k)
            if isinstance(val, list):
                st.session_state[k] = []
            elif isinstance(val, dict):
                st.session_state[k] = {}
            else:
                st.session_state.pop(k, None)
        if rerun:
            st.rerun()
