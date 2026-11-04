# fix_v2.ps1 - Arabic text in code-behind + searchable ComboBox
$ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
$ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'

if (-not (Test-Path $ViewsDir)) { Write-Host 'ERROR: Path not found' -ForegroundColor Red; exit 1 }

Write-Host '========================================' -ForegroundColor Cyan
Write-Host '  Techtoon Fix v2 - Arabic in code-behind' -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan

# حذف الملفات المكررة
Get-ChildItem -Path $ViewsDir -Filter 'InvoiceEdit*' | Where-Object { $_.Name -match '\(1\)' } | Remove-Item -Force -ErrorAction SilentlyContinue

# ===== InvoiceEditWindow.xaml (ASCII only) =====
$xamlPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml'
if (Test-Path $xamlPath) { Copy-Item $xamlPath ($xamlPath + '.bak_v2') -Force }
$xaml = @'
<Window x:Class="Techtoon.UI.Views.InvoiceEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Invoice" Height="850" Width="1200"
        WindowStartupLocation="CenterScreen" FlowDirection="RightToLeft"
        Background="#F5F7FA" Loaded="Window_Loaded" FontFamily="Tahoma">
    <Window.Resources>
        <Style x:Key="Hdr" TargetType="TextBlock">
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="Margin" Value="0,0,6,0"/>
            <Setter Property="VerticalAlignment" Value="Center"/>
        </Style>
        <Style x:Key="Inp" TargetType="TextBox">
            <Setter Property="Padding" Value="6,4"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="BorderBrush" Value="#B0BEC5"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="Background" Value="White"/>
        </Style>
        <Style x:Key="AddBtn" TargetType="Button">
            <Setter Property="Background" Value="#26A69A"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="14,6"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="SaveBtn" TargetType="Button">
            <Setter Property="Background" Value="#00897B"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="Padding" Value="24,10"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="CancelBtn" TargetType="Button">
            <Setter Property="Background" Value="#ECEFF1"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="18,10"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
    </Window.Resources>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock x:Name="HeaderIcon" Text="***" FontSize="28" Margin="0,0,10,0"/>
                    <StackPanel>
                        <TextBlock x:Name="HeaderTitle" Text="Invoice" FontSize="18" FontWeight="Bold" Foreground="#00695C"/>
                        <TextBlock Text="{Binding InvoiceTypeDisplay}" FontSize="12" Foreground="#78909C"/>
                    </StackPanel>
                </StackPanel>
                <Grid Grid.Column="1" Margin="20,0,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="140"/>
                        <ColumnDefinition Width="18"/><ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="150"/><ColumnDefinition Width="18"/>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="120"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/><RowDefinition Height="8"/><RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock x:Name="LblSerial" Grid.Row="0" Grid.Column="0" Text="No:" Style="{StaticResource Hdr}"/>
                    <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding SerialNumber}" Style="{StaticResource Inp}" IsReadOnly="True"/>
                    <TextBlock x:Name="LblDate" Grid.Row="0" Grid.Column="3" Text="Date:" Style="{StaticResource Hdr}"/>
                    <DatePicker Grid.Row="0" Grid.Column="4" SelectedDate="{Binding InvoiceDate}" FontSize="13"/>
                    <TextBlock x:Name="LblType" Grid.Row="0" Grid.Column="6" Text="Type:" Style="{StaticResource Hdr}"/>
                    <ComboBox x:Name="CmbType" Grid.Row="0" Grid.Column="7" SelectedValue="{Binding InvoiceType}" SelectedValuePath="Tag" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblParty" Grid.Row="2" Grid.Column="0" Text="Party:" Style="{StaticResource Hdr}"/>
                    <ComboBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="4" ItemsSource="{Binding Parties}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding PartyId}" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblWarehouse" Grid.Row="2" Grid.Column="6" Text="WH:" Style="{StaticResource Hdr}"/>
                    <ComboBox Grid.Row="2" Grid.Column="7" ItemsSource="{Binding Warehouses}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding WarehouseId}" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                </Grid>
            </Grid>
        </Border>

        <!-- Items -->
        <Border Grid.Row="1" Background="White" Margin="10,8,10,4" Padding="8" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock x:Name="LblItems" Grid.Column="0" Text="Items" FontSize="15" FontWeight="Bold" Foreground="#37474F" VerticalAlignment="Center"/>
                    <Button x:Name="BtnAddLine" Grid.Column="1" Content="+ Add" Style="{StaticResource AddBtn}" Command="{Binding AddLineCommand}"/>
                </Grid>
                <DataGrid x:Name="LinesGrid" Grid.Row="1" ItemsSource="{Binding Lines}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MinHeight="180" LoadingRow="LinesGrid_LoadingRow">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn x:Name="ColItem" Header="Item" Width="320">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox x:Name="ItemSearchCombo"
                                              ItemsSource="{Binding DataContext.AllItemsList, RelativeSource={RelativeSource AncestorType=DataGrid}}"
                                              DisplayMemberPath="SearchDisplay"
                                              IsEditable="True"
                                              Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                                              StaysOpenOnEdit="True"
                                              SelectionChanged="ItemSearchCombo_SelectionChanged"
                                              Padding="4,3" FontSize="13"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColBarcode" Header="Barcode" Width="110" Binding="{Binding Barcode}" IsReadOnly="True"/>
                        <DataGridTextColumn x:Name="ColUnit" Header="Unit" Width="70" Binding="{Binding Unit}" IsReadOnly="True"/>
                        <DataGridTemplateColumn x:Name="ColQty" Header="Qty" Width="90">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Quantity, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="qty" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPrice" Header="Price" Width="100">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Price, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="price" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColDisc" Header="Disc" Width="80">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Discount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="discount" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColTotal" Header="Total" Width="110" Binding="{Binding Total, StringFormat=N2}" IsReadOnly="True">
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="FontWeight" Value="SemiBold"/>
                                    <Setter Property="Foreground" Value="#00695C"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
                        <DataGridTemplateColumn x:Name="ColDel" Header="X" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeleteLineCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <!-- Payments -->
        <Border x:Name="PaymentsSection" Grid.Row="2" Background="White" Margin="10,4,10,4" Padding="10" BorderBrush="#80CBC4" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock x:Name="LblPayments" Grid.Column="0" Text="Payments" FontSize="15" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
                    <Button x:Name="BtnAddPay" Grid.Column="1" Content="+ Add" Style="{StaticResource AddBtn}" Command="{Binding AddPaymentCommand}"/>
                </Grid>
                <DataGrid x:Name="PaymentsGrid" Grid.Row="1" ItemsSource="{Binding Payments}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MaxHeight="140" CellEditEnding="PaymentsGrid_CellEditEnding">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn x:Name="ColPayMethod" Header="Method" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox x:Name="PayMethodCombo" SelectedValue="{Binding Method, UpdateSourceTrigger=PropertyChanged}" SelectedValuePath="Tag" Padding="4,2"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPayAmount" Header="Amount" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Amount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Padding="4,3" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColPayRef" Header="Ref" Width="180" Binding="{Binding Reference, UpdateSourceTrigger=PropertyChanged}"/>
                        <DataGridTemplateColumn x:Name="ColPayDate" Header="Date" Width="150">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <DatePicker SelectedDate="{Binding PaymentDate}" FontSize="12"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColPayNotes" Header="Notes" Width="*" Binding="{Binding Notes, UpdateSourceTrigger=PropertyChanged}"/>
                        <DataGridTemplateColumn x:Name="ColPayDel" Header="X" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <!-- Totals + Buttons -->
        <Border Grid.Row="3" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="300"/>
                    </Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" VerticalAlignment="Center">
                        <TextBlock x:Name="StatusText" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" TextWrapping="Wrap"/>
                    </StackPanel>
                    <Grid Grid.Column="1">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/><ColumnDefinition Width="110"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <TextBlock x:Name="LblSubtotal" Grid.Row="0" Grid.Column="0" Text="Sub:" FontSize="13" Foreground="#546E7A" Padding="0,2"/>
                        <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding SubtotalDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#37474F" TextAlignment="Left" Padding="0,2"/>
                        <StackPanel Grid.Row="1" Grid.Column="0" Orientation="Horizontal">
                            <TextBlock x:Name="LblDisc" Text="Disc:" FontSize="13" Foreground="#546E7A" VerticalAlignment="Center"/>
                            <TextBox Text="{Binding DiscountAmount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Width="80" Margin="6,0,0,0" Padding="3,2" FontSize="12" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                        </StackPanel>
                        <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding DiscountAmount, StringFormat=N2}" FontSize="13" Foreground="#E53935" TextAlignment="Left"/>
                        <TextBlock x:Name="LblTotal" Grid.Row="2" Grid.Column="0" Text="Total:" FontSize="14" FontWeight="Bold" Foreground="#00695C" Padding="0,2"/>
                        <TextBlock Grid.Row="2" Grid.Column="1" Text="{Binding TotalDisplay}" FontSize="15" FontWeight="Bold" Foreground="#00695C" TextAlignment="Left" Padding="0,2"/>
                        <TextBlock x:Name="LblPaid" Grid.Row="3" Grid.Column="0" Text="Paid:" FontSize="13" Foreground="#2E7D32" Padding="0,2"/>
                        <TextBlock Grid.Row="3" Grid.Column="1" Text="{Binding TotalPaidDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#2E7D32" TextAlignment="Left" Padding="0,2"/>
                        <TextBlock x:Name="LblRemaining" Grid.Row="4" Grid.Column="0" Text="Rem:" FontSize="14" FontWeight="Bold" Foreground="#E65100" Padding="0,2"/>
                        <TextBlock Grid.Row="4" Grid.Column="1" Text="{Binding RemainingDisplay}" FontSize="14" FontWeight="Bold" Foreground="#E65100" TextAlignment="Left" Padding="0,2"/>
                    </Grid>
                </Grid>
                <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Left" FlowDirection="LeftToRight" Margin="0,8,0,0">
                    <Button x:Name="BtnSave" Content="Save" Style="{StaticResource SaveBtn}" Margin="0,0,8,0" Command="{Binding SaveCommand}"/>
                    <Button x:Name="BtnSavePrint" Content="Save+Print" Style="{StaticResource SaveBtn}" Margin="0,0,8,0" Command="{Binding SaveAndPrintCommand}"/>
                    <Button x:Name="BtnCancel" Content="Cancel" Style="{StaticResource CancelBtn}" Command="{Binding CancelCommand}"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</Window>
