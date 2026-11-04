# fix_cs.py - إصلاح نهائي لسلسلة الاتصال
import re, os, shutil

FILE = r"D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI\Views\InvoiceEditViewModel.cs"
CS = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"

if not os.path.exists(FILE):
    print("ERROR: File not found"); exit(1)

shutil.copy2(FILE, FILE + ".bak3")
with open(FILE, "r", encoding="utf-8-sig") as f:
    content = f.read()

# استبدل أي return داخل GetConnectionString + أي متغير defaultCs
# الطريقة: ابحث عن بداية الدالة ونهايتها (تتبع الأقواس)
start = content.find("public static string GetConnectionString")
if start < 0:
    print("ERROR: Method not found"); exit(1)

# أوجد بداية جسم الدالة
brace_start = content.index("{", start)
brace_count = 1
i = brace_start + 1
while i < len(content) and brace_count > 0:
    if content[i] == "{": brace_count += 1
    elif content[i] == "}": brace_count -= 1
    i += 1
brace_end = i  # الموضع بعد } الأخيرة

# حدد المسافة البادئة
line_start = content.rfind("\n", 0, start) + 1
indent = content[line_start:start]

# النص الجديد للدالة
new_method = (
    "public static string GetConnectionString()\n" +
    indent + "{\n" +
    indent + '    return "' + CS + '";\n' +
    indent + "}"
)

new_content = content[:line_start] + new_method + content[brace_end:]

with open(FILE, "w", encoding="utf-8") as f:
    f.write(new_content)

# تحقق
with open(FILE, "r", encoding="utf-8") as f:
    v = f.read()
n = v.count("ep-bitter-night")
print(f"OK: Connection string found {n} time(s)")

# اعرض مقطع الدالة
idx = v.find("public static string GetConnectionString")
print("\n--- Method preview ---")
print(v[idx:idx+200])
print("\nDONE - Now run: dotnet build Techtoon.UI")