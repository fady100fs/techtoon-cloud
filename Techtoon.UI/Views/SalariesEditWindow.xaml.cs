using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views;

public partial class SalariesEditWindow : Window
{
    private int _id = 0;

    public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

    public SalariesEditWindow(int id = 0)
    {
        InitializeComponent();
        _id = id;
        try { LblTitle.Text = id > 0 ? "\u062A\u0639\u062F\u064A\u0644 \u0635\u0631\u0641 \u0645\u0631\u062A\u0628" : "\u0625\u0636\u0627\u0641\u0629 \u0635\u0631\u0641 \u0645\u0631\u062A\u0628"; } catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try {
            CmbEmp.Items.Clear();
            using (var conn = new NpgsqlConnection(GetCS())) {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"SELECT id, full_name FROM employees WHERE status = 'active' OR status = '1' ORDER BY full_name", conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    CmbEmp.Items.Add(new ComboItem { Id = r.GetInt32(0), Name = r.GetString(1) });
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
            using var cmd = new NpgsqlCommand(@"SELECT employee_id, month, year, net_salary, payment_date FROM salary_records WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                if (!r.IsDBNull(0) && CmbEmp.Items.Count > 0)
                    for (int j = 0; j < CmbEmp.Items.Count; j++)
                        if (((ComboItem)CmbEmp.Items[j]).Id == r.GetInt32(0)) { CmbEmp.SelectedIndex = j; break; }
                if (!r.IsDBNull(1)) TxtMonth.Text = r.GetValue(1).ToString();
                if (!r.IsDBNull(2)) TxtYear.Text = r.GetValue(2).ToString();
                if (!r.IsDBNull(3)) TxtNet.Text = r.GetValue(3).ToString();
                if (!r.IsDBNull(4)) DpDate.SelectedDate = Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(4));
            }
        }
        catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var p0 = ((ComboItem)CmbEmp.SelectedItem)?.Id ?? 0;
            var p1 = TxtMonth.Text;
            var p2 = TxtYear.Text;
            var p3 = TxtNet.Text;
            var p4 = DpDate.SelectedDate ?? DateTime.Now;
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            if (_id > 0)
            {
                using var cmd = new NpgsqlCommand(@"UPDATE salary_records SET employee_id=@p0, month=@p1, year=@p2, net_salary=@p3, payment_date=@p4 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            int _i1;
            if (!int.TryParse(p1, out _i1)) _i1 = 0;
            cmd.Parameters.AddWithValue("p1", _i1);
            int _i2;
            if (!int.TryParse(p2, out _i2)) _i2 = 0;
            cmd.Parameters.AddWithValue("p2", _i2);
            double _d3;
            if (!double.TryParse(p3, out _d3)) _d3 = 0;
            cmd.Parameters.AddWithValue("p3", _d3);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new NpgsqlCommand(@"INSERT INTO salary_records (employee_id, month, year, basic_salary, gross_salary, net_salary, payment_date, status, created_at) VALUES (@p0, @p1, @p2, @p3, @p3, @p3, @p4, 'paid', NOW())", conn);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            int _i1;
            if (!int.TryParse(p1, out _i1)) _i1 = 0;
            cmd.Parameters.AddWithValue("p1", _i1);
            int _i2;
            if (!int.TryParse(p2, out _i2)) _i2 = 0;
            cmd.Parameters.AddWithValue("p2", _i2);
            double _d3;
            if (!double.TryParse(p3, out _d3)) _d3 = 0;
            cmd.Parameters.AddWithValue("p3", _d3);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "Salari", _id > 0 ? _id : 0, "Saved from EditWindow");
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
            
            using var cmd = new NpgsqlCommand("DELETE FROM salary_records WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            cmd.ExecuteNonQuery();
            
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "Salari", _id > 0 ? _id : 0, "Saved from EditWindow");
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