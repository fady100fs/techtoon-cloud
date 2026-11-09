using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views;

public partial class EmployeesEditWindow : Window
{
    private int _id = 0;

    public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

    public EmployeesEditWindow(int id = 0)
    {
        InitializeComponent();
        _id = id;
        try { LblTitle.Text = id > 0 ? "\u062A\u0639\u062F\u064A\u0644 \u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0645\u0648\u0638\u0641\u064A\u0646" : "\u0625\u0636\u0627\u0641\u0629 \u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0645\u0648\u0638\u0641\u064A\u0646"; } catch { }
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
            using var cmd = new NpgsqlCommand(@"SELECT employee_code, full_name, COALESCE(job_title,''), COALESCE(department,''), COALESCE(phone,''), basic_salary, hire_date FROM employees WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                if (!r.IsDBNull(0)) TxtCode.Text = r.GetValue(0).ToString();
                if (!r.IsDBNull(1)) TxtName.Text = r.GetValue(1).ToString();
                if (!r.IsDBNull(2)) TxtJob.Text = r.GetValue(2).ToString();
                if (!r.IsDBNull(3)) TxtDept.Text = r.GetValue(3).ToString();
                if (!r.IsDBNull(4)) TxtPhone.Text = r.GetValue(4).ToString();
                if (!r.IsDBNull(5)) TxtSalary.Text = r.GetValue(5).ToString();
                if (!r.IsDBNull(6)) DpHire.SelectedDate = r.GetDateTime(6);
            }
        }
        catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var p0 = TxtCode.Text;
            var p1 = TxtName.Text;
            var p2 = TxtJob.Text;
            var p3 = TxtDept.Text;
            var p4 = TxtPhone.Text;
            var p5 = TxtSalary.Text;
            var p6 = DpHire.SelectedDate ?? DateTime.Now;
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            if (_id > 0)
            {
                using var cmd = new NpgsqlCommand(@"UPDATE employees SET employee_code=@p0, full_name=@p1, job_title=@p2, department=@p3, phone=@p4, basic_salary=@p5, hire_date=@p6 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p3", (object)p3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
            double _d5;
            if (!double.TryParse(p5, out _d5)) _d5 = 0;
            cmd.Parameters.AddWithValue("p5", _d5);
            cmd.Parameters.AddWithValue("p6", (object)p6 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new NpgsqlCommand(@"INSERT INTO employees (employee_code, full_name, job_title, department, phone, basic_salary, hire_date, status) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, 'active')", conn);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p2", (object)p2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p3", (object)p3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
            double _d5;
            if (!double.TryParse(p5, out _d5)) _d5 = 0;
            cmd.Parameters.AddWithValue("p5", _d5);
            cmd.Parameters.AddWithValue("p6", (object)p6 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "Employe", _id > 0 ? _id : 0, "Saved from EditWindow");
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
            
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM salary_records WHERE employee_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", _id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                        while (rCheck.Read())
                            items.Add(new DependencyItem { Type = "Related", Display = $"Record ID: {rCheck.GetInt32(0)}", TargetWindow = "SalariesEditWindow", TargetId = rCheck.GetInt32(0) });
                }
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM petty_cash_custodies WHERE employee_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", _id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                        while (rCheck.Read())
                            items.Add(new DependencyItem { Type = "Related", Display = $"Record ID: {rCheck.GetInt32(0)}", TargetWindow = "PettyCashEditWindow", TargetId = rCheck.GetInt32(0) });
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
            
            using var cmd = new NpgsqlCommand("DELETE FROM employees WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            cmd.ExecuteNonQuery();
            
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "Employe", _id > 0 ? _id : 0, "Saved from EditWindow");
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