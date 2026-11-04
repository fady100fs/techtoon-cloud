# fix_encoding.py - إصلاح ترميز XAML + إنشاء appsettings.json
import os, shutil

PROJECT = r"D:\PROG\Accounting Prog\Techtoon Accounting C+"
VIEWS = os.path.join(PROJECT, "Techtoon.UI", "Views")
UI_DIR = os.path.join(PROJECT, "Techtoon.UI")

# 1) إنشاء appsettings.json
print("[1/3] Creating appsettings.json...")
appsettings_path = os.path.join(UI_DIR, "appsettings.json")
with open(appsettings_path, "w", encoding="utf-8") as f:
    f.write("""{
  "ConnectionStrings": {
    "DefaultConnection": "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"
  }
}""")
print(f"  OK: {appsettings_path}")

# انسخه لمجلد الإخراج
bin_dir = os.path.join(UI_DIR, "bin", "Debug", "net10.0-windows")
if os.path.isdir(bin_dir):
    shutil.copy2(appsettings_path, os.path.join(bin_dir, "appsettings.json"))
    print(f"  OK: copied to bin folder")

# 2) إصلاح ترميز XAML (إضافة XML declaration + BOM)
print("\n[2/3] Fixing XAML encoding...")
xaml_path = os.path.join(VIEWS, "InvoiceEditWindow.xaml")
if os.path.exists(xaml_path):
    with open(xaml_path, "rb") as f:
        raw = f.read()
    content = raw[3:].decode("utf-8") if raw.startswith(b'\xef\xbb\xbf') else raw.decode("utf-8", errors="replace")
    if not content.startswith("<?xml"):
        content = '<?xml version="1.0" encoding="utf-8"?>\n' + content
    with open(xaml_path, "w", encoding="utf-8-sig") as f:
        f.write(content)
    print("  OK: XAML re-saved with BOM + XML declaration")

# 3) إصلاح ترميز ملفات .cs
print("\n[3/3] Fixing .cs encoding...")
for fname in ["InvoiceEditWindow.xaml.cs", "InvoiceEditViewModel.cs"]:
    fpath = os.path.join(VIEWS, fname)
    if not os.path.exists(fpath):
        continue
    with open(fpath, "rb") as f:
        raw = f.read()
    content = raw[3:].decode("utf-8") if raw.startswith(b'\xef\xbb\xbf') else raw.decode("utf-8", errors="replace")
    with open(fpath, "w", encoding="utf-8-sig") as f:
        f.write(content)
    print(f"  OK: {fname}")

print("\n" + "=" * 50)
print("DONE! Now run:")
print("  dotnet build Techtoon.UI")
print("  dotnet run --project Techtoon.UI")
print("=" * 50)