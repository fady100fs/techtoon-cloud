# fix_v6.ps1 - المدفوعات قابلة للتعديل + ربط بالفواتير
 $ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
 $ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'
 $VMsDir = Join-Path $ProjectPath 'Techtoon.UI\ViewModels'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Fix v6 - Editable Payments + Invoice Link" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
# ============================================================
# 1) تحديث InvoiceEditViewModel.cs
#    - إضافة InvoiceId للدفعات
#    - دالة LoadUnpaidInvoicesForParty
#    - إضافة UnpaidInvoices collection
# ============================================================
Write-Host "`n[1/3] Updating InvoiceEditViewModel.cs..." -ForegroundColor Yellow
 $vmPath = Join-Path $ViewsDir 'InvoiceEditViewModel.cs'
Copy-Item $vmPath ($vmPath + '.bak_v6') -Force
 $content = Get-Content $vmPath -Raw -Encoding UTF8

# إضافة InvoiceId و InvoiceNumber لـ InvoicePaymentRow
 $oldRow = 'public class InvoicePaymentRow : ViewModelBase
    {
        public int Id { get; set; }'
 $newRow = 'public class InvoicePaymentRow : ViewModelBase
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public string InvoiceDisplay { get { return string.IsNullOrEmpty(InvoiceNumber) ? "" : InvoiceNumber; } }'
 $content = $content.Replace($oldRow, $newRow)

# إضافة UnpaidInvoices collection و SelectedPartyUnpaidInvoices
if ($content -notmatch 'UnpaidInvoices') {
    $old = 'public ObservableCollection<InvoicePaymentRow> Payments { get; set; }'
    $new = 'public ObservableCollection<InvoicePaymentRow> Payments { get; set; }
        public ObservableCollection<UnpaidInvoiceOption> UnpaidInvoices { get; set; }'
    $content = $content.Replace($old, $new)
}

# إضافة تهيئة UnpaidInvoices في الكونستركتور
if ($content -notmatch 'UnpaidInvoices = new ObservableCollection') {
    $old = 'Payments = new ObservableCollection<InvoicePaymentRow>();'
    $new = 'Payments = new ObservableCollection<InvoicePaymentRow>();
            UnpaidInvoices = new ObservableCollection<UnpaidInvoiceOption>();'
    $content = $content.Replace($old, $new)
}

# إضافة صنف UnpaidInvoiceOption ودالة LoadUnpaidInvoicesForParty قبل آخر }
if ($content -notmatch 'class UnpaidInvoiceOption') {
    $insertCode = @'

    public class UnpaidInvoiceOption
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get { return Total - Paid; } }
        public string Display
        {
            get
            {
                return InvoiceNumber + " - Rem: " + Remaining.ToString("N2");
            }
        }
    }
}
'@
    # أضف قبل آخر } في الملف
    $lastBrace = $content.LastIndexOf('}')
    if ($lastBrace -gt 0) {
        $content = $content.Substring(0, $lastBrace) + $insertCode.TrimEnd('}') + "`n}"
    }
}

# إضافة دالة LoadUnpaidInvoicesForParty بعد LoadWarehouses
if ($content -notmatch 'LoadUnpaidInvoicesForParty') {
    $marker = 'private string GenerateSerial'
    $idx = $content.IndexOf($marker)
    if ($idx -gt 0) {
        $insertMethod = @'
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

        private string GenerateSerial'@
        $content = $content.Substring(0, $idx) + $insertMethod + $content.Substring($idx)
    }
}

# تحديث دالة SaveAsync لحفظ InvoiceId في الدفعات
 $oldPaySave = 'string paySql = @"INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at)
                            VALUES (@inv, @mth, @amt, @ref, @pdt)";'
 $newPaySave = 'string paySql = @"INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at)
                            VALUES (@inv, @mth, @amt, @ref, @pdt)";
                        // إذا كانت الدفعة مرتبطة بفاتورة أخرى، سجلها أيضاً
                        if (pay.InvoiceId > 0 && pay.InvoiceId != Id)
                        {
                            // الدفعة مرتبطة بفاتورة سابقة - حدّث مدفوعاتها
                            string updSql = "UPDATE invoices SET paid_amount = paid_amount + @amt WHERE id = @pid";
                            using (var upCmd = new NpgsqlCommand(updSql, conn))
                            {
                                upCmd.Parameters.AddWithValue("amt", pay.Amount);
                                upCmd.Parameters.AddWithValue("pid", pay.InvoiceId);
                                upCmd.ExecuteNonQuery();
                            }
                        }'
 $content = $content.Replace($oldPaySave, $newPaySave)

