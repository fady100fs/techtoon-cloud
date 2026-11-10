using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    /// <summary>خيار مركز تكلفة (للاختيار في القائمة).</summary>
    public class CostCenterOption : ViewModelBase
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Display => (Code ?? "") + " - " + (Name ?? "");
        public override string ToString() => Display;
    }

    /// <summary>سطر توزيع تكلفة (حساب + نسبة + شهر/سنة).</summary>
    public class CostAllocationRow : ViewModelBase
    {
        public int Id { get; set; }
        public int CostCenterId { get; set; }
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = "";
        public string AccountName { get; set; } = "";
        public decimal Percentage { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Notes { get; set; } = "";

        public string AccountDisplay => (AccountCode ?? "") + " - " + (AccountName ?? "");
        public string PercentageDisplay => Percentage.ToString("0.##") + "%";
        public string PeriodDisplay => Month.ToString("00") + "/" + Year;
    }

    /// <summary>سطر حركة على مركز التكلفة.</summary>
    public class CostCenterTransactionRow : ViewModelBase
    {
        public int Id { get; set; }
        public int CostCenterId { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = "debit";
        public string ReferenceType { get; set; } = "";
        public int? ReferenceId { get; set; }

        public string DateDisplay => Date == DateTime.MinValue ? "" : Date.ToString("yyyy-MM-dd");
        public string AmountDisplay => Amount.ToString("N2");
        public string TypeDisplay => (TransactionType ?? "").Trim().ToLowerInvariant() switch
        {
            "debit" => "مدين (يزيد التكلفة)",
            "credit" => "دائن (ينقص التكلفة)",
            "expense" => "مصروف",
            "revenue" => "إيراد",
            "transfer_out" => "تحويل صادر",
            "transfer_in" => "تحويل وارد",
            _ => TransactionType ?? ""
        };
        public string ReferenceDisplay
        {
            get
            {
                bool hasType = !string.IsNullOrWhiteSpace(ReferenceType);
                int refIdValue = ReferenceId.GetValueOrDefault();
                bool hasId = refIdValue > 0;
                if (!hasType && !hasId) return "";
                if (hasType && hasId) return ReferenceType + " #" + refIdValue;
                return hasType ? ReferenceType : "#" + refIdValue;
            }
        }
    }

    /// <summary>ملخص مركز التكلفة (إجماليات + الصافي).</summary>
    public class CostCenterSummaryRow : ViewModelBase
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public decimal DebitTotal { get; set; }
        public decimal CreditTotal { get; set; }
        public decimal Net { get; set; }
        public string DebitDisplay => DebitTotal.ToString("N2");
        public string CreditDisplay => CreditTotal.ToString("N2");
        public string NetDisplay => Net.ToString("N2");
    }

    /// <summary>
    /// B5: توزيع التكاليف وحركات مراكز التكلفة.
    /// يعرض المركز المختار توزيعاته وحركاته، مع الصافي لكل مركز في تبويب الملخص.
    /// </summary>
    public class CostAllocationsViewModel : ViewModelBase
    {
        public ObservableCollection<CostCenterOption> CostCenters { get; } = new();
        public ObservableCollection<CostAllocationRow> Allocations { get; } = new();
        public ObservableCollection<CostCenterTransactionRow> Transactions { get; } = new();
        public ObservableCollection<CostCenterSummaryRow> Summary { get; } = new();

        private CostCenterOption _selectedCostCenter;
        public CostCenterOption SelectedCostCenter
        {
            get => _selectedCostCenter;
            set
            {
                if (SetProperty(ref _selectedCostCenter, value))
                {
                    LoadAllocations();
                    LoadTransactions();
                    OnPropertyChanged(nameof(SelectedCostCenterDisplay));
                    OnPropertyChanged(nameof(SelectedNetDisplay));
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private CostAllocationRow _selectedAllocation;
        public CostAllocationRow SelectedAllocation
        {
            get => _selectedAllocation;
            set
            {
                if (SetProperty(ref _selectedAllocation, value))
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private CostCenterTransactionRow _selectedTransaction;
        public CostCenterTransactionRow SelectedTransaction
        {
            get => _selectedTransaction;
            set
            {
                if (SetProperty(ref _selectedTransaction, value))
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public string SelectedCostCenterDisplay => SelectedCostCenter == null ? "لم يتم اختيار مركز تكلفة" : SelectedCostCenter.Display;
        public string SelectedNetDisplay => SelectedCostCenter == null ? "" : NetFor(SelectedCostCenter.Id).ToString("N2");

        public ICommand RefreshCommand { get; }
        public ICommand AddAllocationCommand { get; }
        public ICommand EditAllocationCommand { get; }
        public ICommand DeleteAllocationCommand { get; }
        public ICommand AddTransactionCommand { get; }
        public ICommand EditTransactionCommand { get; }
        public ICommand DeleteTransactionCommand { get; }

        public CostAllocationsViewModel()
        {
            RefreshCommand = new UiRelayCommand(_ => LoadAll());
            AddAllocationCommand = new UiRelayCommand(_ => OpenAllocationEditor(0), _ => SelectedCostCenter != null);
            EditAllocationCommand = new UiRelayCommand(_ => OpenAllocationEditor(SelectedAllocation?.Id ?? 0), _ => SelectedAllocation != null);
            DeleteAllocationCommand = new UiRelayCommand(_ => DeleteAllocation(), _ => SelectedAllocation != null);
            AddTransactionCommand = new UiRelayCommand(_ => OpenTransactionEditor(0), _ => SelectedCostCenter != null);
            EditTransactionCommand = new UiRelayCommand(_ => OpenTransactionEditor(SelectedTransaction?.Id ?? 0), _ => SelectedTransaction != null);
            DeleteTransactionCommand = new UiRelayCommand(_ => DeleteTransaction(), _ => SelectedTransaction != null);
            LoadAll();
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        public void LoadAll()
        {
            LoadCostCenters();
            LoadAllocations();
            LoadTransactions();
            LoadSummary();
            OnPropertyChanged(nameof(SelectedCostCenterDisplay));
            OnPropertyChanged(nameof(SelectedNetDisplay));
        }

        private void LoadCostCenters()
        {
            int keepId = SelectedCostCenter?.Id ?? 0;
            CostCenters.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT id, code, name FROM cost_centers ORDER BY name LIMIT 500", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CostCenters.Add(new CostCenterOption
                    {
                        Id = r.GetInt32(0),
                        Code = r.IsDBNull(1) ? "" : r.GetString(1),
                        Name = r.IsDBNull(2) ? "" : r.GetString(2)
                    });

                if (keepId > 0)
                {
                    var keep = CostCenters.FirstOrDefault(c => c.Id == keepId);
                    if (keep != null) SelectedCostCenter = keep;
                    else StatusMessage = "تم التحميل: " + CostCenters.Count + " مركز تكلفة";
                }
                else if (CostCenters.Count > 0)
                {
                    SelectedCostCenter = CostCenters[0];
                }
                else
                {
                    StatusMessage = "لا توجد مراكز تكلفة — أضف مركز تكلفة من صفحة 'مراكز التكلفة'.";
                }
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        public void LoadAllocations()
        {
            int keepId = SelectedAllocation?.Id ?? 0;
            Allocations.Clear();
            if (SelectedCostCenter == null) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    @"SELECT ca.id, ca.cost_center_id, ca.account_id, COALESCE(a.code,''), COALESCE(a.name,''),
                              ca.allocation_percentage, ca.month, ca.year, COALESCE(ca.notes,'')
                       FROM cost_allocations ca
                       LEFT JOIN accounts a ON a.id = ca.account_id
                       WHERE ca.cost_center_id = @cc
                       ORDER BY ca.year DESC, ca.month DESC, ca.id DESC", conn);
                cmd.Parameters.AddWithValue("cc", SelectedCostCenter.Id);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Allocations.Add(new CostAllocationRow
                    {
                        Id = r.GetInt32(0),
                        CostCenterId = r.GetInt32(1),
                        AccountId = r.GetInt32(2),
                        AccountCode = r.IsDBNull(3) ? "" : r.GetString(3),
                        AccountName = r.IsDBNull(4) ? "" : r.GetString(4),
                        Percentage = Convert.ToDecimal(r.GetValue(5)),
                        Month = r.GetInt32(6),
                        Year = r.GetInt32(7),
                        Notes = r.IsDBNull(8) ? "" : r.GetString(8)
                    });

                if (keepId > 0)
                {
                    var keep = Allocations.FirstOrDefault(a => a.Id == keepId);
                    if (keep != null) SelectedAllocation = keep;
                }
                StatusMessage = "تم التحميل: " + Allocations.Count + " توزيع";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        public void LoadTransactions()
        {
            int keepId = SelectedTransaction?.Id ?? 0;
            Transactions.Clear();
            if (SelectedCostCenter == null) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    @"SELECT id, cost_center_id, date, description, amount, transaction_type,
                              COALESCE(reference_type,''), reference_id
                       FROM cost_center_transactions
                       WHERE cost_center_id = @cc
                       ORDER BY date DESC, id DESC LIMIT 500", conn);
                cmd.Parameters.AddWithValue("cc", SelectedCostCenter.Id);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Transactions.Add(new CostCenterTransactionRow
                    {
                        Id = r.GetInt32(0),
                        CostCenterId = r.GetInt32(1),
                        Date = DbValue.AsDateTime(r.GetValue(2)),
                        Description = r.IsDBNull(3) ? "" : r.GetString(3),
                        Amount = Convert.ToDecimal(r.GetValue(4)),
                        TransactionType = r.IsDBNull(5) ? "debit" : r.GetString(5),
                        ReferenceType = r.IsDBNull(6) ? "" : r.GetString(6),
                        ReferenceId = r.IsDBNull(7) ? (int?)null : r.GetInt32(7)
                    });

                if (keepId > 0)
                {
                    var keep = Transactions.FirstOrDefault(t => t.Id == keepId);
                    if (keep != null) SelectedTransaction = keep;
                }
                StatusMessage = "تم التحميل: " + Transactions.Count + " حركة";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        public void LoadSummary()
        {
            Summary.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    @"SELECT cc.id, cc.code, cc.name,
                              COALESCE((SELECT SUM(t.amount) FROM cost_center_transactions t
                                         WHERE t.cost_center_id = cc.id
                                           AND LOWER(t.transaction_type) IN ('debit','expense','transfer_out')),0),
                              COALESCE((SELECT SUM(t.amount) FROM cost_center_transactions t
                                         WHERE t.cost_center_id = cc.id
                                           AND LOWER(t.transaction_type) IN ('credit','revenue','transfer_in')),0)
                       FROM cost_centers cc
                       ORDER BY cc.name LIMIT 500", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    decimal debit = Convert.ToDecimal(r.GetValue(3));
                    decimal credit = Convert.ToDecimal(r.GetValue(4));
                    Summary.Add(new CostCenterSummaryRow
                    {
                        Id = r.GetInt32(0),
                        Code = r.IsDBNull(1) ? "" : r.GetString(1),
                        Name = r.IsDBNull(2) ? "" : r.GetString(2),
                        DebitTotal = debit,
                        CreditTotal = credit,
                        Net = debit - credit
                    });
                }
                OnPropertyChanged(nameof(SelectedNetDisplay));
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private decimal NetFor(int costCenterId)
        {
            var s = Summary.FirstOrDefault(x => x.Id == costCenterId);
            return s?.Net ?? 0m;
        }

        private void OpenAllocationEditor(int id)
        {
            if (SelectedCostCenter == null) return;
            try
            {
                var win = new Views.CostAllocationEditWindow(SelectedCostCenter.Id, id);
                if (win.ShowDialog() == true)
                {
                    LoadAllocations();
                    LoadSummary();
                }
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenTransactionEditor(int id)
        {
            if (SelectedCostCenter == null) return;
            try
            {
                var win = new Views.CostCenterTransactionWindow(SelectedCostCenter.Id, id);
                if (win.ShowDialog() == true)
                {
                    LoadTransactions();
                    LoadSummary();
                }
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void DeleteAllocation()
        {
            if (SelectedAllocation == null) return;
            if (System.Windows.MessageBox.Show("تأكيد حذف التوزيع؟\n" + SelectedAllocation.AccountDisplay + " — " + SelectedAllocation.PeriodDisplay,
                    "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
                != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM cost_allocations WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", SelectedAllocation.Id);
                cmd.ExecuteNonQuery();
                AuditService.Log("DELETE", "CostAllocation", SelectedAllocation.Id,
                    "حذف توزيع تكلفة: مركز " + SelectedAllocation.CostCenterId + " / حساب " + SelectedAllocation.AccountDisplay + " / " + SelectedAllocation.PeriodDisplay);
                StatusMessage = "تم الحذف بنجاح";
                LoadAllocations();
            }
            catch (Npgsql.PostgresException px)
            {
                System.Windows.MessageBox.Show("خطأ في قاعدة البيانات: " + px.Message, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("خطأ: " + ex.Message, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void DeleteTransaction()
        {
            if (SelectedTransaction == null) return;
            if (System.Windows.MessageBox.Show("تأكيد حذف الحركة؟\n" + SelectedTransaction.Description + " — " + SelectedTransaction.AmountDisplay,
                    "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
                != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM cost_center_transactions WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", SelectedTransaction.Id);
                cmd.ExecuteNonQuery();
                AuditService.Log("DELETE", "CostCenterTransaction", SelectedTransaction.Id,
                    "حذف حركة مركز تكلفة: " + SelectedTransaction.Description + " (" + SelectedTransaction.AmountDisplay + ")");
                StatusMessage = "تم الحذف بنجاح";
                LoadTransactions();
                LoadSummary();
            }
            catch (Npgsql.PostgresException px)
            {
                System.Windows.MessageBox.Show("خطأ في قاعدة البيانات: " + px.Message, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("خطأ: " + ex.Message, "خطأ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
