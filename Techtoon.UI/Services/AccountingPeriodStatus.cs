using System;
using System.Collections.Generic;
using Npgsql;

namespace Techtoon.UI.Services
{
    /// <summary>
    /// A4: قيم حالة الفترة المحاسبية (<c>accounting_periods.status</c>).
    ///
    /// العمود <c>status</c> قد يكون VARCHAR أو نوع ENUM في PostgreSQL (وقيمه تختلف بين النسخ:
    /// open/closed أو مغلق/مفتوح). لذلك تُقرأ القيم الفعلية من كتالوج قاعدة البيانات عند أول استخدام
    /// ولا تُفترض — وهذا يضمن أن يمرّر INSERT قيمة يقبلها العمود فعلًا.
    ///
    /// الدوال المُصدَّرة:
    ///   OpenValue  : القيمة التي يجب استخدامها لحالة "مفتوحة" عند الإدراج/التعديل.
    ///   ClosedValue: القيمة التي يجب استخدامها لحالة "مغلقة" عند الإدراج/التعديل.
    ///   IsClosed   : هل قيمة الحالة (من أي إصدار/لغة) تُعتبر "مغلقة"؟
    /// </summary>
    public static class AccountingPeriodStatus
    {
        public const string OpenFallback = "open";
        public const string ClosedFallback = "closed";

        private static readonly object _lock = new object();
        private static bool _resolved;
        private static string _open = OpenFallback;
        private static string _closed = ClosedFallback;

        /// <summary>كل الصيغ المعروفة للحالة المغلقة (تُقرأ من الذاكرة فقط — بلا استعلام قاعدة بيانات).</summary>
        private static HashSet<string> _closedLiterals = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "closed", "close", "locked", "lock",
            "مغلق", "مقفل", "مقفلة"
        };

        /// <summary>القيمة المستخدمة للحالة المفتوحة.</summary>
        public static string OpenValue
        {
            get { EnsureResolved(); return _open; }
        }

        /// <summary>القيمة المستخدمة للحالة المغلقة.</summary>
        public static string ClosedValue
        {
            get { EnsureResolved(); return _closed; }
        }

        /// <summary>يفرض إعادة قراءة القيم من قاعدة البيانات (يُستخدم بعد أي تغيّر في القاعدة).</summary>
        public static void Reset()
        {
            lock (_lock) { _resolved = false; }
        }

        private static void EnsureResolved()
        {
            lock (_lock)
            {
                if (_resolved) return;
                _resolved = true;
                try { Resolve(); }
                catch { _open = OpenFallback; _closed = ClosedFallback; }
            }
        }

        private static void Resolve()
        {
            using var conn = new NpgsqlConnection(SecureConnectionManager.GetConnectionString());
            conn.Open();

            string udt = null;
            using (var cmd = new NpgsqlCommand(
                "SELECT udt_name FROM information_schema.columns WHERE table_name = 'accounting_periods' " +
                "AND column_name = 'status' ORDER BY (table_schema = 'public') DESC LIMIT 1", conn))
            using (var r = cmd.ExecuteReader())
            {
                if (r.Read()) udt = r.IsDBNull(0) ? null : r.GetString(0);
            }

            // عمود نصي عادي → القيم open/closed كافية.
            if (string.IsNullOrWhiteSpace(udt)) return;
            if (udt.Equals("varchar", StringComparison.OrdinalIgnoreCase)
                || udt.Equals("text", StringComparison.OrdinalIgnoreCase)
                || udt.Equals("bpchar", StringComparison.OrdinalIgnoreCase)
                || udt.Equals("character", StringComparison.OrdinalIgnoreCase)
                || udt.Equals("character varying", StringComparison.OrdinalIgnoreCase))
                return;

            // عمود ENUM → نقرأ التسميات المتاحة ونختار ما يقابل "مفتوح/مغلق".
            var labels = new List<string>();
            using (var cmd = new NpgsqlCommand(
                "SELECT e.enumlabel FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid " +
                "WHERE t.typname = @t ORDER BY e.enumsortorder", conn))
            {
                cmd.Parameters.AddWithValue("t", udt);
                using var r = cmd.ExecuteReader();
                while (r.Read()) labels.Add(r.GetString(0));
            }

            _open = Pick(labels, new[] { "open", "مفتوح", "OPEN" }, OpenFallback);
            _closed = Pick(labels, new[] { "closed", "مغلق", "مقفل", "CLOSED", "LOCKED" }, ClosedFallback);
            var updated = new HashSet<string>(_closedLiterals, StringComparer.OrdinalIgnoreCase) { _closed };
            _closedLiterals = updated;
        }

        private static string Pick(List<string> labels, string[] preferred, string fallback)
        {
            foreach (var p in preferred)
                foreach (var l in labels)
                    if (string.Equals(l.Trim(), p, StringComparison.OrdinalIgnoreCase)) return l;
            return labels.Count > 0 ? labels[0] : fallback;
        }

        /// <summary>
        /// هل قيمة الحالة تُعتبر "مغلقة"؟ (يتعامل مع open/closed و مغلق/مقفل و LOCKED).
        /// لا تُجري استعلام قاعدة بيانات — سريعة وآمنة داخل حلقات القراءة.
        /// </summary>
        public static bool IsClosed(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;
            string s = status.Trim();
            if (_closedLiterals.Contains(s)) return true;
            if (string.Equals(s, _closed, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>نص عربي للعرض فقط.</summary>
        public static string Display(string status) => IsClosed(status) ? "مغلقة" : "مفتوحة";
    }
}
