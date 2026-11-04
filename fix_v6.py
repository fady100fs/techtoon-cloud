# fix_v6.py - المدفوعات قابلة للتعديل + ربط بالفواتير
import os, re, shutil

PROJECT = r"D:\PROG\Accounting Prog\Techtoon Accounting C+"
VIEWS = os.path.join(PROJECT, "Techtoon.UI", "Views")
VMS = os.path.join(PROJECT, "Techtoon.UI", "ViewModels")

print("=" * 60)
print("  Techtoon - Fix v6 (Python version)")
print("=" * 60)

# ============================================================
# 1) تحديث InvoiceEditViewModel.cs
# ============================================================
print("\n[1/3] Updating InvoiceEditViewModel.cs...")
vm_path = os.path.join(VIEWS, "InvoiceEditViewModel.cs")
shutil.copy2(vm_path, vm_path + ".bak_v6")
with open(vm_path, "r", encoding="utf-8-sig") as f:
    content = f.read()

# أضف InvoiceId و InvoiceNumber لـ InvoicePaymentRow
old_row = """public class InvoicePaymentRow : ViewModelBase
    {
        public int Id { get; set; }"""
new_row = """public class InvoicePaymentRow : ViewModelBase
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public string InvoiceDisplay { get { return string.IsNullOrEmpty(InvoiceNumber) ? "" : InvoiceNumber; } }"""
content = content.replace(old_row, new_row)

# أضف UnpaidInvoices collection
if "UnpaidInvoices" not in content:
    content = content.replace(
        "public ObservableCollection<InvoicePaymentRow> Payments { get; set; }",
        "public ObservableCollection<InvoicePaymentRow> Payments { get; set; }\n        public ObservableCollection<UnpaidInvoiceOption> UnpaidInvoices { get; set; }"
    )

# أضف تهيئة في الكونستركتور
if "UnpaidInvoices = new ObservableCollection" not in content:
    content = content.replace(
        "Payments = new ObservableCollection<InvoicePaymentRow>();",
        "Payments = new ObservableCollection<InvoicePaymentRow>();\n            UnpaidInvoices = new ObservableCollection<UnpaidInvoiceOption>();"
    )

# أضف دالة LoadUnpaidInvoicesForParty قبل GenerateSerial
if "LoadUnpaidInvoicesForParty" not in content:
    method = '''
public void LoadUnpaidInvoicesForParty(int partyId, string invoiceType)
        {
            UnpaidInvoices.Clear();
            if (partyId <= 0) return;
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string sql = @"SELECT id, invoice_number, total, paid_amount
                        FROM invoices 
                        WHERE party_id = @pid AND type = @type AND paid_amount < total
                        ORDER BY id DESC LIMIT 50";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("pid", partyId);
                        cmd.Parameters.AddWithValue("type", invoiceType);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                UnpaidInvoices.Add(new UnpaidInvoiceOption
                                {
                                    Id = r.IsDBNull(0) ? 0 : r.GetInt32(0),
                                    InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                                    Total = r.IsDBNull(2) ? 0 : Convert.ToDecimal(r.GetValue(2)),
                                    Paid = r.IsDBNull(3) ? 0 : Convert.ToDecimal(r.GetValue(3))
                                });
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private string GenerateSerial'''
    content = content.replace("private string GenerateSerial", method)

# أضف صنف UnpaidInvoiceOption قبل آخر }
if "class UnpaidInvoiceOption" not in content:
    extra = '''

    public class UnpaidInvoiceOption
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get { return Total - Paid; } }
        public string Display
        {
            get { return InvoiceNumber + " - Rem: " + Remaining.ToString("N2"); }
        }
    }
'''
    # أضف قبل آخر } في الملف
    last_brace = content.rfind("}")
    if last_brace > 0:
        content = content[:last_brace] + extra + "}"

with open(vm_path, "w", encoding="utf-8-sig") as f:
    f.write(content)
print("  OK: ViewModel updated")
# ============================================================
# 2) تحديث XAML - قسم المدفوعات قابل للتعديل + زر ربط
# ============================================================
print("\n[2/3] Updating XAML payments section...")
xaml_path = os.path.join(VIEWS, "InvoiceEditWindow.xaml")
shutil.copy2(xaml_path, xaml_path + ".bak_v6")
with open(xaml_path, "r", encoding="ascii") as f:
    xaml = f.read()

new_payments = '''<DataGrid x:Name="PaymentsGrid" Grid.Row="1" ItemsSource="{Binding Payments}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="All" HorizontalGridLinesBrush="#ECEFF1" VerticalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MaxHeight="180" CellEditEnding="PaymentsGrid_CellEditEnding" SelectionMode="Single" SelectionUnit="CellOrRowHeader" IsReadOnly="False" CanUserSortColumns="False">
                    <DataGrid.Columns>
                        <DataGridTextColumn x:Name="ColPayMethod" Width="100" Binding="{Binding Method}" IsReadOnly="False"/>
                        <DataGridTextColumn x:Name="ColPayAmount" Width="110" Binding="{Binding Amount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" IsReadOnly="False">
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="Padding" Value="6,0"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
                        <DataGridTextColumn x:Name="ColPayRef" Width="150" Binding="{Binding Reference, UpdateSourceTrigger=PropertyChanged}" IsReadOnly="False"/>
                        <DataGridTextColumn x:Name="ColPayInvLink" Width="160" Binding="{Binding InvoiceNumber}" IsReadOnly="True">
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="Foreground" Value="#00897B"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
                        <DataGridTemplateColumn x:Name="ColPayDate" Width="130">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBlock Text="{Binding PaymentDate, StringFormat=yyyy-MM-dd}" Padding="4,0" VerticalAlignment="Center"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                            <DataGridTemplateColumn.CellEditingTemplate>
                                <DataTemplate>
                                    <DatePicker SelectedDate="{Binding PaymentDate, UpdateSourceTrigger=PropertyChanged}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellEditingTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPayLink" Width="80">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button x:Name="BtnLinkInvoice" Content="Link" Background="#FB8C00" Foreground="White" BorderThickness="0" FontSize="11" Cursor="Hand" Padding="6,2" Margin="2" Click="BtnLinkInvoice_Click"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPayDel" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="#E53935" Foreground="White" BorderThickness="0" FontSize="12" Cursor="Hand" Padding="8,2" Margin="2" Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>'''

