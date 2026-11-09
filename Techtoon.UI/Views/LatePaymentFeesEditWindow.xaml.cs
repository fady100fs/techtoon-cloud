using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views;

public partial class LatePaymentFeesEditWindow : Window
{
    private int _id = 0;

    public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

    public LatePaymentFeesEditWindow(int id = 0)
    {
        InitializeComponent();
        _id = id;
        try { LblTitle.Text = id > 0 ? "\u062A\u0639\u062F\u064A\u0644 \u0633\u064A\u0627\u0633\u0629 \u0631\u0633\u0648\u0645 \u0627\u0644\u062A\u0623\u062E\u064A\u0631" : "\u0625\u0636\u0627\u0641\u0629 \u0633\u064A\u0627\u0633\u0629 \u0631\u0633\u0648\u0645 \u0627\u0644\u062A\u0623\u062E\u064A\u0631"; } catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CmbType.Items.Clear();
        CmbType.Items.Add(new ComboBoxItem { Content = "\u0646\u0633\u0628\u0629 %", Tag = "percentage" });
        CmbType.Items.Add(new ComboBoxItem { Content = "\u0645\u0628\u0644\u063A \u062B\u0627\u0628\u062A", Tag = "fixed" });
        CmbFreq.Items.Clear();
        CmbFreq.Items.Add(new ComboBoxItem { Content = "\u0645\u0631\u0629 \u0648\u0627\u062D\u062F\u0629", Tag = "one_time" });
        CmbFreq.Items.Add(new ComboBoxItem { Content = "\u064A\u0648\u0645\u064A", Tag = "daily" });
        CmbFreq.Items.Add(new ComboBoxItem { Content = "\u0623\u0633\u0628\u0648\u0639\u064A", Tag = "weekly" });
        CmbFreq.Items.Add(new ComboBoxItem { Content = "\u0634\u0647\u0631\u064A", Tag = "monthly" });
        CmbBasis.Items.Clear();
        CmbBasis.Items.Add(new ComboBoxItem { Content = "\u0627\u0644\u0631\u0635\u064A\u062F \u0627\u0644\u0645\u062A\u0623\u062E\u0631", Tag = "overdue_balance" });
        CmbBasis.Items.Add(new ComboBoxItem { Content = "\u0625\u062C\u0645\u0627\u0644\u064A \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629", Tag = "invoice_total" });
        CmbBasis.Items.Add(new ComboBoxItem { Content = "\u0627\u0644\u0631\u0635\u064A\u062F \u0627\u0644\u0645\u062A\u0628\u0642\u064A", Tag = "remaining_balance" });
        CmbApplies.Items.Clear();
        CmbApplies.Items.Add(new ComboBoxItem { Content = "\u0627\u0644\u0643\u0644", Tag = "all" });
        CmbApplies.Items.Add(new ComboBoxItem { Content = "\u0627\u0644\u0639\u0645\u0644\u0627\u0621", Tag = "customers" });
        CmbApplies.Items.Add(new ComboBoxItem { Content = "\u0627\u0644\u0645\u0648\u0631\u062F\u0648\u0646", Tag = "suppliers" });
        if (_id > 0) LoadData();
    }

    private void LoadData()
    {
        try
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(@"SELECT name, fee_type, fee_value, grace_period_days, calculation_frequency, calculation_basis, applies_to FROM late_fee_policies WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                if (!r.IsDBNull(0)) TxtName.Text = r.GetValue(0).ToString();
                if (!r.IsDBNull(1))
                    for (int j = 0; j < CmbType.Items.Count; j++)
                        if (((ComboBoxItem)CmbType.Items[j]).Tag?.ToString() == r.GetString(1)) { CmbType.SelectedIndex = j; break; }
                if (!r.IsDBNull(2)) TxtVal.Text = r.GetValue(2).ToString();
                if (!r.IsDBNull(3)) TxtGrace.Text = r.GetValue(3).ToString();
                if (!r.IsDBNull(4))
                    for (int j = 0; j < CmbFreq.Items.Count; j++)
                        if (((ComboBoxItem)CmbFreq.Items[j]).Tag?.ToString() == r.GetString(4)) { CmbFreq.SelectedIndex = j; break; }
                if (!r.IsDBNull(5))
                    for (int j = 0; j < CmbBasis.Items.Count; j++)
                        if (((ComboBoxItem)CmbBasis.Items[j]).Tag?.ToString() == r.GetString(5)) { CmbBasis.SelectedIndex = j; break; }
                if (!r.IsDBNull(6))
                    for (int j = 0; j < CmbApplies.Items.Count; j++)
                        if (((ComboBoxItem)CmbApplies.Items[j]).Tag?.ToString() == r.GetString(6)) { CmbApplies.SelectedIndex = j; break; }
            }
        }
        catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var p0 = TxtName.Text;
            var p1 = ((ComboBoxItem)CmbType.SelectedItem)?.Tag?.ToString() ?? "";
            var p2 = TxtVal.Text;
            var p3 = TxtGrace.Text;
            var p4 = ((ComboBoxItem)CmbFreq.SelectedItem)?.Tag?.ToString() ?? "";
            var p5 = ((ComboBoxItem)CmbBasis.SelectedItem)?.Tag?.ToString() ?? "";
            var p6 = ((ComboBoxItem)CmbApplies.SelectedItem)?.Tag?.ToString() ?? "";
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            if (_id > 0)
            {
                using var cmd = new NpgsqlCommand(@"UPDATE late_fee_policies SET name=@p0, fee_type=@p1, fee_value=@p2, grace_period_days=@p3, calculation_frequency=@p4, calculation_basis=@p5, applies_to=@p6 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            double _d2;
            if (!double.TryParse(p2, out _d2)) _d2 = 0;
            cmd.Parameters.AddWithValue("p2", _d2);
            int _i3;
            if (!int.TryParse(p3, out _i3)) _i3 = 0;
            cmd.Parameters.AddWithValue("p3", _i3);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p5", (object)p5 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p6", (object)p6 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new NpgsqlCommand(@"INSERT INTO late_fee_policies (name, fee_type, fee_value, grace_period_days, calculation_frequency, calculation_basis, applies_to, is_active, effective_from, created_by, created_at) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, TRUE, CURRENT_DATE, 1, NOW())", conn);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            double _d2;
            if (!double.TryParse(p2, out _d2)) _d2 = 0;
            cmd.Parameters.AddWithValue("p2", _d2);
            int _i3;
            if (!int.TryParse(p3, out _i3)) _i3 = 0;
            cmd.Parameters.AddWithValue("p3", _i3);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p5", (object)p5 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p6", (object)p6 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "LatePaymentFe", _id > 0 ? _id : 0, "Saved from EditWindow");
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
            
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM invoice_late_fees WHERE policy_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", _id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                        while (rCheck.Read())
                            items.Add(new DependencyItem { Type = "Related", Display = $"Record ID: {rCheck.GetInt32(0)}", TargetWindow = "InvoiceLateFeesEditWindow", TargetId = rCheck.GetInt32(0) });
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
            
            using var cmd = new NpgsqlCommand("DELETE FROM late_fee_policies WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            cmd.ExecuteNonQuery();
            
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "LatePaymentFe", _id > 0 ? _id : 0, "Saved from EditWindow");
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