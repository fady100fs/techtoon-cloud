using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views;

public partial class BankReconciliationEditWindow : Window
{
    private int _id = 0;

    public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

    public BankReconciliationEditWindow(int id = 0)
    {
        InitializeComponent();
        _id = id;
        try { LblTitle.Text = id > 0 ? "\u062A\u0639\u062F\u064A\u0644 \u062A\u0633\u0648\u064A\u0629 \u0628\u0646\u0643\u064A\u0629" : "\u0625\u0636\u0627\u0641\u0629 \u062A\u0633\u0648\u064A\u0629 \u0628\u0646\u0643\u064A\u0629"; } catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
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
        if (_id > 0) LoadData();
    }

    private void LoadData()
    {
        try
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(@"SELECT cash_box_id, period_start, period_end, opening_book_balance, opening_bank_balance, closing_book_balance, closing_bank_balance FROM bank_reconciliations WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                if (!r.IsDBNull(0) && CmbCB.Items.Count > 0)
                    for (int j = 0; j < CmbCB.Items.Count; j++)
                        if (((ComboItem)CmbCB.Items[j]).Id == r.GetInt32(0)) { CmbCB.SelectedIndex = j; break; }
                if (!r.IsDBNull(1)) DpPS.SelectedDate = Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(1));
                if (!r.IsDBNull(2)) DpPE.SelectedDate = Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(2));
                if (!r.IsDBNull(3)) TxtOBB.Text = r.GetValue(3).ToString();
                if (!r.IsDBNull(4)) TxtOBank.Text = r.GetValue(4).ToString();
                if (!r.IsDBNull(5)) TxtCBB.Text = r.GetValue(5).ToString();
                if (!r.IsDBNull(6)) TxtCBank.Text = r.GetValue(6).ToString();
            }
        }
        catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var p0 = ((ComboItem)CmbCB.SelectedItem)?.Id ?? 0;
            var p1 = DpPS.SelectedDate ?? DateTime.Now;
            var p2 = DpPE.SelectedDate ?? DateTime.Now;
            var p3 = TxtOBB.Text;
            var p4 = TxtOBank.Text;
            var p5 = TxtCBB.Text;
            var p6 = TxtCBank.Text;
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            if (_id > 0)
            {
                using var cmd = new NpgsqlCommand(@"UPDATE bank_reconciliations SET cash_box_id=@p0, period_start=@p1, period_end=@p2, opening_book_balance=@p3, opening_bank_balance=@p4, closing_book_balance=@p5, closing_bank_balance=@p6 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p3", (object)p3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p5", (object)p5 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p6", (object)p6 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new NpgsqlCommand(@"INSERT INTO bank_reconciliations (cash_box_id, period_start, period_end, opening_book_balance, opening_bank_balance, closing_book_balance, closing_bank_balance, difference, status, created_by, created_at) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p5 - @p6, 'draft', 1, NOW())", conn);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p3", (object)p3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p5", (object)p5 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p6", (object)p6 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "BankReconciliation", _id > 0 ? _id : 0, "Saved from EditWindow");
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
            
            using var cmd = new NpgsqlCommand("DELETE FROM bank_reconciliations WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            cmd.ExecuteNonQuery();
            
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "BankReconciliation", _id > 0 ? _id : 0, "Saved from EditWindow");
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