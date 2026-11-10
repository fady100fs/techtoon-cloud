using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views;

public partial class PettyCashEditWindow : Window
{
    private int _id = 0;

    public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

    public PettyCashEditWindow(int id = 0)
    {
        InitializeComponent();
        _id = id;
        try { LblTitle.Text = id > 0 ? "\u062A\u0639\u062F\u064A\u0644 \u0639\u0647\u062F\u0629 \u0646\u062B\u0631\u064A\u0629" : "\u0625\u0636\u0627\u0641\u0629 \u0639\u0647\u062F\u0629 \u0646\u062B\u0631\u064A\u0629"; } catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try {
            CmbEmp.Items.Clear();
            using (var conn = new NpgsqlConnection(GetCS())) {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT id, full_name FROM employees WHERE COALESCE(status,'ACTIVE') ILIKE 'active%' ORDER BY full_name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbEmp.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
        } catch (Exception ex) { MessageBox.Show("تعذر تحميل الموظفين: " + ex.Message); }
        try {
            CmbCB.Items.Clear();
            using (var conn = new NpgsqlConnection(GetCS())) {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT id, name FROM cash_boxes WHERE COALESCE(is_active, TRUE) = TRUE ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbCB.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
        } catch { }
        try {
            CmbCustAcc.Items.Clear();
            using (var conn = new NpgsqlConnection(GetCS())) {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT id, name FROM accounts WHERE UPPER(COALESCE(type,'')) = 'ASSET' ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbCustAcc.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
        } catch (Exception ex) { MessageBox.Show("تعذر تحميل حسابات العهدة: " + ex.Message); }
        if (_id > 0) LoadData();
    }

    private void LoadData()
    {
        try
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(@"SELECT employee_id, amount, cash_box_id, custody_account_id, issued_date, COALESCE(description,'') FROM petty_cash_custodies WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                if (!r.IsDBNull(0) && CmbEmp.Items.Count > 0)
                    for (int j = 0; j < CmbEmp.Items.Count; j++)
                        if (((ComboItem)CmbEmp.Items[j]).Id == r.GetInt32(0)) { CmbEmp.SelectedIndex = j; break; }
                if (!r.IsDBNull(1)) TxtAmt.Text = r.GetValue(1).ToString();
                if (!r.IsDBNull(2) && CmbCB.Items.Count > 0)
                    for (int j = 0; j < CmbCB.Items.Count; j++)
                        if (((ComboItem)CmbCB.Items[j]).Id == r.GetInt32(2)) { CmbCB.SelectedIndex = j; break; }
                if (!r.IsDBNull(3) && CmbCustAcc.Items.Count > 0)
                    for (int j = 0; j < CmbCustAcc.Items.Count; j++)
                        if (((ComboItem)CmbCustAcc.Items[j]).Id == r.GetInt32(3)) { CmbCustAcc.SelectedIndex = j; break; }
                if (!r.IsDBNull(4)) DpDate.SelectedDate = r.GetDateTime(4);
                if (!r.IsDBNull(5)) TxtDesc.Text = r.GetValue(5).ToString();
            }
        }
        catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var p0 = ((ComboItem)CmbEmp.SelectedItem)?.Id ?? 0;
            var p1 = TxtAmt.Text;
            var p2 = ((ComboItem)CmbCB.SelectedItem)?.Id ?? 0;
            var p3 = ((ComboItem)CmbCustAcc.SelectedItem)?.Id ?? 0;
            var p4 = DpDate.SelectedDate ?? DateTime.Now;
            var p5 = TxtDesc.Text;
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            if (_id > 0)
            {
                using var cmd = new NpgsqlCommand(@"UPDATE petty_cash_custodies SET employee_id=@p0, amount=@p1, cash_box_id=@p2, custody_account_id=@p3, issued_date=@p4, description=@p5 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            decimal _decl1;
            if (!decimal.TryParse(p1, System.Globalization.CultureInfo.CurrentCulture, out _decl1)) _decl1 = 0;
            cmd.Parameters.AddWithValue("p1", _decl1);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p3", (object)p3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p5", (object)p5 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new NpgsqlCommand(@"INSERT INTO petty_cash_custodies (employee_id, amount, cash_box_id, custody_account_id, issued_date, description, status, created_by, created_at) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, 'open', 1, NOW())", conn);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            decimal _decl1;
            if (!decimal.TryParse(p1, System.Globalization.CultureInfo.CurrentCulture, out _decl1)) _decl1 = 0;
            cmd.Parameters.AddWithValue("p1", _decl1);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p3", (object)p3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p5", (object)p5 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "PettyCash", _id > 0 ? _id : 0, "Saved from EditWindow");
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
            
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM petty_cash_expenses WHERE custody_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", _id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                        while (rCheck.Read())
                            items.Add(new DependencyItem { Type = "Related", Display = $"Record ID: {rCheck.GetInt32(0)}", TargetWindow = "PettyCashExpensesEditWindow", TargetId = rCheck.GetInt32(0) });
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
            
            using var cmd = new NpgsqlCommand("DELETE FROM petty_cash_custodies WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            cmd.ExecuteNonQuery();
            
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "PettyCah", _id > 0 ? _id : 0, "Saved from EditWindow");
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