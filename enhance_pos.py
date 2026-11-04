# -*- coding: utf-8 -*-
"""Techtoon POS Enhancement - تطبيق كل التحسينات دفعة واحدة"""
import os, shutil, re

PROJECT = os.getcwd()
VIEWS = os.path.join(PROJECT, "Techtoon.UI", "Views")
CS_FILE = os.path.join(VIEWS, "InvoiceEditViewModel.cs")
XAML_FILE = os.path.join(VIEWS, "InvoiceEditWindow.xaml")
XAML_CS_FILE = os.path.join(VIEWS, "InvoiceEditWindow.xaml.cs")

print("=" * 60)
print("  Techtoon POS Enhancement")
print("  Project: " + PROJECT)
print("=" * 60)

# ============================================================
# 1) InvoiceEditViewModel.cs
# ============================================================
print("\n[1/3] Enhancing InvoiceEditViewModel.cs...")
if not os.path.exists(CS_FILE):
    print("  ERROR: File not found: " + CS_FILE)
    exit(1)

shutil.copy2(CS_FILE, CS_FILE + ".bak_pos")
with open(CS_FILE, "r", encoding="utf-8-sig") as f:
    content = f.read()

# إضافة متغيرات
if "_isSaving" not in content:
    content = content.replace(
        "private bool _isBusy = false;",
        "private bool _isBusy = false;\n        private bool _isSaving = false;"
    )
    print("  OK: Added _isSaving field")

# إضافة Commands
if "SaveDraftCommand" not in content:
    content = content.replace(
        "public ICommand CancelCommand { get; set; }",
        "public ICommand CancelCommand { get; set; }\n        public ICommand SaveDraftCommand { get; set; }\n        public ICommand CancelInvoiceCommand { get; set; }\n        public ICommand AddByBarcodeCommand { get; set; }"
    )
    print("  OK: Added new commands")

# تهيئة Commands
if "SaveDraftCommand = new" not in content:
    content = content.replace(
        "CancelCommand = new UiRelayCommand(_ => { DialogResult = false; RequestClose?.Invoke(); });",
        "CancelCommand = new UiRelayCommand(_ => { DialogResult = false; RequestClose?.Invoke(); });\n            SaveDraftCommand = new UiRelayCommand(_ => { _ = SaveAsDraftAsync(); }, _ => !IsBusy);\n            CancelInvoiceCommand = new UiRelayCommand(_ => { _ = CancelInvoiceAsync(); }, _ => Id > 0 && !IsBusy);\n            AddByBarcodeCommand = new UiRelayCommand(p => { _ = AddByBarcodeAsync(p?.ToString() or ""); });"
    )
    print("  OK: Initialized commands")