[System.IO.File]::WriteAllText($vmPath, $content, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: ViewModel updated (InvoiceId + UnpaidInvoices)" -ForegroundColor Green
# ============================================================
# 2) تحديث InvoiceEditWindow.xaml
#    - استبدال DataGrid المدفوعات بـ StackPanel من الحقول القابلة للتعديل
#    - إضافة ComboBox لربط الدفعة بفاتورة غير مدفوعة
# ============================================================
Write-Host "`n[2/3] Updating XAML payments section..." -ForegroundColor Yellow
 $xamlPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml'
 $content = Get-Content $xamlPath -Raw -Encoding ASCII

# استبدال DataGrid المدفوعات بالكامل
 $pattern = '(?s)<DataGrid x:Name="PaymentsGrid".*?</DataGrid>'

 $newPayments = @'
<DataGrid x:Name="PaymentsGrid" Grid.Row="1" ItemsSource="{Binding Payments}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="All" HorizontalGridLinesBrush="#ECEFF1" VerticalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MaxHeight="180" CellEditEnding="PaymentsGrid_CellEditEnding" SelectionMode="Single" SelectionUnit="CellOrRowHeader" IsReadOnly="False" CanUserSortColumns="False">
                    <DataGrid.Columns>
                        <DataGridTextColumn x:Name="ColPayMethod" Width="120" Binding="{Binding Method}" IsReadOnly="False"/>
                        <DataGridTextColumn x:Name="ColPayAmount" Width="120" Binding="{Binding Amount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" IsReadOnly="False">
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="Padding" Value="6,0"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
                        <DataGridTextColumn x:Name="ColPayRef" Width="160" Binding="{Binding Reference, UpdateSourceTrigger=PropertyChanged}" IsReadOnly="False"/>
                        <DataGridTextColumn x:Name="ColPayInvLink" Width="180" Binding="{Binding InvoiceNumber, UpdateSourceTrigger=PropertyChanged}" IsReadOnly="True">
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="Foreground" Value="#00897B"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
                        <DataGridTemplateColumn x:Name="ColPayDate" Width="140">
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
                        <DataGridTemplateColumn x:Name="ColPayLink" Width="120">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button x:Name="BtnLinkInvoice" Content="Link" Background="#FB8C00" Foreground="White" BorderThickness="0" FontSize="11" Cursor="Hand" Padding="8,2" Margin="2" Click="BtnLinkInvoice_Click"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPayDel" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="#E53935" Foreground="White" BorderThickness="0" FontSize="12" Cursor="Hand" Padding="8,2" Margin="2"
                                            Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
'@

 $newContent = [regex]::Replace($content, $pattern, $newPayments.Trim(), [System.Text.RegularExpressions.RegexOptions]::Singleline)

if ($newContent -ne $content) {
    [System.IO.File]::WriteAllText($xamlPath, $newContent, [System.Text.ASCIIEncoding]::new())
    Write-Host "  OK: Payments section updated (editable + link button)" -ForegroundColor Green
} else {
    Write-Host "  ERROR: Pattern not found" -ForegroundColor Red
}
# ============================================================
# 3) تحديث InvoiceEditWindow.xaml.cs
#    - إضافة BtnLinkInvoice_Click
#    - تحديث النصوص العربية
# ============================================================
Write-Host "`n[3/3] Updating code-behind..." -ForegroundColor Yellow
 $csPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml.cs'
Copy-Item $csPath ($csPath + '.bak_v6') -Force
 $csContent = Get-Content $csPath -Raw -Encoding UTF8

# إضافة دالة BtnLinkInvoice_Click قبل آخر }
if ($csContent -notmatch 'BtnLinkInvoice_Click') {
    $insertMethod = @'

        // ربط الدفعة بفاتورة غير مدفوعة للعميل/المورد
        private void BtnLinkInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is InvoicePaymentRow pay)
                {
                    // حمّل الفواتير غير المدفوعة للطرف المحدد
                    _vm.LoadUnpaidInvoicesForParty(_vm.PartyId, _vm.InvoiceType);
                    
                    if (_vm.UnpaidInvoices.Count == 0)
                    {
                        System.Windows.MessageBox.Show(
                            "\u0644\u0627 \u062A\u0648\u062C\u062F \u0641\u0648\u0627\u062A\u064A\u0631 \u063A\u064A\u0631 \u0645\u062F\u0641\u0648\u0639\u0629 \u0644\u0647\u0630\u0627 \u0627\u0644\u0637\u0631\u0641",
                            "\u062A\u0646\u0628\u064A\u0647");
                        return;
                    }
                    
                    // اعرض نافذة اختيار الفاتورة
                    var win = new System.Windows.Window
                    {
                        Title = "\u0627\u062E\u062A\u0631 \u0641\u0627\u062A\u0648\u0631\u0629",
                        Width = 400,
                        Height = 300,
                        WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                        Owner = this
                    };
                    
                    var list = new System.Windows.Controls.ListBox { Margin = new Thickness(10) };
                    foreach (var inv in _vm.UnpaidInvoices)
                    {
                        list.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = inv.Display, Tag = inv });
                    }
                    
                    var btnOk = new System.Windows.Controls.Button
                    {
                        Content = "\u062A\u0623\u0643\u064A\u062F",
                        Width = 100,
                        Margin = new Thickness(10),
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                    };
                    
                    btnOk.Click += (s, ev) =>
                    {
                        if (list.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag is UnpaidInvoiceOption inv)
                        {
                            pay.InvoiceId = inv.Id;
                            pay.InvoiceNumber = inv.InvoiceNumber;
                            // إذا كان المبلغ 0، املأه بقيمة المتبقي
                            if (pay.Amount <= 0) pay.Amount = inv.Remaining;
                            win.Close();
                        }
                    };
                    
                    var panel = new System.Windows.Controls.StackPanel();
                    panel.Children.Add(new System.Windows.Controls.TextBlock
                    {
                        Text = "\u0627\u062E\u062A\u0631 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629 \u0627\u0644\u0645\u0631\u0627\u062F \u0631\u0628\u0637\u0647\u0627 \u0628\u0647\u0630\u0647 \u0627\u0644\u062F\u0641\u0639\u0629:",
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
'@
    # أضف قبل آخر }
    $lastBrace = $csContent.LastIndexOf('}')
    if ($lastBrace -gt 0) {
        $csContent = $csContent.Substring(0, $lastBrace) + $insertMethod + "`n    }`n}"
    }
}

# تحديث النصوص العربية للمدفوعات
if ($csContent -match 'ColPayMethod\.Header.*?\\u0637.*?\}') {
    $csContent = $csContent -replace 'try \{ ColPayMethod\.Header = "[^"]*"; \} catch \{ \}', 'try { ColPayMethod.Header = "\u0637\u0631\u064A\u0642\u0629"; } catch { }'
}
# إضافة نص لـ ColPayInvLink و ColPayLink
if ($csContent -notmatch 'ColPayInvLink\.Header') {
    $csContent = $csContent -replace 'try \{ ColPayRef\.Header = "[^"]*"; \} catch \{ \}', 'try { ColPayRef.Header = "\u0645\u0631\u062C\u0639"; } catch { }
            try { ColPayInvLink.Header = "\u0641\u0627\u062A\u0648\u0631\u0629 \u0645\u0631\u062A\u0628\u0637\u0629"; } catch { }
            try { ColPayLink.Header = "\u0631\u0628\u0637"; } catch { }'
}

[System.IO.File]::WriteAllText($csPath, $csContent, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: Code-behind updated (Link button + Arabic texts)" -ForegroundColor Green

# ============================================================
# البناء
# ============================================================
Write-Host "`n===== Build =====" -ForegroundColor Cyan
dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1 | Select-Object -Last 10
Write-Host "`nDONE! Run: dotnet run --project Techtoon.UI" -ForegroundColor Green
