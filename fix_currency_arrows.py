# -*- coding: utf-8 -*-
"""fix_currency_arrows.py - إصلاح currency_id + الأسهم"""
import os, re

VM_FILE = r"D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI\Views\InvoiceEditViewModel.cs"
CS_FILE = r"D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI\Views\InvoiceEditWindow.xaml.cs"
XAML_FILE = r"D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI\Views\InvoiceEditWindow.xaml"

print("=" * 60)
print("  Fix: currency_id + Arrow Keys")
print("=" * 60)

# ============================================================
# 1) إصلاح currency_id في InvoiceEditViewModel.cs
# ============================================================
print("\n[1/3] Fixing currency_id...")
with open(VM_FILE, "r", encoding="utf-8-sig") as f:
    content = f.read()

# ابحث عن كل جمل INSERT INTO invoices وأضف currency_id
# نمط 1: INSERT INTO invoices (invoice_number, ... status, created_at, updated_at)
content = content.replace(
    "(invoice_number, date, type, party_id, warehouse_id, notes, subtotal, discount_amount, total, paid_amount, status, created_at, updated_at)",
    "(invoice_number, date, type, party_id, warehouse_id, notes, subtotal, discount_amount, total, paid_amount, status, currency_id, created_at, updated_at)"
)

# نمط 2: INSERT INTO invoices (invoice_number, ... status, created_at, updated_at) بدون notes
content = content.replace(
    "(invoice_number, date, type, party_id, warehouse_id, subtotal, discount_amount, total, paid_amount, status, created_at, updated_at)",
    "(invoice_number, date, type, party_id, warehouse_id, subtotal, discount_amount, total, paid_amount, status, currency_id, created_at, updated_at)"
)

# ابحث عن VALUES وأضف , 1 قبل NOW(), NOW()
content = content.replace(
    "@pa, @status, NOW(), NOW()) RETURNING id",
    "@pa, @status, 1, NOW(), NOW()) RETURNING id"
)
content = content.replace(
    "@pa, 'draft', NOW(), NOW()) RETURNING id",
    "@pa, 'draft', 1, NOW(), NOW()) RETURNING id"
)

# ابحث عن UPDATE invoices وأضف currency_id=1
content = content.replace(
    "paid_amount=@pa,",
    "paid_amount=@pa, currency_id=1,"
)
# لكن تجنب التكرار لو تم تطبيقه مرتين
content = content.replace("currency_id=1, currency_id=1,", "currency_id=1,")

with open(VM_FILE, "w", encoding="utf-8-sig") as f:
    f.write(content)

# تحقق
with open(VM_FILE, "r", encoding="utf-8-sig") as f:
    verify = f.read()
count = verify.count("currency_id")
print(f"  OK: currency_id appears {count} times in file")

# ============================================================
# 2) إصلاح الأسهم في InvoiceEditWindow.xaml.cs
# ============================================================
print("\n[2/3] Fixing arrow keys in code-behind...")
with open(CS_FILE, "r", encoding="utf-8-sig") as f:
    cs = f.read()

# ابحث عن LinesGrid_PreviewKeyDown (فارغة أو موجودة) واستبدلها
pattern = r'private void LinesGrid_PreviewKeyDown\(object sender, KeyEventArgs e\)\s*\{[^}]*\}'
replacement = """private void LinesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // منع DataGrid من التقاط الأسهم عندما يكون التركيز على ComboBox البحث
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter || e.Key == Key.Escape)
            {
                var focused = Keyboard.FocusedElement as DependencyObject;
                while (focused != null)
                {
                    if (focused is ComboBox cb && cb.Name == "ItemSearchCombo")
                    {
                        e.Handled = true;
                        return;
                    }
                    focused = VisualTreeHelper.GetParent(focused);
                }
            }
        }"""

cs_new = re.sub(pattern, replacement.strip(), cs, count=1, flags=re.DOTALL)

if cs_new != cs:
    with open(CS_FILE, "w", encoding="utf-8-sig") as f:
        f.write(cs_new)
    print("  OK: LinesGrid_PreviewKeyDown replaced")
else:
    print("  WARN: Pattern not found - trying alternate")
    # لو لم يجده، أضفه قبل آخر }
    if "LinesGrid_PreviewKeyDown" not in cs:
        last_brace = cs.rfind("}")
        if last_brace > 0:
            cs = cs[:last_brace] + "\n        " + replacement + "\n    }\n}"
            with open(CS_FILE, "w", encoding="utf-8-sig") as f:
                f.write(cs)
            print("  OK: LinesGrid_PreviewKeyDown added")
    else:
        print("  SKIP: Already exists")

# ============================================================
# 3) التحقق من XAML - PreviewKeyDown على ComboBox
# ============================================================
print("\n[3/3] Verifying XAML has PreviewKeyDown on ComboBox...")
with open(XAML_FILE, "r", encoding="ascii") as f:
    xaml = f.read()

if "PreviewKeyDown" not in xaml:
    # أضف PreviewKeyDown للـ ComboBox
    xaml = xaml.replace(
        'SelectionChanged="ItemSearchCombo_SelectionChanged"',
        'SelectionChanged="ItemSearchCombo_SelectionChanged" PreviewKeyDown="ItemSearchCombo_PreviewKeyDown"'
    )
    with open(XAML_FILE, "w", encoding="ascii") as f:
        f.write(xaml)
    print("  OK: Added PreviewKeyDown to ComboBox in XAML")
elif "ItemSearchCombo_PreviewKeyDown" in xaml:
    print("  OK: PreviewKeyDown already attached to ComboBox")
else:
    # PreviewKeyDown موجود لكن ليس للـ ComboBox
    xaml = xaml.replace(
        'SelectionChanged="ItemSearchCombo_SelectionChanged"',
        'SelectionChanged="ItemSearchCombo_SelectionChanged" PreviewKeyDown="ItemSearchCombo_PreviewKeyDown"'
    )
    with open(XAML_FILE, "w", encoding="ascii") as f:
        f.write(xaml)
    print("  OK: Added ItemSearchCombo_PreviewKeyDown to ComboBox")

# تحقق من PreviewKeyDown على DataGrid
if "PreviewKeyDown" not in xaml or "LinesGrid" not in xaml.split("PreviewKeyDown")[0][-200:]:
    xaml = xaml.replace(
        'MinHeight="180">',
        'MinHeight="180" PreviewKeyDown="LinesGrid_PreviewKeyDown">'
    )
    with open(XAML_FILE, "w", encoding="ascii") as f:
        f.write(xaml)
    print("  OK: Added PreviewKeyDown to DataGrid")

print("\n" + "=" * 60)
print("  DONE! Now: dotnet build Techtoon.UI")
print("=" * 60)