# إضافة Validate + AddByBarcode + SaveDraft + CancelInvoice
if "private bool Validate()" not in content:
    new_methods = r'''
        private bool Validate()
        {
            if (PartyId <= 0)
            {
                StatusMessage = "\u0627\u062E\u062A\u0631 \u0627\u0644\u0637\u0631\u0641";
                System.Windows.MessageBox.Show("\u0627\u0644\u0631\u062C\u0627\u0621 \u0627\u062E\u062A\u064A\u0627\u0631 \u0627\u0644\u0637\u0631\u0641", "\u062A\u062D\u0642\u0642");
                return false;
            }
            if (Lines.Count == 0 || Lines.All(l => l.ItemId <= 0))
            {
                StatusMessage = "\u0623\u0636\u0641 \u0628\u0646\u0648\u062F";
                System.Windows.MessageBox.Show("\u0627\u0644\u0631\u062C\u0627\u0621 \u0625\u0636\u0627\u0641\u0629 \u0635\u0646\u0641", "\u062A\u062D\u0642\u0642");
                return false;
            }
            foreach (var line in Lines)
            {
                if (line.ItemId > 0 && line.Quantity <= 0)
                {
                    StatusMessage = "\u0627\u0644\u0643\u0645\u064A\u0629 \u063A\u064A\u0631 \u0635\u062D\u064A\u062D\u0629";
                    return false;
                }
            }
            return true;
        }

        public async System.Threading.Tasks.Task<bool> AddByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return false;
            barcode = barcode.Trim();
            var item = _allItems.FirstOrDefault(it => (it.Barcode ?? "").Equals(barcode, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                StatusMessage = "\u0644\u0627 \u064A\u0648\u062C\u062F \u0635\u0646\u0641 \u0628\u0627\u0631\u0643\u0648\u062F: " + barcode;
                System.Windows.MessageBox.Show("\u0644\u0627 \u064A\u0648\u062C\u062F \u0635\u0646\u0641 \u0628\u0627\u0631\u0643\u0648\u062F: " + barcode, "\u062A\u0646\u0628\u064A\u0647");
                return false;
            }
            var existing = Lines.FirstOrDefault(l => l.ItemId == item.Id);
            if (existing != null)
            {
                existing.Quantity += 1;
                RefreshTotals();
                StatusMessage = "\u0632\u064A\u0648\u062F\u062A \u0627\u0644\u0643\u0645\u064A\u0629: " + item.Name;
            }
            else
            {
                var line = new InvoiceLineItem(_allItems);
                line.SelectItem(item);
                Lines.Add(line);
                RefreshTotals();
                StatusMessage = "\u0623\u0636\u064A\u0641: " + item.Name;
            }
            await System.Threading.Tasks.Task.CompletedTask;
            return true;
        }

        public async System.Threading.Tasks.Task SaveAsDraftAsync()
        {
            if (_isSaving) return;
            if (Lines.Count == 0 || Lines.All(l => l.ItemId <= 0))
            {
                System.Windows.MessageBox.Show("\u0644\u0627 \u064A\u0645\u0643\u0646 \u062D\u0641\u0638 \u0645\u0633\u0648\u062F\u0629 \u0641\u0627\u0631\u063A\u0629", "\u062A\u062D\u0642\u0642");
                return;
            }
            _isSaving = true;
            IsBusy = true;
            try
            {
                RefreshTotals();
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            if (Id > 0)
                            {
                                using (var cmd = new NpgsqlCommand("UPDATE invoices SET invoice_number=@sn, date=@dt, type=@tp, party_id=@pid, warehouse_id=@wid, subtotal=@st, discount_amount=@da, total=@tt, paid_amount=@pa, status='draft', updated_at=NOW() WHERE id=@id", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("id", Id);
                                    cmd.Parameters.AddWithValue("sn", SerialNumber);
                                    cmd.Parameters.AddWithValue("dt", InvoiceDate);
                                    cmd.Parameters.AddWithValue("tp", InvoiceType);
                                    cmd.Parameters.AddWithValue("pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                                    cmd.Parameters.AddWithValue("wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                                    cmd.Parameters.AddWithValue("st", Subtotal);
                                    cmd.Parameters.AddWithValue("da", DiscountAmount);
                                    cmd.Parameters.AddWithValue("tt", Total);
                                    cmd.Parameters.AddWithValue("pa", TotalPaid);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                using (var cmd = new NpgsqlCommand("INSERT INTO invoices (invoice_number, date, type, party_id, warehouse_id, subtotal, discount_amount, total, paid_amount, status, created_at, updated_at) VALUES (@sn, @dt, @tp, @pid, @wid, @st, @da, @tt, @pa, 'draft', NOW(), NOW()) RETURNING id", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("sn", SerialNumber);
                                    cmd.Parameters.AddWithValue("dt", InvoiceDate);
                                    cmd.Parameters.AddWithValue("tp", InvoiceType);
                                    cmd.Parameters.AddWithValue("pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                                    cmd.Parameters.AddWithValue("wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                                    cmd.Parameters.AddWithValue("st", Subtotal);
                                    cmd.Parameters.AddWithValue("da", DiscountAmount);
                                    cmd.Parameters.AddWithValue("tt", Total);
                                    cmd.Parameters.AddWithValue("pa", TotalPaid);
                                    var o = cmd.ExecuteScalar();
                                    if (o != null && o != DBNull.Value) Id = Convert.ToInt32(o);
                                }
                            }
                            using (var cmd = new NpgsqlCommand("DELETE FROM invoice_lines WHERE invoice_id=@id", conn, tran)) { cmd.Parameters.AddWithValue("id", Id); cmd.ExecuteNonQuery(); }
                            foreach (var line in Lines)
                            {
                                if (line.ItemId <= 0) continue;
                                using (var cmd = new NpgsqlCommand("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, discount, total) VALUES (@inv, @iid, @qty, @prc, @dsc, @tot)", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("inv", Id);
                                    cmd.Parameters.AddWithValue("iid", line.ItemId);
                                    cmd.Parameters.AddWithValue("qty", line.Quantity);
                                    cmd.Parameters.AddWithValue("prc", line.Price);
                                    cmd.Parameters.AddWithValue("dsc", line.Discount);
                                    cmd.Parameters.AddWithValue("tot", line.Total);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            tran.Commit();
                            StatusMessage = "\u062A\u0645 \u0627\u0644\u062D\u0641\u0638 \u0643\u0645\u0633\u0648\u062F\u0629 - ID: " + Id;
                            DialogResult = true;
                            RequestClose?.Invoke();
                        }
                        catch { try { tran.Rollback(); } catch { } throw; }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "\u062E\u0637\u0623: " + ex.Message;
                System.Windows.MessageBox.Show("\u062E\u0637\u0623 \u062D\u0641\u0638 \u0627\u0644\u0645\u0633\u0648\u062F\u0629:\n" + ex.Message, "\u062E\u0637\u0623");
            }
            finally { IsBusy = false; _isSaving = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        public async System.Threading.Tasks.Task CancelInvoiceAsync()
        {
            if (Id <= 0) return;
            var result = System.Windows.MessageBox.Show(
                "\u0647\u0644 \u062A\u0631\u064A\u062F \u0625\u0644\u063A\u0627\u0621 \u0647\u0630\u0647 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629\u061F",
                "\u062A\u0623\u0643\u064A\u062F",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;
            IsBusy = true;
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("UPDATE invoices SET status='cancelled', updated_at=NOW() WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                StatusMessage = "\u062A\u0645 \u0625\u0644\u063A\u0627\u0621 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629";
                DialogResult = true;
                RequestClose?.Invoke();
            }
            catch (Exception ex) { StatusMessage = "\u062E\u0637\u0623: " + ex.Message; }
            finally { IsBusy = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }

'''
    content = content.replace(
        "public async System.Threading.Tasks.Task SaveAsync()",
        new_methods + "public async System.Threading.Tasks.Task SaveAsync()"
    )
    print("  OK: Added Validate, AddByBarcode, SaveDraft, CancelInvoice")

