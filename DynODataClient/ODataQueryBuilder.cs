using System;
using System.Collections.Generic;
using System.Text;

namespace DynODataClient
{
    public static class ODataQueryBuilder
    {

        public static string BuildFilter(params string[] conditions)
        {
            var valid = conditions
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .ToList();

            return valid.Count == 0
                ? string.Empty
                : "&$filter=" + string.Join(" and ", valid);
        }

        // Handy helpers so you don’t mess up quoting

        public static string Eq(string field, string? value, bool useOdataString = true)
            => string.IsNullOrWhiteSpace(value) ? "" : $"{field} eq {(useOdataString == false ? value : ODataString(value))}";

        public static string Eq(string field, Guid? value)
            => value is null || value == Guid.Empty ? "" : $"{field} eq {value}";

        public static string Eq(string field, int? value)
            => value is null ? "" : $"{field} eq {value.Value}";

        private static string ODataString(string value)
            => $"'{value.Replace("'", "''")}'"; // escape single quotes for OData

    }

}
