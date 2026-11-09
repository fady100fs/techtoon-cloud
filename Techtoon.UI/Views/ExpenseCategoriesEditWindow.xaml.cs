using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views;

public partial class ExpenseCategoriesEditWindow : Window
{
    private int _id = 0;

    public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

    public ExpenseCategoriesEditWindow(int id = 0)
    {
        InitializeComponent();
        _id = id;
        try { LblTitle.Text = id > 0 ? "\u062A\u0639\u062F\u064A\u0644 \u062A\u0635\u0646\u064A\u0641\u0627\u062A \u0627\u0644\u0645\u0635\u0631\u0648\u0641\u0627\u062A" : "\u0625\u0636\u0627\u0641\u0629 \u062A\u0635\u0646\u064A\u0641\u0627\u062A \u0627\u0644\u0645\u0635\u0631\u0648\u0641\u0627\u062A"; } catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try {
            CmbAcc.Items.Clear();
            using (var conn = new NpgsqlConnection(GetCS())) {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT id, name FROM accounts WHERE type = 5 ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbAcc.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
        } catch { }
        if (_id > 0) LoadData();
    }

    private void LoadData()
    {
        try
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(@"SELECT name, COALESCE(description,''), account_id FROM expense_categories WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                if (!r.IsDBNull(0)) TxtName.Text = r.GetValue(0).ToString();
                if (!r.IsDBNull(1)) TxtDesc.Text = r.GetValue(1).ToString();
                if (!r.IsDBNull(2) && CmbAcc.Items.Count > 0)
                    for (int j = 0; j < CmbAcc.Items.Count; j++)
                        if (((ComboItem)CmbAcc.Items[j]).Id == r.GetInt32(2)) { CmbAcc.SelectedIndex = j; break; }
            }
        }
        catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var p0 = TxtName.Text;
            var p1 = TxtDesc.Text;
            var p2 = ((ComboItem)CmbAcc.SelectedItem)?.Id ?? 0;
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            if (_id > 0)
            {
                using var cmd = new NpgsqlCommand(@"UPDATE expense_categories SET name=@p0, description=@p1, account_id=@p2 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new NpgsqlCommand(@"INSERT INTO expense_categories (name, description, account_id, is_active) VALUES (@p0, @p1, @p2, TRUE)", conn);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "ExpeneCategori", _id > 0 ? _id : 0, "Saved from EditWindow");
            DialogResult = true;
            Close();
        }
        catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_id == 0) return;
        
        try
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            
            var items = new System.Collections.Generic.List<DependencyItem>();
            
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM expenses WHERE expense_category_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", _id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                        while (rCheck.Read())
                            items.Add(new DependencyItem { Type = "Related", Display = $"Record ID: {rCheck.GetInt32(0)}", TargetWindow = "ExpensesEditWindow", TargetId = rCheck.GetInt32(0) });
                }
            
            if (items.Count > 0)
            {
                var win = new DependenciesWindow(items);
                if (win.ShowDialog() == true && win.SelectedItem != null)
                {
                    var type = Type.GetType($"Techtoon.UI.Views.{win.SelectedItem.TargetWindow}, Techtoon.UI");
                    if (type != null) ((System.Windows.Window)Activator.CreateInstance(type, win.SelectedItem.TargetId)).ShowDialog();
                }
                return;
            }
            
            if (System.Windows.MessageBox.Show("\u062A\u0623\u0643\u064A\u062F \u0627\u0644\u062D\u0630\u0641\u061F", "\u062A\u0623\u0643\u064A\u062F", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes) return;
            
            using var cmd = new NpgsqlCommand("DELETE FROM expense_categories WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            cmd.ExecuteNonQuery();
            
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "ExpeneCategori", _id > 0 ? _id : 0, "Saved from EditWindow");
            DialogResult = true;
            Close();
        }
        catch (Npgsql.PostgresException px)
        {
            System.Windows.MessageBox.Show("\u062E\u0637\u0623 \u0641\u064A \u0642\u0627\u0639\u062F\u0629 \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A: " + px.Message, "\u062E\u0637\u0623: ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("\u062E\u0637\u0623: " + ex.Message, "\u062E\u0637\u0623: ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}