# تحسين SaveAsync
if "if (!Validate()) return;" not in content:
    content = content.replace(
        "public async System.Threading.Tasks.Task SaveAsync()\n        {\n            IsBusy = true;",
        "public async System.Threading.Tasks.Task SaveAsync()\n        {\n            if (_isSaving) return;\n            if (!Validate()) return;\n            _isSaving = true;\n            IsBusy = true;"
    )
    print("  OK: Added Validate + _isSaving to SaveAsync")

if "finally { IsBusy = false; _isSaving = false; }" not in content:
    content = content.replace(
        "finally { IsBusy = false; }",
        "finally { IsBusy = false; _isSaving = false; }"
    )
    print("  OK: Added _isSaving reset in finally")

with open(CS_FILE, "w", encoding="utf-8-sig") as f:
    f.write(content)
print("  DONE: InvoiceEditViewModel.cs enhanced")

# ============================================================
# 2) InvoiceEditWindow.xaml - إضافة أزرار
# ============================================================
print("\n[2/3] Enhancing InvoiceEditWindow.xaml...")
if os.path.exists(XAML_FILE):
    shutil.copy2(XAML_FILE, XAML_FILE + ".bak_pos")
    with open(XAML_FILE, "r", encoding="ascii") as f:
        xaml = f.read()
    
    if 'BtnSaveDraft' not in xaml:
        old_buttons = '<Button x:Name="BtnSave" Style="{StaticResource SaveBtn}" Margin="0,0,8,0" Command="{Binding SaveCommand}"/>\n                    <Button x:Name="BtnCancel" Style="{StaticResource CancelBtn}" Command="{Binding CancelCommand}"/>'
        new_buttons = '<Button x:Name="BtnSave" Style="{StaticResource SaveBtn}" Margin="0,0,8,0" Command="{Binding SaveCommand}"/>\n                    <Button x:Name="BtnSaveDraft" Background="#FB8C00" Foreground="White" FontWeight="SemiBold" FontSize="13" Padding="18,10" BorderThickness="0" Cursor="Hand" Margin="0,0,8,0" Command="{Binding SaveDraftCommand}"/>\n                    <Button x:Name="BtnCancelInvoice" Background="#E53935" Foreground="White" FontWeight="SemiBold" FontSize="13" Padding="18,10" BorderThickness="0" Cursor="Hand" Margin="0,0,8,0" Command="{Binding CancelInvoiceCommand}"/>\n                    <Button x:Name="BtnCancel" Style="{StaticResource CancelBtn}" Command="{Binding CancelCommand}"/>'
        if old_buttons in xaml:
            xaml = xaml.replace(old_buttons, new_buttons)
            print("  OK: Added SaveDraft + CancelInvoice buttons")
        else:
            print("  WARN: Buttons pattern not found - skipping")
        
        with open(XAML_FILE, "w", encoding="ascii") as f:
            f.write(xaml)
    else:
        print("  SKIP: Buttons already exist")
    print("  DONE: InvoiceEditWindow.xaml enhanced")
