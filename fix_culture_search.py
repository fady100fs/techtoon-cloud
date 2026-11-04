# 3) إضافة MessageBox تشخيصي لمعرفة عدد الأصناف المحملة
print("\n[3/3] Adding diagnostic MessageBox...")
vm_path = os.path.join(VIEWS, "InvoiceEditViewModel.cs")

if os.path.exists(vm_path):
    with open(vm_path, "r", encoding="utf-8-sig") as f:
        content = f.read()
    
    if "Items loaded:" not in content:
        # ابحث عن نهاية LoadItems
        idx = content.find("LoadItems()")
        if idx >= 0:
            ret_idx = content.find("return list;", idx)
            if ret_idx >= 0:
                insert = '\n            try { System.Windows.MessageBox.Show("Items loaded: " + list.Count.ToString(), "Debug - LoadItems"); } catch { }\n'
                content = content[:ret_idx] + insert + content[ret_idx:]
                with open(vm_path, "w", encoding="utf-8-sig") as f:
                    f.write(content)
                print("  OK: Diagnostic MessageBox added")
            else:
                print("  WARN: return list not found")
        else:
            print("  WARN: LoadItems not found")
    else:
        print("  SKIP: Already present")
else:
    print(f"  ERROR: ViewModel not found")

print("\n" + "=" * 60)
print("  DONE!")
print("  Now: dotnet build Techtoon.UI && dotnet run --project Techtoon.UI")
print("=" * 60)