'@
[System.IO.File]::WriteAllText($xamlPath, $xaml, [System.Text.ASCIIEncoding]::new())
Write-Host "  OK: XAML (ASCII)" -ForegroundColor Green

# ===== InvoiceEditWindow.xaml.cs (UTF-8 with BOM) =====
$csPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml.cs'
if (Test-Path $csPath) { Copy-Item $csPath ($csPath + '.bak_v2') -Force }
$cs = @'
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoiceEditWindow : Window
    {
        private readonly InvoiceEditViewModel _vm;
        private readonly int? _editId;

        public InvoiceEditWindow(int? editId = null)
        {
            InitializeComponent();
            _editId = editId;
            _vm = new InvoiceEditViewModel();
            if (editId.HasValue) _vm.Id = editId.Value;
            DataContext = _vm;
            _vm.RequestClose += () => { DialogResult = _vm.DialogResult; Close(); };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ضبط كل النصوص العربية من الكود (يحل مشكلة الترميز)
            SetArabicTexts();
            // تعبئة قوائم الطريقة
            FillTypeCombo();
            FillPaymentMethods();
            // تحميل البيانات
            await _vm.LoadAsync();
            // رسالة تشخيصية
            try { MessageBox.Show("Items loaded: " + (_vm.AllItemsList?.Count ?? 0), "Debug"); } catch { }
            if (_editId.HasValue) await _vm.LoadFromInvoiceAsync(_editId.Value);
        }

        private void SetArabicTexts()
        {
            try { HeaderIcon.Text = "\uDDF9"; } catch { }
            try { HeaderTitle.Text = "\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // فاتورة
            try { LblSerial.Text = "\u0627\u0644\u0631\u0642\u0645:"; } catch { } // الرقم:
            try { LblDate.Text = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E:"; } catch { } // التاريخ:
            try { LblType.Text = "\u0627\u0644\u0646\u0648\u0639:"; } catch { } // النوع:
            try { LblParty.Text = "\u0627\u0644\u0637\u0631\u0641:"; } catch { } // الطرف:
            try { LblWarehouse.Text = "\u0627\u0644\u0645\u062E\u0632\u0646:"; } catch { } // المخزن:
            try { LblItems.Text = "\u0628\u0646\u0648\u062F \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // بنود الفاتورة
            try { BtnAddLine.Content = "+ \u0625\u0636\u0627\u0641\u0629 \u0635\u0646\u0641"; } catch { } // + إضافة صنف
            try { ColItem.Header = "\u0635\u0646\u0641 (\u0627\u0643\u062A\u0628 \u0644\u0644\u0628\u062D\u062B)"; } catch { } // صنف (اكتب للبحث)
            try { ColBarcode.Header = "\u0628\u0627\u0631\u0643\u0648\u062F"; } catch { } // باركود
            try { ColUnit.Header = "\u0627\u0644\u0648\u062D\u062F\u0629"; } catch { } // الوحدة
            try { ColQty.Header = "\u0627\u0644\u0643\u0645\u064A\u0629"; } catch { } // الكمية
            try { ColPrice.Header = "\u0627\u0644\u0633\u0639\u0631"; } catch { } // السعر
            try { ColDisc.Header = "\u062E\u0635\u0645"; } catch { } // خصم
            try { ColTotal.Header = "\u0625\u062C\u0645\u0627\u0644\u064A"; } catch { } // إجمالي
            try { ColDel.Header = "\u062D\u0630\u0641"; } catch { } // حذف
            try { LblPayments.Text = "\u0637\u0631\u0642 \u0627\u0644\u062F\u0641\u0639 \u0627\u0644\u0645\u062A\u0639\u062F\u062F\u0629"; } catch { } // طرق الدفع المتعددة
            try { BtnAddPay.Content = "+ \u0625\u0636\u0627\u0641\u0629 \u062F\u0641\u0639\u0629"; } catch { } // + إضافة دفعة
            try { ColPayMethod.Header = "\u0637\u0631\u064A\u0642\u0629 \u0627\u0644\u062F\u0641\u0639"; } catch { } // طريقة الدفع
            try { ColPayAmount.Header = "\u0627\u0644\u0645\u0628\u0644\u063A"; } catch { } // المبلغ
            try { ColPayRef.Header = "\u0645\u0631\u062C\u0639"; } catch { } // مرجع
            try { ColPayDate.Header = "\u062A\u0627\u0631\u064A\u062E"; } catch { } // تاريخ
            try { ColPayNotes.Header = "\u0645\u0644\u0627\u062D\u0638\u0627\u062A"; } catch { } // ملاحظات
            try { ColPayDel.Header = "\u062D\u0630\u0641"; } catch { } // حذف
            try { LblSubtotal.Text = "\u0627\u0644\u0645\u062C\u0645\u0648\u0639:"; } catch { } // المجموع:
            try { LblDisc.Text = "\u062E\u0635\u0645:"; } catch { } // خصم:
            try { LblTotal.Text = "\u0625\u062C\u0645\u0627\u0644\u064A:"; } catch { } // إجمالي:
            try { LblPaid.Text = "\u0627\u0644\u0645\u062F\u0641\u0648\u0639:"; } catch { } // المدفوع:
            try { LblRemaining.Text = "\u0627\u0644\u0645\u062A\u0628\u0642\u064A:"; } catch { } // المتبقي:
            try { BtnSave.Content = "\uD83D\uDCBE \u062D\u0641\u0638 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // 💾 حفظ الفاتورة
            try { BtnSavePrint.Content = "\uD83D\uDDA8 \u062D\u0641\u0638 \u0648\u0637\u0628\u0627\u0639\u0629"; } catch { } // 🖨 حفظ وطباعة
            try { BtnCancel.Content = "\u2716 \u0625\u0644\u063A\u0627\u0621"; } catch { } // ✖ إلغاء
        }

        private void FillTypeCombo()
        {
            try
            {
                CmbType.Items.Clear();
                CmbType.Items.Add(new ComboBoxItem { Content = "\u0645\u0628\u064A\u0639\u0627\u062A", Tag = "sales" }); // مبيعات
                CmbType.Items.Add(new ComboBoxItem { Content = "\u0645\u0634\u062A\u0631\u064A\u0627\u062A", Tag = "purchase" }); // مشتريات
            }
            catch { }
        }

        private void FillPaymentMethods()
        {
            // سيتم تعبئة عند تحميل كل صف
        }

        private void LinesGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // تعبئة قائمة طرق الدفع في كل صف
            try
            {
                var row = e.Row;
                var payCombo = FindChild<ComboBox>(row, "PayMethodCombo");
                if (payCombo != null && payCombo.Items.Count == 0)
                {
                    payCombo.Items.Clear();
                    payCombo.Items.Add(new ComboBoxItem { Content = "\u0646\u0642\u062F\u064A", Tag = "cash" }); // نقدي
                    payCombo.Items.Add(new ComboBoxItem { Content = "\u0628\u0637\u0627\u0642\u0629", Tag = "card" }); // بطاقة
                    payCombo.Items.Add(new ComboBoxItem { Content = "\u062A\u062D\u0648\u064A\u0644 \u0628\u0646\u0643\u064A", Tag = "bank" }); // تحويل بنكي
                    payCombo.Items.Add(new ComboBoxItem { Content = "\u0634\u064A\u0643", Tag = "check" }); // شيك
                    payCombo.Items.Add(new ComboBoxItem { Content = "\u0622\u062C\u0644", Tag = "credit" }); // آجل
                }
            }
            catch { }
        }

        // البحث: عند اختيار صنف من القائمة المنسدلة
        private void ItemSearchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedItem is ItemSearchResult item)
                {
                    if (cb.DataContext is InvoiceLineItem line)
                    {
                        line.SelectItem(item);
                    }
                }
            }
            catch { }
        }

        private void Cell_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not TextBox tb) return;
            if (tb.Tag is not string tag) return;
            if (tb.DataContext is not InvoiceLineItem line) return;
            LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            int idx = _vm.Lines.IndexOf(line);
            int last = _vm.Lines.Count - 1;
            if (idx >= 0 && idx < last)
            {
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(idx + 1) is DataGridRow nextRow)
                {
                    var next = FindDescendant<TextBox>(nextRow, x => (x.Tag as string) == tag);
                    if (next != null) { next.Focus(); next.SelectAll(); }
                }
            }
            else if (idx == last)
            {
                _vm.AddLineCommand.Execute(null);
                LinesGrid.UpdateLayout();
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(_vm.Lines.Count - 1) is DataGridRow newRow)
                {
                    var cb = FindDescendant<ComboBox>(newRow, x => x.Name == "ItemSearchCombo");
                    cb?.Focus();
                }
            }
            e.Handled = true;
        }

        private static T? FindDescendant<T>(DependencyObject? parent, Func<T, bool> predicate) where T : class
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T matched && predicate(matched)) return matched;
                var deep = FindDescendant(child, predicate);
                if (deep != null) return deep;
            }
            return null;
        }

        private static T? FindChild<T>(DependencyObject? parent, string name) where T : FrameworkElement
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T fe && fe.Name == name) return fe;
                var deep = FindChild<T>(child, name);
                if (deep != null) return deep;
            }
            return null;
        }

        private void PaymentsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit) _vm.RefreshTotals();
        }
    }
}
'@
[System.IO.File]::WriteAllText($csPath, $cs, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: xaml.cs (UTF-8 BOM)" -ForegroundColor Green

# ===== appsettings.json =====
$appsettings = '{"ConnectionStrings":{"DefaultConnection":"Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"}}'
$ap = Join-Path $ProjectPath 'Techtoon.UI\appsettings.json'
[System.IO.File]::WriteAllText($ap, $appsettings, [System.Text.UTF8Encoding]::new($false))
$bd = Join-Path $ProjectPath 'Techtoon.UI\bin\Debug\net10.0-windows'
if (Test-Path $bd) { Copy-Item $ap (Join-Path $bd 'appsettings.json') -Force }
Write-Host "  OK: appsettings.json" -ForegroundColor Green

# ===== تنظيف obj =====
$objDir = Join-Path $ProjectPath 'Techtoon.UI\obj\Debug\net10.0-windows'
if (Test-Path $objDir) {
    Get-ChildItem -Path $objDir -Filter 'InvoiceEdit*' | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Host "  Cleaned obj" -ForegroundColor Green
}

# ===== البناء =====
Write-Host ''
Write-Host '===== Build =====' -ForegroundColor Cyan
dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1 | Select-Object -Last 8
Write-Host ''
Write-Host 'DONE! Run: dotnet run --project Techtoon.UI' -ForegroundColor Green