else:
    print("  SKIP: XAML file not found")

# ============================================================
# 3) InvoiceEditWindow.xaml.cs - اختصارات + نصوص
# ============================================================
print("\n[3/3] Enhancing InvoiceEditWindow.xaml.cs...")
if os.path.exists(XAML_CS_FILE):
    shutil.copy2(XAML_CS_FILE, XAML_CS_FILE + ".bak_pos")
    with open(XAML_CS_FILE, "r", encoding="utf-8-sig") as f:
        cs = f.read()
    
    # إضافة نصوص عربية للأزرار
    if 'BtnSaveDraft.Content' not in cs:
        cs = cs.replace(
            'try { BtnCancel.Content = "\\u2716 \\u0625\\u0644\\u063A\\u0627\\u0621"; } catch { }',
            'try { BtnCancel.Content = "\\u2716 \\u0625\\u0644\\u063A\\u0627\\u0621"; } catch { }\n            try { BtnSaveDraft.Content = "\\uD83D\\uDCDD \\u062D\\u0641\\u0638 \\u0643\\u0645\\u0633\\u0648\\u062F\\u0629"; } catch { }\n            try { BtnCancelInvoice.Content = "\\u274C \\u0625\\u0644\\u063A\\u0627\\u0621 \\u0627\\u0644\\u0641\\u0627\\u062A\\u0648\\u0631\\u0629"; } catch { }'
        )
        print("  OK: Added Arabic text for new buttons")
    
    # إضافة اختصارات لوحة المفاتيح
    if 'Window_PreviewKeyDown' not in cs:
        cs = cs.replace(
            'InitializeComponent();',
            'InitializeComponent();\n            this.PreviewKeyDown += Window_PreviewKeyDown;'
        )
        
        keydown = r'''
        // اختصارات: F2=حفظ, F3=مسودة, F4=إلغاء, F9=بحث
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.F2) { if (_vm.SaveCommand.CanExecute(null)) { _vm.SaveCommand.Execute(null); e.Handled = true; } }
                else if (e.Key == Key.F3) { if (_vm.SaveDraftCommand.CanExecute(null)) { _vm.SaveDraftCommand.Execute(null); e.Handled = true; } }
                else if (e.Key == Key.F4) { if (_vm.CancelInvoiceCommand.CanExecute(null)) { _vm.CancelInvoiceCommand.Execute(null); e.Handled = true; } }
                else if (e.Key == Key.F9)
                {
                    if (LinesGrid.Items.Count > 0 && LinesGrid.ItemContainerGenerator.ContainerFromIndex(0) is DataGridRow row)
                    {
                        var sc = FindDescendant<ComboBox>(row, x => x.Name == "ItemSearchCombo");
                        sc?.Focus();
                        e.Handled = true;
                    }
                }
            }
            catch { }
        }

'''
        cs = cs.replace('private void SetArabicTexts()', keydown + 'private void SetArabicTexts()')
        print("  OK: Added keyboard shortcuts (F2/F3/F4/F9)")
    
    with open(XAML_CS_FILE, "w", encoding="utf-8-sig") as f:
        f.write(cs)
    print("  DONE: InvoiceEditWindow.xaml.cs enhanced")
else:
    print("  SKIP: xaml.cs file not found")

print("\n" + "=" * 60)
print("  ALL ENHANCEMENTS APPLIED!")
print("=" * 60)
print("\nNew features:")
print("  1. AddByBarcodeAsync - إضافة صنف بالباركود")
print("  2. SaveAsDraftAsync - حفظ كمسودة")
print("  3. CancelInvoiceAsync - إلغاء فاتورة")
print("  4. Validate - تحقق من المدخلات")
print("  5. _isSaving - منع تكرار الحفظ")
print("  6. F2/F3/F4/F9 - اختصارات لوحة المفاتيح")
print("\nNext: dotnet build Techtoon.UI")
