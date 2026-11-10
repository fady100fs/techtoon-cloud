using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Npgsql;
using NpgsqlTypes;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    /// <summary>سياسة عامة متاحة للتخصيص (خصم مبكر أو رسوم تأخير).</summary>
    public class PolicyOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Kind { get; set; } = "";
        public decimal DefaultValue { get; set; }
        public int DefaultDays { get; set; }
        public bool IsActive { get; set; }
        public string Display { get; set; } = "";
        public string DefaultValueDisplay => DefaultValue.ToString("N2");
    }

    /// <summary>نوع استثناء رسوم التأخير.</summary>
    public class ExceptionTypeOption
    {
        public string Value { get; set; } = "";
        public string Display { get; set; } = "";
    }

    /// <summary>سياسة خصم مُسندة إلى عميل.</summary>
    public class PartyDiscountPolicyRow
    {
        public int Id { get; set; }
        public int PolicyId { get; set; }
        public string PolicyName { get; set; } = "";
        public string PolicyType { get; set; } = "";
        public decimal PolicyDefaultValue { get; set; }
        public int PolicyEligibilityDays { get; set; }
        public decimal? CustomDiscountValue { get; set; }
        public int? CustomEligibilityDays { get; set; }
        public string Notes { get; set; } = "";
        public DateTime AssignedAt { get; set; }
        public bool IsActive { get; set; }

        public string PolicyNameDisplay => string.IsNullOrWhiteSpace(PolicyName) ? ("سياسة #" + PolicyId) : PolicyName;
        public string PolicyTypeDisplay => PolicyType == "percentage" ? "نسبة مئوية" : (PolicyType == "fixed" ? "مبلغ ثابت" : PolicyType);
        public string PolicyDefaultValueDisplay => PolicyDefaultValue.ToString("N2");
        public string PolicyEligibilityDaysDisplay => PolicyEligibilityDays.ToString();
        public string CustomDiscountValueDisplay => CustomDiscountValue.HasValue ? CustomDiscountValue.Value.ToString("N2") : "—";
        public string CustomEligibilityDaysDisplay => CustomEligibilityDays.HasValue ? CustomEligibilityDays.Value.ToString() : "—";
        public string AssignedAtDisplay => AssignedAt == default ? "" : AssignedAt.ToString("dd/MM/yyyy HH:mm");
        public string StatusDisplay => IsActive ? "سارية" : "معطّلة";
        public string NotesDisplay => string.IsNullOrWhiteSpace(Notes) ? "—" : Notes;
    }

    /// <summary>استثناء رسوم تأخير مُسند إلى عميل.</summary>
    public class PartyLateFeeExceptionRow
    {
        public int Id { get; set; }
        public int PolicyId { get; set; }
        public string PolicyName { get; set; } = "";
        public string ExceptionType { get; set; } = "";
        public decimal? CustomFeeValue { get; set; }
        public int? CustomGraceDays { get; set; }
        public string Reason { get; set; } = "";
        public DateTime SetAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }

        public string PolicyNameDisplay => string.IsNullOrWhiteSpace(PolicyName) ? ("سياسة #" + PolicyId) : PolicyName;
        public string ExceptionTypeDisplay => ExceptionType switch
        {
            "exempt" => "إعفاء كامل",
            "custom_rate" => "قيمة رسوم مخصصة",
            "custom_grace" => "فترة سماح مخصصة",
            _ => ExceptionType
        };
        public string CustomFeeValueDisplay => CustomFeeValue.HasValue ? CustomFeeValue.Value.ToString("N2") : "—";
        public string CustomGraceDaysDisplay => CustomGraceDays.HasValue ? CustomGraceDays.Value.ToString() : "—";
        public string SetAtDisplay => SetAt == default ? "" : SetAt.ToString("dd/MM/yyyy HH:mm");
        public string ExpiresAtDisplay => ExpiresAt.HasValue ? ExpiresAt.Value.ToString("dd/MM/yyyy") : "—";
        public string StatusDisplay => IsActive ? "ساري" : "معطّل";
        public string ReasonDisplay => string.IsNullOrWhiteSpace(Reason) ? "—" : Reason;
    }

    /// <summary>
    /// GAP B6 — سياسات العميل: ربط العميل بسياسة خصم مبكر وباستثناء رسوم تأخير.
    /// كل العمليات تتم على party_discount_policies و party_late_fee_exceptions مباشرة.
    /// </summary>
    public class PartyPolicyViewModel : ViewModelBase
    {
        private int _partyId;
        public int PartyId
        {
            get => _partyId;
            set { if (SetProperty(ref _partyId, value)) OnPropertyChanged(nameof(IsPartyLoaded)); }
        }

        public bool IsPartyLoaded => PartyId > 0;

        public ObservableCollection<PartyDiscountPolicyRow> DiscountPolicies { get; } = new();
        public ObservableCollection<PartyLateFeeExceptionRow> LateFeeExceptions { get; } = new();
        public ObservableCollection<PolicyOption> AvailableDiscountPolicies { get; } = new();
        public ObservableCollection<PolicyOption> AvailableLateFeePolicies { get; } = new();
        public ObservableCollection<ExceptionTypeOption> ExceptionTypes { get; } = new();

        // ===== نموذج سياسة الخصم =====
        private int? _selectedDiscountPolicyId;
        public int? SelectedDiscountPolicyId { get => _selectedDiscountPolicyId; set => SetProperty(ref _selectedDiscountPolicyId, value); }

        private string _customDiscountValueText = "";
        public string CustomDiscountValueText { get => _customDiscountValueText; set => SetProperty(ref _customDiscountValueText, value); }

        private string _customEligibilityDaysText = "";
        public string CustomEligibilityDaysText { get => _customEligibilityDaysText; set => SetProperty(ref _customEligibilityDaysText, value); }

        private string _discountNotes = "";
        public string DiscountNotes { get => _discountNotes; set => SetProperty(ref _discountNotes, value); }

        // ===== نموذج استثناء رسوم التأخير =====
        private int? _selectedExceptionPolicyId;
        public int? SelectedExceptionPolicyId { get => _selectedExceptionPolicyId; set => SetProperty(ref _selectedExceptionPolicyId, value); }

        private string _selectedExceptionType = "exempt";
        public string SelectedExceptionType { get => _selectedExceptionType; set => SetProperty(ref _selectedExceptionType, value); }

        private string _customFeeValueText = "";
        public string CustomFeeValueText { get => _customFeeValueText; set => SetProperty(ref _customFeeValueText, value); }

        private string _customGraceDaysText = "";
        public string CustomGraceDaysText { get => _customGraceDaysText; set => SetProperty(ref _customGraceDaysText, value); }

        private string _exceptionReason = "";
        public string ExceptionReason { get => _exceptionReason; set => SetProperty(ref _exceptionReason, value); }

        private DateTime? _expiresAt;
        public DateTime? ExpiresAt { get => _expiresAt; set => SetProperty(ref _expiresAt, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AssignDiscountCommand { get; }
        public ICommand AssignExceptionCommand { get; }
        public ICommand RemoveDiscountCommand { get; }
        public ICommand RemoveExceptionCommand { get; }

        public PartyPolicyViewModel()
        {
            ExceptionTypes.Add(new ExceptionTypeOption { Value = "exempt", Display = "إعفاء كامل من رسوم التأخير" });
            ExceptionTypes.Add(new ExceptionTypeOption { Value = "custom_rate", Display = "قيمة رسوم مخصصة" });
            ExceptionTypes.Add(new ExceptionTypeOption { Value = "custom_grace", Display = "فترة سماح مخصصة" });

            RefreshCommand = new UiRelayCommand(_ => Load());
            AssignDiscountCommand = new UiRelayCommand(_ => AssignDiscount());
            AssignExceptionCommand = new UiRelayCommand(_ => AssignException());
            RemoveDiscountCommand = new UiRelayCommand(p => RemoveDiscount(p as PartyDiscountPolicyRow));
            RemoveExceptionCommand = new UiRelayCommand(p => RemoveException(p as PartyLateFeeExceptionRow));
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        private static decimal ToDecimal(object? value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            if (value is decimal d) return d;
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        /// <summary>تحميل السياسات العامة + سياسات العميل الحالية.</summary>
        public void Load()
        {
            LoadAvailablePolicies();
            if (!IsPartyLoaded)
            {
                DiscountPolicies.Clear();
                LateFeeExceptions.Clear();
                StatusMessage = "احفظ بيانات العميل أولاً حتى يمكن تخصيص السياسات له.";
                return;
            }
            LoadPartyPolicies();
        }

        private void LoadAvailablePolicies()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();

                AvailableDiscountPolicies.Clear();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, COALESCE(name, ''), COALESCE(discount_type, ''), COALESCE(discount_value, 0), " +
                    "COALESCE(eligibility_days, 0), COALESCE(is_active, TRUE) FROM early_payment_discount_policies ORDER BY name", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var name = r.IsDBNull(1) ? "" : r.GetString(1);
                        var active = r.IsDBNull(5) ? true : r.GetBoolean(5);
                        AvailableDiscountPolicies.Add(new PolicyOption
                        {
                            Id = r.GetInt32(0),
                            Name = name,
                            Kind = r.IsDBNull(2) ? "" : r.GetString(2),
                            DefaultValue = ToDecimal(r.GetValue(3)),
                            DefaultDays = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4)),
                            IsActive = active,
                            Display = string.IsNullOrWhiteSpace(name) ? ("سياسة #" + r.GetInt32(0)) : (active ? name : name + " (معطّلة)")
                        });
                    }
                }

                AvailableLateFeePolicies.Clear();
                using (var cmd = new NpgsqlCommand(
                    "SELECT id, COALESCE(name, ''), COALESCE(fee_type, ''), COALESCE(fee_value, 0), " +
                    "COALESCE(grace_period_days, 0), COALESCE(is_active, TRUE) FROM late_fee_policies ORDER BY name", conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var name = r.IsDBNull(1) ? "" : r.GetString(1);
                        var active = r.IsDBNull(5) ? true : r.GetBoolean(5);
                        AvailableLateFeePolicies.Add(new PolicyOption
                        {
                            Id = r.GetInt32(0),
                            Name = name,
                            Kind = r.IsDBNull(2) ? "" : r.GetString(2),
                            DefaultValue = ToDecimal(r.GetValue(3)),
                            DefaultDays = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4)),
                            IsActive = active,
                            Display = string.IsNullOrWhiteSpace(name) ? ("سياسة #" + r.GetInt32(0)) : (active ? name : name + " (معطّلة)")
                        });
                    }
                }
            }
            catch (Exception ex) { StatusMessage = "خطأ في تحميل السياسات العامة: " + ex.Message; }
        }

        private void LoadPartyPolicies()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();

                DiscountPolicies.Clear();
                using (var cmd = new NpgsqlCommand(
                    "SELECT pdp.id, pdp.policy_id, COALESCE(ep.name, ''), COALESCE(ep.discount_type, ''), " +
                    "COALESCE(ep.discount_value, 0), COALESCE(ep.eligibility_days, 0), " +
                    "pdp.custom_discount_value, pdp.custom_eligibility_days, COALESCE(pdp.notes, ''), " +
                    "pdp.assigned_at, pdp.is_active " +
                    "FROM party_discount_policies pdp " +
                    "LEFT JOIN early_payment_discount_policies ep ON ep.id = pdp.policy_id " +
                    "WHERE pdp.party_id = @pid ORDER BY pdp.is_active DESC, pdp.id", conn))
                {
                    cmd.Parameters.AddWithValue("@pid", PartyId);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        DiscountPolicies.Add(new PartyDiscountPolicyRow
                        {
                            Id = r.GetInt32(0),
                            PolicyId = r.GetInt32(1),
                            PolicyName = r.IsDBNull(2) ? "" : r.GetString(2),
                            PolicyType = r.IsDBNull(3) ? "" : r.GetString(3),
                            PolicyDefaultValue = ToDecimal(r.GetValue(4)),
                            PolicyEligibilityDays = r.IsDBNull(5) ? 0 : Convert.ToInt32(r.GetValue(5)),
                            CustomDiscountValue = r.IsDBNull(6) ? (decimal?)null : ToDecimal(r.GetValue(6)),
                            CustomEligibilityDays = r.IsDBNull(7) ? (int?)null : Convert.ToInt32(r.GetValue(7)),
                            Notes = r.IsDBNull(8) ? "" : r.GetString(8),
                            AssignedAt = DbValue.AsDateTime(r.GetValue(9)),
                            IsActive = r.IsDBNull(10) ? true : r.GetBoolean(10)
                        });
                    }
                }

                LateFeeExceptions.Clear();
                using (var cmd = new NpgsqlCommand(
                    "SELECT plfe.id, plfe.policy_id, COALESCE(lfp.name, ''), plfe.exception_type, " +
                    "plfe.custom_fee_value, plfe.custom_grace_days, COALESCE(plfe.reason, ''), " +
                    "plfe.set_at, plfe.expires_at, plfe.is_active " +
                    "FROM party_late_fee_exceptions plfe " +
                    "LEFT JOIN late_fee_policies lfp ON lfp.id = plfe.policy_id " +
                    "WHERE plfe.party_id = @pid ORDER BY plfe.is_active DESC, plfe.id", conn))
                {
                    cmd.Parameters.AddWithValue("@pid", PartyId);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        LateFeeExceptions.Add(new PartyLateFeeExceptionRow
                        {
                            Id = r.GetInt32(0),
                            PolicyId = r.GetInt32(1),
                            PolicyName = r.IsDBNull(2) ? "" : r.GetString(2),
                            ExceptionType = r.IsDBNull(3) ? "" : r.GetString(3),
                            CustomFeeValue = r.IsDBNull(4) ? (decimal?)null : ToDecimal(r.GetValue(4)),
                            CustomGraceDays = r.IsDBNull(5) ? (int?)null : Convert.ToInt32(r.GetValue(5)),
                            Reason = r.IsDBNull(6) ? "" : r.GetString(6),
                            SetAt = DbValue.AsDateTime(r.GetValue(7)),
                            ExpiresAt = DbValue.AsNullableDateTime(r.GetValue(8)),
                            IsActive = r.IsDBNull(9) ? true : r.GetBoolean(9)
                        });
                    }
                }

                StatusMessage = "تم تحميل سياسات العميل — " + DiscountPolicies.Count + " سياسة خصم و " + LateFeeExceptions.Count + " استثناء رسوم تأخير.";
            }
            catch (Exception ex) { StatusMessage = "خطأ في تحميل سياسات العميل: " + ex.Message; }
        }

        /// <summary>معرّف المستخدم الذي ينفّذ العملية (عمود NOT NULL مع FK إلى users).</summary>
        private static int ResolveUserId(NpgsqlConnection conn)
        {
            var current = AuthService.CurrentUser?.Id ?? 0;
            if (current > 0) return current;
            try
            {
                using var cmd = new NpgsqlCommand("SELECT id FROM users ORDER BY id LIMIT 1", conn);
                var v = cmd.ExecuteScalar();
                if (v != null && v != DBNull.Value) return Convert.ToInt32(v);
            }
            catch { }
            throw new InvalidOperationException("تعذّر تحديد المستخدم المنفّذ للعملية (لا يوجد مستخدم مسجّل).");
        }

        /// <summary>بارامتر بنوع صريح (يضمن نوع البيانات عند الإرسال إلى PostgreSQL).</summary>
        private static NpgsqlParameter P(string name, NpgsqlDbType type, object? value)
        {
            return new NpgsqlParameter(name, type) { Value = value ?? DBNull.Value };
        }

        private bool TryParseOptionalDecimal(string? text, out decimal? value, out string error)
        {
            value = null;
            error = "";
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (!decimal.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out var parsed))
            {
                error = "القيمة المدخلة غير صحيحة — يجب أن تكون رقمًا.";
                return false;
            }
            if (parsed < 0m)
            {
                error = "لا يمكن إدخال قيمة سالبة.";
                return false;
            }
            value = parsed;
            return true;
        }

        private bool TryParseOptionalInt(string? text, out int? value, out string error)
        {
            value = null;
            error = "";
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed))
            {
                error = "القيمة المدخلة غير صحيحة — يجب أن تكون عددًا صحيحًا.";
                return false;
            }
            if (parsed < 0)
            {
                error = "لا يمكن إدخال قيمة سالبة.";
                return false;
            }
            value = parsed;
            return true;
        }

        /// <summary>تعيين/تحديث سياسة الخصم للعميل.</summary>
        public void AssignDiscount()
        {
            StatusMessage = "";
            if (!IsPartyLoaded) { StatusMessage = "لا يمكن تعيين سياسات قبل حفظ بيانات العميل."; return; }
            if (!SelectedDiscountPolicyId.HasValue || SelectedDiscountPolicyId.Value <= 0) { StatusMessage = "اختر سياسة الخصم المطلوبة."; return; }

            if (!TryParseOptionalDecimal(CustomDiscountValueText, out var customValue, out var err))
            { StatusMessage = "قيمة الخصم المخصصة: " + err; return; }
            if (!TryParseOptionalInt(CustomEligibilityDaysText, out var customDays, out err))
            { StatusMessage = "مدة الأهلية المخصصة: " + err; return; }

            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                int userId = ResolveUserId(conn);
                using var tx = conn.BeginTransaction();

                int? existingId = null;
                using (var chk = new NpgsqlCommand(
                    "SELECT id FROM party_discount_policies WHERE party_id = @pid AND policy_id = @pol LIMIT 1", conn, tx))
                {
                    chk.Parameters.AddWithValue("@pid", PartyId);
                    chk.Parameters.AddWithValue("@pol", SelectedDiscountPolicyId);
                    var v = chk.ExecuteScalar();
                    if (v != null && v != DBNull.Value) existingId = Convert.ToInt32(v);
                }

                int rowId;
                if (existingId.HasValue)
                {
                    rowId = existingId.Value;
                    using (var upd = new NpgsqlCommand(
                        "UPDATE party_discount_policies SET custom_discount_value = @cdv, custom_eligibility_days = @ced, " +
                        "is_active = TRUE, assigned_by = @by, assigned_at = NOW(), notes = @notes WHERE id = @id AND party_id = @pid", conn, tx))
                    {
                        upd.Parameters.Add(P("@cdv", NpgsqlDbType.Numeric, customValue));
                        upd.Parameters.Add(P("@ced", NpgsqlDbType.Integer, customDays));
                        upd.Parameters.Add(P("@by", NpgsqlDbType.Integer, userId));
                        upd.Parameters.Add(P("@notes", NpgsqlDbType.Text, string.IsNullOrWhiteSpace(DiscountNotes) ? null : DiscountNotes.Trim()));
                        upd.Parameters.Add(P("@id", NpgsqlDbType.Integer, rowId));
                        upd.Parameters.Add(P("@pid", NpgsqlDbType.Integer, PartyId));
                        upd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var ins = new NpgsqlCommand(
                        "INSERT INTO party_discount_policies (party_id, policy_id, custom_discount_value, custom_eligibility_days, " +
                        "is_active, assigned_by, assigned_at, notes) " +
                        "VALUES (@pid, @pol, @cdv, @ced, TRUE, @by, NOW(), @notes) RETURNING id", conn, tx))
                    {
                        ins.Parameters.Add(P("@pid", NpgsqlDbType.Integer, PartyId));
                        ins.Parameters.Add(P("@pol", NpgsqlDbType.Integer, SelectedDiscountPolicyId));
                        ins.Parameters.Add(P("@cdv", NpgsqlDbType.Numeric, customValue));
                        ins.Parameters.Add(P("@ced", NpgsqlDbType.Integer, customDays));
                        ins.Parameters.Add(P("@by", NpgsqlDbType.Integer, userId));
                        ins.Parameters.Add(P("@notes", NpgsqlDbType.Text, string.IsNullOrWhiteSpace(DiscountNotes) ? null : DiscountNotes.Trim()));
                        var v = ins.ExecuteScalar();
                        rowId = v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
                    }
                }

                tx.Commit();
                AuditService.Log(existingId.HasValue ? "UPDATE" : "CREATE", "PartyDiscountPolicy", rowId,
                    "Party #" + PartyId + " policy #" + SelectedDiscountPolicyId +
                    " customValue=" + (customValue.HasValue ? customValue.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                    " customDays=" + (customDays.HasValue ? customDays.Value.ToString(CultureInfo.InvariantCulture) : "-"));
                StatusMessage = "تم حفظ سياسة الخصم للعميل.";
                LoadPartyPolicies();
            }
            catch (Exception ex) { StatusMessage = "خطأ في حفظ سياسة الخصم: " + ex.Message; }
        }

        /// <summary>تعيين/تحديث استثناء رسوم التأخير للعميل.</summary>
        public void AssignException()
        {
            StatusMessage = "";
            if (!IsPartyLoaded) { StatusMessage = "لا يمكن تعيين استثناءات قبل حفظ بيانات العميل."; return; }
            if (!SelectedExceptionPolicyId.HasValue || SelectedExceptionPolicyId.Value <= 0) { StatusMessage = "اختر سياسة رسوم التأخير المطلوبة."; return; }
            if (string.IsNullOrWhiteSpace(SelectedExceptionType)) { StatusMessage = "اختر نوع الاستثناء."; return; }
            if (SelectedExceptionType != "exempt" && SelectedExceptionType != "custom_rate" && SelectedExceptionType != "custom_grace")
            { StatusMessage = "نوع الاستثناء غير صالح."; return; }

            if (!TryParseOptionalDecimal(CustomFeeValueText, out var customFee, out var err))
            { StatusMessage = "قيمة الرسوم المخصصة: " + err; return; }
            if (!TryParseOptionalInt(CustomGraceDaysText, out var customGrace, out err))
            { StatusMessage = "فترة السماح المخصصة: " + err; return; }

            if (SelectedExceptionType == "custom_rate" && !customFee.HasValue)
            { StatusMessage = "يجب إدخال قيمة الرسوم المخصصة لنوع الاستثناء (قيمة رسوم مخصصة)."; return; }
            if (SelectedExceptionType == "custom_grace" && !customGrace.HasValue)
            { StatusMessage = "يجب إدخال فترة السماح المخصصة لنوع الاستثناء (فترة سماح مخصصة)."; return; }

            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                int userId = ResolveUserId(conn);
                using var tx = conn.BeginTransaction();

                int? existingId = null;
                using (var chk = new NpgsqlCommand(
                    "SELECT id FROM party_late_fee_exceptions WHERE party_id = @pid AND policy_id = @pol LIMIT 1", conn, tx))
                {
                    chk.Parameters.AddWithValue("@pid", PartyId);
                    chk.Parameters.AddWithValue("@pol", SelectedExceptionPolicyId);
                    var v = chk.ExecuteScalar();
                    if (v != null && v != DBNull.Value) existingId = Convert.ToInt32(v);
                }

                int rowId;
                if (existingId.HasValue)
                {
                    rowId = existingId.Value;
                    using (var upd = new NpgsqlCommand(
                        "UPDATE party_late_fee_exceptions SET exception_type = @etype, custom_fee_value = @cfv, " +
                        "custom_grace_days = @cgd, is_active = TRUE, reason = @reason, set_by = @by, set_at = NOW(), " +
                        "expires_at = CAST(@exp AS date) WHERE id = @id AND party_id = @pid", conn, tx))
                    {
                        upd.Parameters.Add(P("@etype", NpgsqlDbType.Text, SelectedExceptionType));
                        upd.Parameters.Add(P("@cfv", NpgsqlDbType.Numeric, customFee));
                        upd.Parameters.Add(P("@cgd", NpgsqlDbType.Integer, customGrace));
                        upd.Parameters.Add(P("@reason", NpgsqlDbType.Text, string.IsNullOrWhiteSpace(ExceptionReason) ? null : ExceptionReason.Trim()));
                        upd.Parameters.Add(P("@by", NpgsqlDbType.Integer, userId));
                        upd.Parameters.Add(P("@exp", NpgsqlDbType.Date, ExpiresAt.HasValue ? ExpiresAt.Value.Date : (object?)null));
                        upd.Parameters.Add(P("@id", NpgsqlDbType.Integer, rowId));
                        upd.Parameters.Add(P("@pid", NpgsqlDbType.Integer, PartyId));
                        upd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var ins = new NpgsqlCommand(
                        "INSERT INTO party_late_fee_exceptions (party_id, policy_id, exception_type, custom_fee_value, " +
                        "custom_grace_days, is_active, reason, set_by, set_at, expires_at) " +
                        "VALUES (@pid, @pol, @etype, @cfv, @cgd, TRUE, @reason, @by, NOW(), CAST(@exp AS date)) RETURNING id", conn, tx))
                    {
                        ins.Parameters.Add(P("@pid", NpgsqlDbType.Integer, PartyId));
                        ins.Parameters.Add(P("@pol", NpgsqlDbType.Integer, SelectedExceptionPolicyId));
                        ins.Parameters.Add(P("@etype", NpgsqlDbType.Text, SelectedExceptionType));
                        ins.Parameters.Add(P("@cfv", NpgsqlDbType.Numeric, customFee));
                        ins.Parameters.Add(P("@cgd", NpgsqlDbType.Integer, customGrace));
                        ins.Parameters.Add(P("@reason", NpgsqlDbType.Text, string.IsNullOrWhiteSpace(ExceptionReason) ? null : ExceptionReason.Trim()));
                        ins.Parameters.Add(P("@by", NpgsqlDbType.Integer, userId));
                        ins.Parameters.Add(P("@exp", NpgsqlDbType.Date, ExpiresAt.HasValue ? ExpiresAt.Value.Date : (object?)null));
                        var v = ins.ExecuteScalar();
                        rowId = v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
                    }
                }

                tx.Commit();
                AuditService.Log(existingId.HasValue ? "UPDATE" : "CREATE", "PartyLateFeeException", rowId,
                    "Party #" + PartyId + " policy #" + SelectedExceptionPolicyId + " type=" + SelectedExceptionType +
                    " customFee=" + (customFee.HasValue ? customFee.Value.ToString(CultureInfo.InvariantCulture) : "-") +
                    " customGrace=" + (customGrace.HasValue ? customGrace.Value.ToString(CultureInfo.InvariantCulture) : "-"));
                StatusMessage = "تم حفظ استثناء رسوم التأخير للعميل.";
                LoadPartyPolicies();
            }
            catch (Exception ex) { StatusMessage = "خطأ في حفظ استثناء رسوم التأخير: " + ex.Message; }
        }

        /// <summary>إلغاء تفعيل سياسة الخصم (is_active = FALSE) — السجل يبقى للمراجعة.</summary>
        public void RemoveDiscount(PartyDiscountPolicyRow? row)
        {
            if (row == null) return;
            StatusMessage = "";
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "UPDATE party_discount_policies SET is_active = FALSE WHERE id = @id AND party_id = @pid", conn))
                {
                    cmd.Parameters.AddWithValue("@id", row.Id);
                    cmd.Parameters.AddWithValue("@pid", PartyId);
                    cmd.ExecuteNonQuery();
                }
                AuditService.Log("UPDATE", "PartyDiscountPolicy", row.Id,
                    "Deactivated discount policy #" + row.PolicyId + " for party #" + PartyId);
                StatusMessage = "تم إلغاء تفعيل سياسة الخصم.";
                LoadPartyPolicies();
            }
            catch (Exception ex) { StatusMessage = "خطأ في إلغاء سياسة الخصم: " + ex.Message; }
        }

        /// <summary>إلغاء تفعيل استثناء رسوم التأخير (is_active = FALSE).</summary>
        public void RemoveException(PartyLateFeeExceptionRow? row)
        {
            if (row == null) return;
            StatusMessage = "";
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "UPDATE party_late_fee_exceptions SET is_active = FALSE WHERE id = @id AND party_id = @pid", conn))
                {
                    cmd.Parameters.AddWithValue("@id", row.Id);
                    cmd.Parameters.AddWithValue("@pid", PartyId);
                    cmd.ExecuteNonQuery();
                }
                AuditService.Log("UPDATE", "PartyLateFeeException", row.Id,
                    "Deactivated late fee exception #" + row.PolicyId + " for party #" + PartyId);
                StatusMessage = "تم إلغاء تفعيل استثناء رسوم التأخير.";
                LoadPartyPolicies();
            }
            catch (Exception ex) { StatusMessage = "خطأ في إلغاء استثناء رسوم التأخير: " + ex.Message; }
        }
    }
}
