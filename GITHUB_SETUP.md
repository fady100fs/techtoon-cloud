# 📱 دليل الرفع على GitHub والوصول من الموبايل

## الفكرة أولًا (مهم)

بياناتك **ليست في GitHub ولا في الموبايل** — بياناتك في **Neon PostgreSQL** السحابية (نفس `DATABASE_URL` الموجود في ملف `.env` عندك).

```
جهاز الكمبيوتر ( launcher.py )  ──┐
                                  ├──►  Neon PostgreSQL (البيانات الحقيقية)
الموبايل / Streamlit Cloud  ──────┘
```

عندما ترفع **الكود** على GitHub وتربطه بـ Streamlit Cloud وتضع `DATABASE_URL` في الـ Secrets:
- البرنامج يفتح من الموبايل بنفس الخصائص المعدلة (الترحيل الجديد، تلميح المخزون، لوحة التحكم...)
- وبنفس البيانات تماماً (أي فاتورة تُدخل من الموبايل تظهر فوراً على الكمبيوتر والعكس)
- لا يحتاج أي تعديل على قاعدة البيانات — الحساب 2102 للضريبة يُنشأ تلقائياً عند أول فاتورة بضريبة

---

## الخطوة 1 — رفع الكود إلى GitHub

### أ) من VS Code Terminal (الأسهل)

```bash
# 1. ادخل مجلد المشروع
cd "D:\PROG\Accounting Prog\Techtoon Accounting"

# 2. شغّل سكربت تطبيق التعديلات أولاً (لو لم تفعل)
python "مسار_حزمة_التعديلات\apply_patch.py" --target "D:\PROG\Accounting Prog\Techtoon Accounting"

# 3. هيّئ git (أول مرة فقط)
git init
git config user.name "اسمك"
git config user.email "بريدك@example.com"

# 4. أضف كل الملفات (ملف .gitignore سيستبعد الأسرار تلقائياً)
git add .
git status   # ← تأكد أن .env و .sessions.json غير موجودين في القائمة!

# 5. أول commit
git commit -m "Techtoon Accounting v2 - إصلاحات + ترحيل متوازن + تلميح المخزون"

# 6. أنشئ مستودعاً جديداً على github.com باسم مثلاً: techtoon-accounting
#    (Private مستحسن) ثم اربطه وارفع:
git branch -M main
git remote add origin https://github.com/اسمك/techtoon-accounting.git
git push -u origin main
```

### ب) أو من VS Code الواجهة الرسومية
تبويب **Source Control** → **Initialize Repository** → Stage All → Commit → **Publish Branch**.

> ⚠️ ملاحظة أمنية: ملف `.gitignore` المرفق يستبعد `.env` و `.sessions.json` و `.login_attempts.json` —
> هذه الملفات فيها أسرار/جلسات **لا يجب أن تُرفع على GitHub أبداً**.
> (ملاحظة: النسخة السابقة عندك كانت تحتوي `.sessions.json` بتوكنات دخول حية — لا ترفعها).

---

## الخطوة 2 — النشر على Streamlit Cloud (مجاني)

1. ادخل **share.streamlit.io** وسجّل بحساب GitHub
2. **Create app** → **Deploy a public app from GitHub**
3. اختر المستودع `techtoon-accounting` والفرع `main`
   - Main file path: `app.py`
   - App URL: اختر اسماً مثل `techtoon-accounting`
4. قبل الضغط على Deploy: **Advanced settings → Secrets** والصق:

```toml
DATABASE_URL = "postgresql://user:password@host/neondb?sslmode=require"
```

(انسخه حرفياً من ملف `.env` المحلي عندك — نفس الرابط)

5. اضغط **Deploy** وانتظر 3-5 دقائق

> لو ظهر خطأ اتصال بالقاعدة، تأكد أن رابط Neon يحتوي `?sslmode=require`
> وأن مستخدم Neon مفعّل عليه الوصول من أي IP (إعداد Neon الافتراضي يسمح).

---

## الخطوة 3 — الفتح من الموبايل

1. افتح رابط التطبيق من متصفح الموبايل:
   `https://اسمك-techtoon-accounting.streamlit.app`
2. سجّل دخول بنفس مستخدم/كلمة مرور البرنامج
3. من قائمة المتصفح: **إضافة إلى الشاشة الرئيسية** — يصبح مثل تطبيق بأيقونة

## الخطوة 4 — أي تعديل مستقبلي

```bash
git add .
git commit -m "وصف التعديل"
git push
```
Streamlit Cloud يكتشف التحديث ويعيد النشر تلقائياً خلال دقيقة.

---

## 📄 صفحة 99 (فحص الاتصال)

الصفحة `pages/99_🩺_فحص_الاتصال.py` موجودة كنبض دوري للاتصال بـ Neon عندما يكون
التطبيق على Streamlit Cloud — لا تشارك رابطها مع العملاء (كما هو مكتوب داخلها).

## ❓ أسئلة شائعة

**هل الموبايل يبطّئ النظام؟** لا — القراءة على السحابة مباشرة من Neon، وأداء الواجهة يعتمد على الإنترنت فقط.

**هل أستطيع إدخال فواتير من الموبايل؟** نعم، كل الصلاحيات والصفحات تعمل حسب دور المستخدم، بما فيها تلميح المخزون الجديد.

**هل بيانات سطح المكتب والموبايل متزامنة؟** نعم — نفس قاعدة Neon مباشرة. نسخة سطح المكتب المحلية (SQLite Mirror) تتزامن تلقائياً بعد كل كتابة كما في السابق.
