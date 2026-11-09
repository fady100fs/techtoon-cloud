using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    public class CashBoxRow : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Type { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public string OpeningDisplay { get { return OpeningBalance.ToString("N2"); } }
        public string CurrentDisplay { get { return CurrentBalance.ToString("N2"); } }
    }

    public class CashBoxesViewModel : ViewModelBase
    {
        public ObservableCollection<CashBoxRow> CashBoxes { get; set; }
        public ICommand RefreshCommand { get; set; }

        public CashBoxesViewModel()
        {
            CashBoxes = new ObservableCollection<CashBoxRow>();
            RefreshCommand = new UiRelayCommand(_ => _ = LoadAsync());
            _ = LoadAsync();
        }

        public async System.Threading.Tasks.Task LoadAsync()
        {
            CashBoxes.Clear();
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                await conn.OpenAsync();
                // حساب الرصيد ديناميكيًا: افتتاحي + قبض - صرف - مصروفات + قروض + تحويلات واردة - تحويلات صادرة
                string sql = @"
                    SELECT cb.id, cb.name, cb.code, cb.type, COALESCE(cb.opening_balance, 0),
                    COALESCE(cb.opening_balance, 0) 
                    + COALESCE((SELECT SUM(amount) FROM payments WHERE cash_box_id = cb.id AND payment_type = 'receipt'), 0)
                    - COALESCE((SELECT SUM(amount) FROM payments WHERE cash_box_id = cb.id AND payment_type = 'payment'), 0)
                    - COALESCE((SELECT SUM(amount) FROM expenses WHERE cash_box_id = cb.id), 0)
                    + COALESCE((SELECT SUM(principal_amount) FROM loans WHERE bank_account_id = cb.id), 0)
                    + COALESCE((SELECT SUM(amount) FROM cash_transfers WHERE to_cash_box_id = cb.id), 0)
                    - COALESCE((SELECT SUM(amount) FROM cash_transfers WHERE from_cash_box_id = cb.id), 0)
                    AS current_balance
                    FROM cash_boxes cb WHERE cb.is_active = TRUE ORDER BY cb.name";

                using var cmd = new NpgsqlCommand(sql, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    CashBoxes.Add(new CashBoxRow
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Code = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Type = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        OpeningBalance = Convert.ToDecimal(reader.GetValue(4)),
                        CurrentBalance = Convert.ToDecimal(reader.GetValue(5))
                    });
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}