using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views;

public partial class EarlyPaymentDiscountEditWindow : Window
{
    private int _id = 0;

    public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

    public EarlyPaymentDiscountEditWindow(int id = 0)
    {
        InitializeComponent();
        _id = id;
        try { LblTitle.Text = id > 0 ? "\u062A\u0639\u062F\u064A\u0644 \u0633\u064A\u0627\u0633\u0629 \u062E\u0635\u0645 \u0627\u0644\u0633\u062F\u0627\u062F \u0627\u0644\u0645\u0628\u0643\u0631" : "\u0625\u0636\u0627\u0641\u0629 \u0633\u064A\u0627\u0633\u0629 \u062E\u0635\u0645 \u0627\u0644\u0633\u062F\u0627\u062F \u0627\u0644\u0645\u0628\u0643\u0631"; } catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CmbType.Items.Clear();
        CmbType.Items.Add(new ComboBoxItem { Content = "\u0646\u0633\u0628\u0629 %", Tag = "percentage" });
        CmbType.Items.Add(new ComboBoxItem { Content = "\u0645\u0628\u0644\u063A \u062B\u0627\u0628\u062A", Tag = "fixed" });
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
            using var cmd = new NpgsqlCommand(@"SELECT name, discount_type, discount_value, eligibility_days, applies_to FROM early_payment_discount_policies WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                if (!r.IsDBNull(0)) TxtName.Text = r.GetValue(0).ToString();
                if (!r.IsDBNull(1))
                    for (int j = 0; j < CmbType.Items.Count; j++)
                        if (((ComboBoxItem)CmbType.Items[j]).Tag?.ToString() == r.GetString(1)) { CmbType.SelectedIndex = j; break; }
                if (!r.IsDBNull(2)) TxtVal.Text = r.GetValue(2).ToString();
                if (!r.IsDBNull(3)) TxtDays.Text = r.GetValue(3).ToString();
                if (!r.IsDBNull(4))
                    for (int j = 0; j < CmbApplies.Items.Count; j++)
                        if (((ComboBoxItem)CmbApplies.Items[j]).Tag?.ToString() == r.GetString(4)) { CmbApplies.SelectedIndex = j; break; }
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
            var p3 = TxtDays.Text;
            var p4 = ((ComboBoxItem)CmbApplies.SelectedItem)?.Tag?.ToString() ?? "";
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            if (_id > 0)
            {
                using var cmd = new NpgsqlCommand(@"UPDATE early_payment_discount_policies SET name=@p0, discount_type=@p1, discount_value=@p2, eligibility_days=@p3, applies_to=@p4 WHERE id=@id", conn);
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
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new NpgsqlCommand(@"INSERT INTO early_payment_discount_policies (name, discount_type, discount_value, eligibility_days, applies_to, is_active, effective_from, created_by, created_at) VALUES (@p0, @p1, @p2, @p3, @p4, TRUE, CURRENT_DATE, 1, NOW())", conn);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            double _d2;
            if (!double.TryParse(p2, out _d2)) _d2 = 0;
            cmd.Parameters.AddWithValue("p2", _d2);
            int _i3;
            if (!int.TryParse(p3, out _i3)) _i3 = 0;
            cmd.Parameters.AddWithValue("p3", _i3);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "EarlyPaymentDicount", _id > 0 ? _id : 0, "Saved from EditWindow");
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
            
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM invoice_discount_applications WHERE policy_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", _id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                        while (rCheck.Read())
                            items.Add(new DependencyItem { Type = "Related", Display = $"Record ID: {rCheck.GetInt32(0)}", TargetWindow = "InvoiceDiscountApplicationsEditWindow", TargetId = rCheck.GetInt32(0) });
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
            
            using var cmd = new NpgsqlCommand("DELETE FROM early_payment_discount_policies WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            cmd.ExecuteNonQuery();
            
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "EarlyPaymentDicount", _id > 0 ? _id : 0, "Saved from EditWindow");
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