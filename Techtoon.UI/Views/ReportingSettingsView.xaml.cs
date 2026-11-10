using System;
using System.Globalization;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views
{
    /// <summary>P5: إعدادات التقارير — الأساس المحاسبي + المقارنة + عتبة التقدير.</summary>
    public partial class ReportingSettingsView : UserControl
    {
        public ReportingSettingsView()
        {
            InitializeComponent();
            Loaded += (_, _) => LoadSettings();
            BtnSave.Click += Save_Click;
        }

        private void LoadSettings()
        {
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT COALESCE(default_basis,'accrual'), COALESCE(allow_comparison,TRUE), COALESCE(cash_recognition_threshold_days,30) FROM reporting_settings ORDER BY id LIMIT 1", conn);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    var basis = r.GetString(0);
                    foreach (ComboBoxItem item in CmbBasis.Items)
                        if ((string)item.Tag == basis) { CmbBasis.SelectedItem = item; break; }
                    ChkComparison.IsChecked = r.GetBoolean(1);
                    TxtThreshold.Text = r.GetInt32(2).ToString();
                }
                else
                {
                    CmbBasis.SelectedIndex = 0;
                    ChkComparison.IsChecked = true;
                    TxtThreshold.Text = "30";
                }
            }
            catch (Exception ex) { LblStatus.Text = "خطأ التحميل: " + ex.Message; }
        }

        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var basis = ((ComboBoxItem)CmbBasis.SelectedItem)?.Tag?.ToString() ?? "accrual";
            if (!int.TryParse(TxtThreshold.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var threshold) || threshold < 0)
            { LblStatus.Text = "العتبة غير صحيحة."; return; }
            try
            {
                using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
                conn.Open();
                using var tx = conn.BeginTransaction();
                int updated;
                using (var upd = new NpgsqlCommand(@"UPDATE reporting_settings SET default_basis=@b, allow_comparison=@c,
                    cash_recognition_threshold_days=@t, updated_by=1, updated_at=NOW() RETURNING id", conn, tx))
                {
                    upd.Parameters.AddWithValue("@b", basis);
                    upd.Parameters.AddWithValue("@c", (bool)(ChkComparison.IsChecked == true));
                    upd.Parameters.AddWithValue("@t", threshold);
                    var v = upd.ExecuteScalar();
                    updated = v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
                }
                if (updated == 0)
                {
                    using var ins = new NpgsqlCommand(@"INSERT INTO reporting_settings (default_basis, allow_comparison, cash_recognition_threshold_days, updated_by, updated_at)
                        VALUES (@b, @c, @t, 1, NOW())", conn, tx);
                    ins.Parameters.AddWithValue("@b", basis);
                    ins.Parameters.AddWithValue("@c", (bool)(ChkComparison.IsChecked == true));
                    ins.Parameters.AddWithValue("@t", threshold);
                    ins.ExecuteNonQuery();
                }
                tx.Commit();
                AuditService.Log("UPDATE", "ReportingSettings", 1, "basis=" + basis + ", comparison=" + ChkComparison.IsChecked + ", threshold=" + threshold);
                LblStatus.Text = "تم الحفظ";
            }
            catch (Exception ex) { LblStatus.Text = "خطأ الحفظ: " + ex.Message; }
        }
    }
}
