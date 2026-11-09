using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views;

public partial class LoansEditWindow : Window
{
    private int _id = 0;

    public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

    public LoansEditWindow(int id = 0)
    {
        InitializeComponent();
        _id = id;
        try { LblTitle.Text = id > 0 ? "\u062A\u0639\u062F\u064A\u0644 \u0633\u0644\u0641\u0629/\u0642\u0631\u0636" : "\u0625\u0636\u0627\u0641\u0629 \u0633\u0644\u0641\u0629/\u0642\u0631\u0636"; } catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_id > 0) LoadData();
    }

    private void LoadData()
    {
        try
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(@"SELECT borrower_name, principal_amount, loan_date FROM loans WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                if (!r.IsDBNull(0)) TxtName.Text = r.GetValue(0).ToString();
                if (!r.IsDBNull(1)) TxtAmount.Text = r.GetValue(1).ToString();
                if (!r.IsDBNull(2)) DpDate.SelectedDate = r.GetDateTime(2);
            }
        }
        catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var p0 = TxtName.Text;
            var p1 = TxtAmount.Text;
            var p2 = DpDate.SelectedDate ?? DateTime.Now;
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            if (_id > 0)
            {
                using var cmd = new NpgsqlCommand(@"UPDATE loans SET borrower_name=@p0, principal_amount=@p1, loan_date=@p2 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            double _d1;
            if (!double.TryParse(p1, out _d1)) _d1 = 0;
            cmd.Parameters.AddWithValue("p1", _d1);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new NpgsqlCommand(@"INSERT INTO loans (borrower_name, borrower_type, principal_amount, loan_date, status, created_at) VALUES (@p0, 'employee', @p1, @p2, 'active', NOW())", conn);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            double _d1;
            if (!double.TryParse(p1, out _d1)) _d1 = 0;
            cmd.Parameters.AddWithValue("p1", _d1);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "Loan", _id > 0 ? _id : 0, "Saved from EditWindow");
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
            
            using var cmd = new NpgsqlCommand("DELETE FROM loans WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            cmd.ExecuteNonQuery();
            
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "Loan", _id > 0 ? _id : 0, "Saved from EditWindow");
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