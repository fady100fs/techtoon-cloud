using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Techtoon.UI.ViewModels
{
    public class AccountNode : ViewModelBase
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int? ParentId { get; set; }
        public ObservableCollection<AccountNode> Children { get; } = new();
        public string Display => $"{Code} - {Name}";
        public string TypeDisplay => Type switch
        {
            "ASSET" => "\u0623\u0635\u0644",
            "LIABILITY" => "\u062E\u0635\u0645",
            "EQUITY" => "\u062D\u0642 \u0645\u0644\u0643\u064A\u0629",
            "REVENUE" => "\u0625\u064A\u0631\u0627\u062F",
            "EXPENSE" => "\u0645\u0635\u0631\u0648\u0641",
            _ => Type ?? ""
        };
    }

    public class AccountsViewModel : ViewModelBase
    {
        public ObservableCollection<AccountNode> RootAccounts { get; } = new();
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        private AccountNode _selectedNode;
        public AccountNode SelectedNode { get => _selectedNode; set => SetProperty(ref _selectedNode, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddRootCommand { get; }
        public ICommand AddChildCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public AccountsViewModel()
        {
            RefreshCommand = new RelayCommand(LoadAccounts);
            AddRootCommand = new RelayCommand(() => ShowEditWindow(null));
            AddChildCommand = new RelayCommand(() => { if (SelectedNode != null) ShowEditWindow(SelectedNode); }, () => SelectedNode != null);
            EditCommand = new RelayCommand(() => { if (SelectedNode != null) ShowEditWindow(null, SelectedNode); }, () => SelectedNode != null);
            DeleteCommand = new RelayCommand(DeleteAccount, () => SelectedNode != null);
            LoadAccounts();
        }

        public static string GetCS() => "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";

        public void LoadAccounts()
        {
            RootAccounts.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                var all = new System.Collections.Generic.List<AccountNode>();
                using var cmd = new NpgsqlCommand("SELECT id, code, name, type, parent_id FROM accounts ORDER BY code", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    all.Add(new AccountNode { Id = r.GetInt32(0), Code = r.IsDBNull(1) ? "" : r.GetString(1), Name = r.IsDBNull(2) ? "" : r.GetString(2), Type = r.IsDBNull(3) ? "" : r.GetString(3), ParentId = r.IsDBNull(4) ? null : r.GetInt32(4) });
                foreach (var a in all.Where(x => x.ParentId == null)) { BuildTree(a, all); RootAccounts.Add(a); }
                StatusMessage = "\u062A\u0645 \u062A\u062D\u0645\u064A\u0644 " + all.Count + " \u062D\u0633\u0627\u0628";
            }
            catch (Exception ex) { StatusMessage = "\u062E\u0637\u0623: " + ex.Message; }
        }

        private void BuildTree(AccountNode parent, System.Collections.Generic.List<AccountNode> all)
        {
            foreach (var c in all.Where(x => x.ParentId == parent.Id)) { parent.Children.Add(c); BuildTree(c, all); }
        }

        private void ShowEditWindow(AccountNode parentNode, AccountNode editNode = null)
        {
            bool isEdit = editNode != null;
            bool isChild = parentNode != null;
            var win = new System.Windows.Window
            {
                Title = isEdit ? "\u062A\u0639\u062F\u064A\u0644 \u062D\u0633\u0627\u0628" : (isChild ? "\u0625\u0636\u0627\u0641\u0629 \u062D\u0633\u0627\u0628 \u0641\u0631\u0639\u064A" : "\u0625\u0636\u0627\u0641\u0629 \u062D\u0633\u0627\u0628 \u0631\u0626\u064A\u0633\u064A"),
                Width = 400, Height = 320, WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                FlowDirection = System.Windows.FlowDirection.RightToLeft, FontFamily = new System.Windows.Media.FontFamily("Tahoma"),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 247, 250))
            };

            var panel = new System.Windows.Controls.StackPanel { Margin = new System.Windows.Thickness(20) };
            var txtCode = new System.Windows.Controls.TextBox { Padding = new System.Windows.Thickness(6, 4, 6, 4), Margin = new System.Windows.Thickness(0, 0, 0, 10) };
            var txtName = new System.Windows.Controls.TextBox { Padding = new System.Windows.Thickness(6, 4, 6, 4), Margin = new System.Windows.Thickness(0, 0, 0, 10) };
            var cmbType = new System.Windows.Controls.ComboBox { Padding = new System.Windows.Thickness(6, 4, 6, 4), Margin = new System.Windows.Thickness(0, 0, 0, 10) };
            cmbType.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "\u0623\u0635\u0644", Tag = "ASSET" });
            cmbType.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "\u062E\u0635\u0645", Tag = "LIABILITY" });
            cmbType.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "\u062D\u0642 \u0645\u0644\u0643\u064A\u0629", Tag = "EQUITY" });
            cmbType.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "\u0625\u064A\u0631\u0627\u062F", Tag = "REVENUE" });
            cmbType.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "\u0645\u0635\u0631\u0648\u0641", Tag = "EXPENSE" });
            cmbType.SelectedIndex = 0;

            if (isEdit)
            {
                txtCode.Text = editNode.Code;
                txtName.Text = editNode.Name;
                for (int i = 0; i < cmbType.Items.Count; i++)
                    if ((cmbType.Items[i] as System.Windows.Controls.ComboBoxItem).Tag?.ToString() == editNode.Type) { cmbType.SelectedIndex = i; break; }
            }
            else if (isChild)
            {
                cmbType.IsEnabled = false;
                for (int i = 0; i < cmbType.Items.Count; i++)
                    if ((cmbType.Items[i] as System.Windows.Controls.ComboBoxItem).Tag?.ToString() == parentNode.Type) { cmbType.SelectedIndex = i; break; }
            }

            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "\u0627\u0644\u0643\u0648\u062F:", Margin = new System.Windows.Thickness(0, 0, 0, 3) });
            panel.Children.Add(txtCode);
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "\u0627\u0644\u0627\u0633\u0645:", Margin = new System.Windows.Thickness(0, 0, 0, 3) });
            panel.Children.Add(txtName);
            panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "\u0627\u0644\u0646\u0648\u0639:", Margin = new System.Windows.Thickness(0, 0, 0, 3) });
            panel.Children.Add(cmbType);

            var btnOk = new System.Windows.Controls.Button { Content = "\u062D\u0641\u0638", Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 137, 123)), Foreground = System.Windows.Media.Brushes.White, FontWeight = System.Windows.FontWeights.Bold, Padding = new System.Windows.Thickness(30, 8, 30, 8), BorderThickness = new System.Windows.Thickness(0), HorizontalAlignment = System.Windows.HorizontalAlignment.Center };
            panel.Children.Add(btnOk);
            win.Content = panel;

            btnOk.Click += (s, e) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(txtName.Text)) { System.Windows.MessageBox.Show("\u0623\u062F\u062E\u0644 \u0627\u0644\u0627\u0633\u0645"); return; }
                    string type = (cmbType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "ASSET";
                    int? parentId = isChild ? parentNode.Id : (isEdit ? editNode.ParentId : null);

                    using var conn = new NpgsqlConnection(GetCS());
                    conn.Open();
                    if (isEdit)
                    {
                        using var cmd = new NpgsqlCommand("UPDATE accounts SET code=@c, name=@n, type=@t WHERE id=@id", conn);
                        cmd.Parameters.AddWithValue("c", txtCode.Text.Trim());
                        cmd.Parameters.AddWithValue("n", txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("t", type);
                        cmd.Parameters.AddWithValue("id", editNode.Id);
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        using var cmd = new NpgsqlCommand("INSERT INTO accounts (code, name, type, parent_id) VALUES (@c, @n, @t, @p)", conn);
                        cmd.Parameters.AddWithValue("c", txtCode.Text.Trim());
                        cmd.Parameters.AddWithValue("n", txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("t", type);
                        cmd.Parameters.AddWithValue("p", (object)parentId ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                    win.Close();
                    LoadAccounts();
                }
                catch (Exception ex) { System.Windows.MessageBox.Show("\u062E\u0637\u0623: " + ex.Message); }
            };

            win.ShowDialog();
        }

        private void DeleteAccount()
        {
            if (SelectedNode == null) return;
            if (System.Windows.MessageBox.Show("\u062D\u0630\u0641 \u0627\u0644\u062D\u0633\u0627\u0628\u061F", "\u062A\u0623\u0643\u064A\u062F", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
            try { using var conn = new NpgsqlConnection(GetCS()); conn.Open(); using var cmd = new NpgsqlCommand("DELETE FROM accounts WHERE id=@id", conn); cmd.Parameters.AddWithValue("id", SelectedNode.Id); cmd.ExecuteNonQuery(); LoadAccounts(); }
            catch (Exception ex) { StatusMessage = "\u062E\u0637\u0623: " + ex.Message; }
        }
    }
}
