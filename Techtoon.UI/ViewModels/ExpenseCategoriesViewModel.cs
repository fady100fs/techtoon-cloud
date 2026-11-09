using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class ExpenseCategoryRow : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AccountName { get; set; }
        public bool IsActive { get; set; }
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    }

    public class ExpenseCategoriesViewModel : ViewModelBase
    {
        public ObservableCollection<ExpenseCategoryRow> Categories { get; } = new();

        private ExpenseCategoryRow _selectedItem;
        public ExpenseCategoryRow SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { SetProperty(ref _searchText, value); LoadCategories(); } }
        
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public ExpenseCategoriesViewModel()
        {
            RefreshCommand = new RelayCommand(LoadCategories);
            AddCommand = new RelayCommand(() => OpenEditWindow(0));
            EditCommand = new RelayCommand(() => OpenEditWindow(SelectedItem?.Id ?? 0), () => SelectedItem != null);
            DeleteCommand = new RelayCommand(DeleteCategory, () => SelectedItem != null);
            LoadCategories();
        }

        public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

        public void LoadCategories()
        {
            Categories.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT ec.id, ec.name, COALESCE(ec.description,''), COALESCE(a.name,''), COALESCE(ec.is_active, TRUE) FROM expense_categories ec LEFT JOIN accounts a ON a.id = ec.account_id ORDER BY ec.name LIMIT 500";
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    sql = sql.Replace("ORDER BY", " AND ec.name ILIKE @q ORDER BY");
                }
                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Categories.Add(new ExpenseCategoryRow
                    {
                        Id = r.GetInt32(0),
                        Name = r.IsDBNull(1) ? "" : r.GetString(1),
                        Description = r.IsDBNull(2) ? "" : r.GetString(2),
                        AccountName = r.IsDBNull(3) ? "" : r.GetString(3),
                        IsActive = r.IsDBNull(4) ? true : r.GetBoolean(4)
                    });
                StatusMessage = "تم تحميل " + Categories.Count + " سجل";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenEditWindow(int id)
        {
            try
            {
                var win = new Views.ExpenseCategoriesEditWindow(id);
                win.ShowDialog();
                LoadCategories();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void DeleteCategory()
        {
            if (SelectedItem == null) return;
            int id = SelectedItem.Id;
            
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                
                var items = new System.Collections.Generic.List<Views.DependencyItem>();
                
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM expenses WHERE expense_category_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                    {
                        while (rCheck.Read())
                        {
                            string val = rCheck.IsDBNull(1) ? "" : rCheck.GetValue(1).ToString();
                            items.Add(new Views.DependencyItem { Type = "مصروف", Display = $"مصروف رقم: {rCheck.GetInt32(0)}", TargetWindow = "", TargetId = rCheck.GetInt32(0) });
                        }
                    }
                }
                
                if (items.Count > 0)
                {
                    var win = new Views.DependenciesWindow(items);
                    if (win.ShowDialog() == true && win.SelectedItem != null)
                    {
                        OpenDependencyWindow?.Invoke(win.SelectedItem.TargetWindow, win.SelectedItem.TargetId);
                    }
                    return;
                }
                
                if (System.Windows.MessageBox.Show("تأكيد الحذف؟", "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
                
                using var cmd = new NpgsqlCommand("DELETE FROM expense_categories WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", id);
                cmd.ExecuteNonQuery();
                
                StatusMessage = "تم الحذف بنجاح";
                LoadCategories();
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