xaml = re.sub(r'(?s)<DataGrid x:Name="PaymentsGrid".*?</DataGrid>', new_payments, xaml, count=1, flags=re.DOTALL)

with open(xaml_path, "w", encoding="ascii") as f:
    f.write(xaml)
print("  OK: XAML updated (editable + link button)")
# ============================================================
# 3) تحديث code-behind - إضافة BtnLinkInvoice_Click + النصوص
# ============================================================
print("\n[3/3] Updating code-behind...")
cs_path = os.path.join(VIEWS, "InvoiceEditWindow.xaml.cs")
shutil.copy2(cs_path, cs_path + ".bak_v6")
with open(cs_path, "r", encoding="utf-8-sig") as f:
    cs = f.read()

# أضف BtnLinkInvoice_Click قبل آخر }
if "BtnLinkInvoice_Click" not in cs:
    method = '''

        // ربط الدفعة بفاتورة غير مدفوعة
        private void BtnLinkInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is InvoicePaymentRow pay)
                {
                    _vm.LoadUnpaidInvoicesForParty(_vm.PartyId, _vm.InvoiceType);
                    if (_vm.UnpaidInvoices.Count == 0)
                    {
                        System.Windows.MessageBox.Show(
                            "\u0644\u0627 \u062A\u0648\u062C\u062F \u0641\u0648\u0627\u062A\u064A\u0631 \u063A\u064A\u0631 \u0645\u062F\u0641\u0648\u0639\u0629 \u0644\u0647\u0630\u0627 \u0627\u0644\u0637\u0631\u0641",
                            "\u062A\u0646\u0628\u064A\u0647");
                        return;
                    }
                    var win = new System.Windows.Window
                    {
                        Title = "\u0627\u062E\u062A\u0631 \u0641\u0627\u062A\u0648\u0631\u0629",
                        Width = 400, Height = 350,
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                        Owner = this
                    };
                    var list = new System.Windows.Controls.ListBox { Margin = new Thickness(10), Height = 220 };
                    foreach (var inv in _vm.UnpaidInvoices)
                    {
                        list.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = inv.Display, Tag = inv });
                    }
                    var btnOk = new System.Windows.Controls.Button
                    {
                        Content = "\u062A\u0623\u0643\u064A\u062F",
                        Width = 100, Margin = new Thickness(10),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                    };
                    btnOk.Click += (s, ev) =>
                    {
                        if (list.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag is UnpaidInvoiceOption inv)
                        {
                            pay.InvoiceId = inv.Id;
                            pay.InvoiceNumber = inv.InvoiceNumber;
                            if (pay.Amount <= 0) pay.Amount = inv.Remaining;
                            win.Close();
                        }
                    };
                    var panel = new System.Windows.Controls.StackPanel();
                    panel.Children.Add(new System.Windows.Controls.TextBlock
                    {
                        Text = "\u0627\u062E\u062A\u0631 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629 \u0627\u0644\u0645\u0631\u0627\u062F \u0631\u0628\u0637\u0647\u0627:",
                        Margin = new Thickness(10),
                        FontWeight = FontWeights.SemiBold
                    });
                    panel.Children.Add(list);
                    panel.Children.Add(btnOk);
                    win.Content = panel;
                    win.ShowDialog();
                }
            }
            catch { }
        }
'''
    last_brace = cs.rfind("}")
    if last_brace > 0:
        cs = cs[:last_brace] + method + "\n    }\n}"

# أضف النصوص العربية للأعمدة الجديدة
if "ColPayInvLink.Header" not in cs:
    cs = cs.replace(
        'try { ColPayRef.Header = "\\u0645\\u0631\\u062C\\u0639"; } catch { }',
        'try { ColPayRef.Header = "\\u0645\\u0631\\u062C\\u0639"; } catch { }\n            try { ColPayInvLink.Header = "\\u0641\\u0627\\u062A\\u0648\\u0631\\u0629 \\u0645\\u0631\\u062A\\u0628\\u0637\\u0629"; } catch { }\n            try { ColPayLink.Header = "\\u0631\\u0628\\u0637"; } catch { }'
    )

# تأكد من وجود using System.Windows.Controls
if "using System.Windows.Controls;" not in cs:
    cs = "using System.Windows.Controls;\n" + cs

with open(cs_path, "w", encoding="utf-8-sig") as f:
    f.write(cs)
print("  OK: Code-behind updated (Link button + Arabic texts)")

print("\n" + "=" * 60)
print("  DONE! Now run:")
print("  dotnet build Techtoon.UI")
print("  dotnet run --project Techtoon.UI")
print("=" * 60)
