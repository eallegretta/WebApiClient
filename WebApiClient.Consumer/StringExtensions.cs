using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebApiClient.Consumer
{
    internal static class StringExtensions
    {
        internal static bool In(this string source, params string[] set)
        {
            return In(source, StringComparison.Ordinal, set);
        }

        internal static bool In(this string source, StringComparison comparison, params string[] set)
        {
            return set.Any(target => source.Equals(target, comparison));
        }
    }
}
