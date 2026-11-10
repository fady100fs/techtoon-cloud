using System;
using System.Windows;
using System.Windows.Controls;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.Views;

public partial class RecurringJournalsEditWindow : Window
{
    private int _id = 0;

    public static string GetCS() => Techtoon.UI.Services.SecureConnectionManager.GetConnectionString();

    public RecurringJournalsEditWindow(int id = 0)
    {
        InitializeComponent();
        _id = id;
        try { LblTitle.Text = id > 0 ? "\u062A\u0639\u062F\u064A\u0644 \u0642\u0627\u0644\u0628 \u0642\u064A\u062F \u062F\u0648\u0631\u064A" : "\u0625\u0636\u0627\u0641\u0629 \u0642\u0627\u0644\u0628 \u0642\u064A\u062F \u062F\u0648\u0631\u064A"; } catch { }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        CmbFreq.Items.Clear();
        CmbFreq.Items.Add(new ComboBoxItem { Content = "\u064A\u0648\u0645\u064A", Tag = "daily" });
        CmbFreq.Items.Add(new ComboBoxItem { Content = "\u0623\u0633\u0628\u0648\u0639\u064A", Tag = "weekly" });
        CmbFreq.Items.Add(new ComboBoxItem { Content = "\u0634\u0647\u0631\u064A", Tag = "monthly" });
        CmbFreq.Items.Add(new ComboBoxItem { Content = "\u0631\u0628\u0639 \u0633\u0646\u0648\u064A", Tag = "quarterly" });
        CmbFreq.Items.Add(new ComboBoxItem { Content = "\u0646\u0635\u0641 \u0633\u0646\u0648\u064A", Tag = "semiannual" });
        CmbFreq.Items.Add(new ComboBoxItem { Content = "\u0633\u0646\u0648\u064A", Tag = "annual" });
        if (_id > 0) LoadData();
    }

    private void LoadData()
    {
        try
        {
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            using var cmd = new NpgsqlCommand(@"SELECT name, frequency, execution_day, start_date, end_date, COALESCE(description,'') FROM recurring_journal_templates WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                if (!r.IsDBNull(0)) TxtName.Text = r.GetValue(0).ToString();
                if (!r.IsDBNull(1))
                    for (int j = 0; j < CmbFreq.Items.Count; j++)
                        if (((ComboBoxItem)CmbFreq.Items[j]).Tag?.ToString() == r.GetString(1)) { CmbFreq.SelectedIndex = j; break; }
                if (!r.IsDBNull(2)) TxtDay.Text = r.GetValue(2).ToString();
                if (!r.IsDBNull(3)) DpSD.SelectedDate = Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(3));
                if (!r.IsDBNull(4)) DpED.SelectedDate = Techtoon.Core.Data.DbValue.AsDateTime(r.GetValue(4));
                if (!r.IsDBNull(5)) TxtDesc.Text = r.GetValue(5).ToString();
            }
        }
        catch (Exception ex) { LblStatus.Text = "\u062E\u0637\u0623: " + ex.Message; }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var p0 = TxtName.Text;
            var p1 = ((ComboBoxItem)CmbFreq.SelectedItem)?.Tag?.ToString() ?? "";
            var p2 = TxtDay.Text;
            var p3 = DpSD.SelectedDate ?? DateTime.Now;
            var p4 = DpED.SelectedDate ?? DateTime.Now;
            var p5 = TxtDesc.Text;
            using var conn = new NpgsqlConnection(GetCS());
            conn.Open();
            if (_id > 0)
            {
                using var cmd = new NpgsqlCommand(@"UPDATE recurring_journal_templates SET name=@p0, frequency=@p1, execution_day=@p2, start_date=@p3, end_date=@p4, description=@p5 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("id", _id);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            int _i2;
            if (!int.TryParse(p2, out _i2)) _i2 = 0;
            cmd.Parameters.AddWithValue("p2", _i2);
            cmd.Parameters.AddWithValue("p3", (object)p3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p5", (object)p5 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            else
            {
                using var cmd = new NpgsqlCommand(@"INSERT INTO recurring_journal_templates (name, frequency, execution_day, start_date, end_date, description, is_active, auto_post, next_run_date, created_by, created_at) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, TRUE, FALSE, @p3, 1, NOW())", conn);
            cmd.Parameters.AddWithValue("p0", (object)p0 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p1", (object)p1 ?? DBNull.Value);
            int _i2;
            if (!int.TryParse(p2, out _i2)) _i2 = 0;
            cmd.Parameters.AddWithValue("p2", _i2);
            cmd.Parameters.AddWithValue("p3", (object)p3 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p4", (object)p4 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p5", (object)p5 ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "RecurringJournal", _id > 0 ? _id : 0, "Saved from EditWindow");
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
            
                using (var cmdCheck = new NpgsqlCommand("SELECT id FROM recurring_journal_lines WHERE template_id = @id LIMIT 10", conn))
                {
                    cmdCheck.Parameters.AddWithValue("id", _id);
                    using (var rCheck = cmdCheck.ExecuteReader())
                        while (rCheck.Read())
                            items.Add(new DependencyItem { Type = "Related", Display = $"Record ID: {rCheck.GetInt32(0)}", TargetWindow = "RecurringJournalLinesEditWindow", TargetId = rCheck.GetInt32(0) });
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
            
            using var cmd = new NpgsqlCommand("DELETE FROM recurring_journal_templates WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", _id);
            cmd.ExecuteNonQuery();
            
            AuditService.Log(_id > 0 ? "UPDATE" : "CREATE", "RecurringJournal", _id > 0 ? _id : 0, "Saved from EditWindow");
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