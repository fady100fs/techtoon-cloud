using System;

namespace Techtoon.UI.Services
{
    public static class MathHelper
    {
        public static double EvaluateExpression(string expression)
        {
            try
            {
                if (string.IsNullOrEmpty(expression)) return 0;
                expression = expression.Replace(",", ".").Trim();
                if (System.Text.RegularExpressions.Regex.IsMatch(expression, @"^[0-9\+\-\*\/\(\)\.\s]+$"))
                {
                    var dt = new System.Data.DataTable();
                    var result = dt.Compute(expression, "");
                    return Convert.ToDouble(result);
                }
                return double.TryParse(expression, out double val) ? val : 0;
            }
            catch { return 0; }
        }
    }
}