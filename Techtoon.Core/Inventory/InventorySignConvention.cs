namespace Techtoon.Core.Inventory
{
    /// <summary>
    /// C5 — single source of truth for interpreting inventory_movements.quantity.
    ///
    /// Historically the WPF app wrote 'in'/'out' rows with positive magnitudes while the
    /// API wrote sales 'out' rows with negative quantities. Existing production rows may
    /// therefore follow either convention. To avoid a data migration (never executed
    /// without owner approval) every reader must interpret movement types the same way:
    ///
    ///   in / opening  -> adds stock   (ABS quantity, tolerates legacy negatives)
    ///   out           -> removes stock (ABS quantity, tolerates legacy negatives)
    ///   adjustment    -> signed delta (+/-) by design
    ///
    /// Writers must store positive magnitudes for in/out and signed deltas for adjustment.
    /// </summary>
    public static class InventorySignConvention
    {
        /// <summary>
        /// SQL expression yielding the signed stock effect of a movement row.
        /// </summary>
        /// <param name="alias">Optional table alias, e.g. "im" or "i". Pass null/empty when unqualified.</param>
        public static string SignedQuantitySql(string? alias = "im")
        {
            var p = string.IsNullOrEmpty(alias) ? string.Empty : alias + ".";
            return "CASE " +
                   $"WHEN {p}type IN ('in','opening','transfer_in') THEN ABS({p}quantity) " +
                   $"WHEN {p}type IN ('out','transfer_out') THEN -ABS({p}quantity) " +
                   $"WHEN {p}type = 'adjustment' THEN {p}quantity " +
                   "ELSE 0 END";
        }

        /// <summary>Signed effect for a single known movement type and magnitude (used by writers/validators).</summary>
        public static decimal SignedQuantity(string type, decimal quantity)
        {
            switch ((type ?? string.Empty).ToLowerInvariant())
            {
                case "in":
                case "opening":
                case "transfer_in":
                    return System.Math.Abs(quantity);
                case "out":
                case "transfer_out":
                    return -System.Math.Abs(quantity);
                case "adjustment":
                    return quantity;
                default:
                    return 0m;
            }
        }
    }
}
