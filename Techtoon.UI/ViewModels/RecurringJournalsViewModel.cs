using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class RecurringJournalRow : ViewModelBase
    {
        public int Id { get; set; }
        public string TemplateNumber { get; set; }
        public string Name { get; set; }
        public string Frequency { get; set; }
        public DateTime NextRunDate { get; set; }
        public bool IsActive { get; set; }
        public string FrequencyDisplay => Frequency switch
        {
            "daily" => "يومي",
            "weekly" => "أسبوعي",
            "monthly" => "شهري",
            "quarterly" => "ربع سنوي",
            "semiannual" => "نصف سنوي",
            "annual" => "سنوي",
            "custom" => "مخصص",
            _ => Frequency
        };
    }

    public class RecurringJournalsViewModel : ViewModelBase
    {
        public ObservableCollection<RecurringJournalRow> Templates { get; } = new();

        private RecurringJournalRow _selectedItem;
        public RecurringJournalRow SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadTemplates(); } }
        
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public RecurringJournalsViewModel()
        {
            RefreshCommand = new RelayCommand(LoadTemplates);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteTemplate, () => SelectedItem != null);
            LoadTemplates();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadTemplates()
        {
            Templates.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT id, COALESCE(template_number,''), name, frequency, next_run_date, is_active
                    FROM recurring_journal_templates WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " AND (name ILIKE @q OR template_number ILIKE @q)";
                sql += " ORDER BY created_at DESC LIMIT 500";
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Templates.Add(new RecurringJournalRow
                    {
                        Id = r.GetInt32(0),
                        TemplateNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                        Name = r.IsDBNull(2) ? "" : r.GetString(2),
                        Frequency = r.IsDBNull(3) ? "" : r.GetString(3),
                        NextRunDate = r.IsDBNull(4) ? DateTime.MinValue : r.GetDateTime(4),
                        IsActive = r.IsDBNull(5) ? false : r.GetBoolean(5)
                    });
                StatusMessage = "تم تحميل " + Templates.Count + " قالب";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenEditWindow(int id)
        {
            try
            {
                var win = new Views.RecurringJournalsEditWindow(id);
                win.ShowDialog();
                LoadTemplates();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void DeleteTemplate()
        {
            if (SelectedItem == null) return;
            if (System.Windows.MessageBox.Show("تأكيد الحذف؟", "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM recurring_journal_templates WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", SelectedItem.Id);
                cmd.ExecuteNonQuery();
                StatusMessage = "تم الحذف";
                LoadTemplates();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }
    }
}