# fix_final2.ps1 - إصلاح اختيار الصنف + تحديث صفحة الفواتير
 $ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
 $ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Techtoon - Final Fixes (2 issues)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# ============================================================
# 1) إصلاح اختيار الصنف في InvoiceEditWindow.xaml.cs
#    المشكلة: ComboBox لا يحدث النص فوراً بعد الاختيار
# ============================================================
Write-Host "`n[1/2] Fixing item selection in code-behind..." -ForegroundColor Yellow

 $csPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml.cs'
if (Test-Path $csPath) {
    Copy-Item $csPath ($csPath + '.bak_f2') -Force
    $content = Get-Content $csPath -Raw -Encoding UTF8
    
    # استبدال دالة ItemSearchCombo_SelectionChanged
    $oldMethod = '(?s)private void ItemSearchCombo_SelectionChanged\(.*?\n        \}'
    $newMethod = @'
private void ItemSearchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedItem is ItemSearchResult item)
                {
                    if (cb.DataContext is InvoiceLineItem line)
                    {
                        line.SelectItem(item);
                        // تحديث النص في الـ ComboBox فوراً
                        cb.Text = item.Name;
                        // إعادة التركيز للكمية
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                                LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
                                if (LinesGrid.ItemContainerGenerator.ContainerFromItem(line) is DataGridRow row)
                                {
                                    var qtyTb = FindDescendant<TextBox>(row, x => (x.Tag as string) == "qty");
                                    qtyTb?.Focus();
                                    qtyTb?.SelectAll();
                                }
                            }
                            catch { }
                        }), System.Windows.Threading.DispatcherPriority.Input);
                    }
                }
            }
            catch { }
        }
'@
    
    $content = [regex]::Replace($content, $oldMethod, $newMethod)
    [System.IO.File]::WriteAllText($csPath, $content, [System.Text.UTF8Encoding]::new($true))
    Write-Host "  OK: Item selection fixed" -ForegroundColor Green
}
# ============================================================
# 2) إصلاح تحديث صفحة الفواتير بعد الحفظ
#    المشكلة: صفحة الفواتير لا تعيد تحميل البيانات
# ============================================================
Write-Host "`n[2/2] Fixing invoices page refresh..." -ForegroundColor Yellow

# ابحث عن ملف عرض الفواتير
 $invoicesViewPath = Join-Path $ViewsDir 'InvoicesView.xaml.cs'
if (-not (Test-Path $invoicesViewPath)) {
    $invoicesViewPath = Join-Path $ViewsDir 'InvoicesView.xaml.cs'
}

if (Test-Path $invoicesViewPath) {
    Copy-Item $invoicesViewPath ($invoicesViewPath + '.bak_f2') -Force
    $content = Get-Content $invoicesViewPath -Raw -Encoding UTF8
    
    # إضافة دالة ReloadAsync ودالة Refresh للوصول إليها من خارج الصفحة
    if ($content -notmatch 'public.*Refresh\b') {
        $insertCode = @'

        // دالة لتحديث الصفحة من الخارج (بعد حفظ فاتورة جديدة)
        public static void RefreshAllOpenInvoicesViews()
        {
            try
            {
                if (System.Windows.Application.Current?.MainWindow?.Content is System.Windows.Controls.Frame frame)
                {
                    var page = frame.Content;
                    if (page != null)
                    {
                        var refreshMethod = page.GetType().GetMethod("Refresh");
                        if (refreshMethod != null) refreshMethod.Invoke(page, null);
                    }
                }
            }
            catch { }
        }

        public void Refresh()
        {
            try { LoadInvoices(); } catch { }
        }

        private void LoadInvoices()
        {
            try
            {
                // اقرأ الفواتير من DB واعرضها
                var invoices = LoadInvoicesFromDb();
                if (InvoicesList != null)
                {
                    InvoicesList.ItemsSource = invoices;
                }
            }
            catch { }
        }

        private System.Collections.Generic.List<InvoiceRow> LoadInvoicesFromDb()
        {
            var list = new System.Collections.Generic.List<InvoiceRow>();
            try
            {
                string cs = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
                using (var conn = new Npgsql.NpgsqlConnection(cs))
                {
                    conn.Open();
                    string sql = @"SELECT i.id, i.invoice_number, i.date, i.type, i.total, i.paid_amount,
                        COALESCE(p.name, '') AS party_name
                        FROM invoices i LEFT JOIN parties p ON i.party_id = p.id
                        ORDER BY i.id DESC LIMIT 100";
                    using (var cmd = new Npgsql.NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new InvoiceRow
                            {
                                Id = r.IsDBNull(0) ? 0 : r.GetInt32(0),
                                InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                                Date = r.IsDBNull(2) ? DateTime.MinValue : r.GetDateTime(2),
                                Type = r.IsDBNull(3) ? "" : r.GetString(3),
                                Total = r.IsDBNull(4) ? 0 : Convert.ToDecimal(r.GetValue(4)),
                                Paid = r.IsDBNull(5) ? 0 : Convert.ToDecimal(r.GetValue(5)),
                                PartyName = r.IsDBNull(6) ? "" : r.GetString(6)
                            });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public class InvoiceRow
        {
            public int Id { get; set; }
            public string InvoiceNumber { get; set; }
            public DateTime Date { get; set; }
            public string Type { get; set; }
            public decimal Total { get; set; }
            public decimal Paid { get; set; }
            public string PartyName { get; set; }
            public decimal Remaining { get { return Total - Paid; } }
        }
'@
        # أضف في نهاية الكلاس (قبل آخر })
        $lastBrace = $content.LastIndexOf('}')
        if ($lastBrace -gt 0) {
            $content = $content.Substring(0, $lastBrace) + $insertCode + "`n}" 
        }
        [System.IO.File]::WriteAllText($invoicesViewPath, $content, [System.Text.UTF8Encoding]::new($true))
        Write-Host "  OK: Invoices page refresh added" -ForegroundColor Green
    } else {
        Write-Host "  SKIP: Refresh already exists" -ForegroundColor Yellow
    }
} else {
    Write-Host "  WARN: InvoicesView.xaml.cs not found" -ForegroundColor Yellow
}

# ============================================================
# 3) تحديث InvoiceEditViewModel.cs لإعلام نافذة الفواتير بعد الحفظ
# ============================================================
Write-Host "`n[3/3] Updating ViewModel to notify after save..." -ForegroundColor Yellow
 $vmPath = Join-Path $ViewsDir 'InvoiceEditViewModel.cs'
if (Test-Path $vmPath) {
    $content = Get-Content $vmPath -Raw -Encoding UTF8
    # بعد RequestClose في SaveAsync، أضف استدعاء RefreshAllOpenInvoicesViews
    if ($content -notmatch 'RefreshAllOpenInvoicesViews') {
        $old = 'StatusMessage = "Saved! ID: " + Id;'
        $new = 'StatusMessage = "Saved! ID: " + Id;
                try { Techtoon.UI.Views.InvoicesView.RefreshAllOpenInvoicesViews(); } catch { }'
        $content = $content.Replace($old, $new)
        [System.IO.File]::WriteAllText($vmPath, $content, [System.Text.UTF8Encoding]::new($true))
        Write-Host "  OK: ViewModel notifies after save" -ForegroundColor Green
    } else {
        Write-Host "  SKIP: Already present" -ForegroundColor Yellow
    }
}

# ============================================================
# البناء
# ============================================================
Write-Host "`n===== Build =====" -ForegroundColor Cyan
dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1 | Select-Object -Last 8
Write-Host "`nDONE! Run: dotnet run --project Techtoon.UI" -ForegroundColor Green
