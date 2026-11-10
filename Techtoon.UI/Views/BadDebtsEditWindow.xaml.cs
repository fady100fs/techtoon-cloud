using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views;

public partial class BadDebtsEditWindow : Window
{
    private int _id = 0;

    public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

    public BadDebtsEditWindow(int id = 0)
    {
        InitializeComponent();
        _id = id;
        try { LblTitle.Text = id > 0 ? "\u062A\u0639\u062F\u064A\u0644 \u0634\u0637\u0628 \u062F\u064A\u0646 \u0645\u0639\u062F\u0648\u0645" : "\u0625\u0636\u0627\u0641\u0629 \u0634\u0637\u0628 \u062F\u064A\u0646 \u0645\u0639\u062F\u0648\u0645"; } catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try {
            CmbParty.Items.Clear();
            using (var conn = new NpgsqlConnection(GetCS())) {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT id, name FROM parties ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbParty.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
        } catch { }
        try {
            CmbBdAcc.Items.Clear();
            using (var conn = new NpgsqlConnection(GetCS())) {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT id, name FROM accounts WHERE type = 5 ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbBdAcc.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
        } catch { }
        try {
            CmbArAcc.Items.Clear();
            using (var conn = new NpgsqlConnection(GetCS())) {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT id, name FROM accounts WHERE type = 1 ORDER BY name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbArAcc.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
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
            using var cmd = new NpgsqlCommand(@"SELECT party_id, writeoff_amount, COALESCE(reason,''), writeoff_date, bad_debt_account_id, ar_account_id FROM bad_debt_writeoffs WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                if (!r.IsDBNull(0) && CmbParty.Items.Count > 0)
                    for (int j = 0; j < CmbParty.Items.Count; j++)
                        if (((ComboItem)CmbParty.Items[j]).Id == r.GetInt32(0)) { CmbParty.SelectedIndex = j; break; }
                if (!r.IsDBNull(1)) TxtAmt.Text = r.GetValue(1).ToString();
                if (!r.IsDBNull(2)) TxtReason.Text = r.GetValue(2).ToString();
                if (!r.IsDBNull(3)) DpDate.SelectedDate = Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(3));
                if (!r.IsDBNull(4) && CmbBdAcc.Items.Count > 0)
                    for (int j = 0; j < CmbBdAcc.Items.Count; j++)
                        if (((ComboItem)CmbBdAcc.Items[j]).Id == r.GetInt32(4)) { CmbBdAcc.SelectedIndex = j; break; }
                if (!r.IsDBNull(5) && CmbArAcc.Items.Count > 0)
                    for (int j = 0; j < CmbArAcc.Items.Count; j++)
                        if (((ComboItem)CmbArAcc.Items[j]).Id == r.GetInt32(5)) { CmbArAcc.SelectedIndex = j; break; }
            }
        }
        catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var p0 = ((ComboItem)CmbParty.SelectedItem)?.Id ?? 0;
            var p1 = TxtAmt.Text;
            var p2 = TxtReason.Text;
            var p3 = DpDate.SelectedDate ?? DateTime.Now;
            var p4 = ((ComboItem)CmbBdAcc.SelectedItem)?.Id ?? 0;
            var p5 = ((ComboItem)CmbArAcc.SelectedItem)?.Id ?? 0;
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            if (_id > 0)
            {
                using var cmd = new NpgsqlCommand(@"UPDATE bad_debt_writeoffs SET party_id=@p0, writeoff_amount=@p1, reason=@p2, writeoff_date=@p3, bad_debt_account_id=@p4, ar_account_id=@p5 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            double _d1;
            if (!double.TryParse(p1, out _d1)) _d1 = 0;
            cmd.Parameters.AddWithValue("p1", _d1);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p3", (object)p3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p5", (object)p5 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new NpgsqlCommand(@"INSERT INTO bad_debt_writeoffs (party_id, writeoff_amount, reason, writeoff_date, bad_debt_account_id, ar_account_id, status, requested_by, requested_at, writeoff_type, original_amount, remaining_amount) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, 'pending', 1, NOW(), 'full', @p1, @p1)", conn);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            double _d1;
            if (!double.TryParse(p1, out _d1)) _d1 = 0;
            cmd.Parameters.AddWithValue("p1", _d1);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p3", (object)p3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p5", (object)p5 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "BadDebt", _id > 0 ? _id : 0, "Saved from EditWindow");
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
            
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM bad_debt_reversals WHERE writeoff_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", _id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                        while (rCheck.Read())
                            items.Add(new DependencyItem { Type = "Related", Display = $"Record ID: {rCheck.GetInt32(0)}", TargetWindow = "BadDebtReversalsEditWindow", TargetId = rCheck.GetInt32(0) });
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
            
            using var cmd = new NpgsqlCommand("DELETE FROM bad_debt_writeoffs WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            cmd.ExecuteNonQuery();
            
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "BadDebt", _id > 0 ? _id : 0, "Saved from EditWindow");
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