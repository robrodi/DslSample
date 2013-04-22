using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeskyScript
{
    internal static class EnumParsing
    {
        [Pure]
        internal static TEnum? TryParse<TEnum>(string word) where TEnum : struct
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(word));
            TEnum value;
            var success = Enum.TryParse(word, true, out value);
            return success ? value : (TEnum?)null;
        }
        [Pure]
        internal static TEnum Parse<TEnum>(string word) where TEnum : struct
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(word));
            TEnum value;
            Contract.Assert(Enum.TryParse(word, true, out value), string.Format("{0} missing.  Found <{1}>", typeof(TEnum).Name, word));
            return value;
        }
    }
}
