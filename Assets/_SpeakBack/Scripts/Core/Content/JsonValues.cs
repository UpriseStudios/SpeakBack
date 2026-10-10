using System;
using System.Collections.Generic;

namespace Uprise.SpeakBack.Core.Content
{
    /// <summary>Typed accessors over <see cref="MiniJson"/> trees, with messages that name the missing or mistyped field.</summary>
    public static class JsonValues
    {
        /// <summary>Required string property.</summary>
        public static string String(IReadOnlyDictionary<string, object> obj, string key) =>
            Get(obj, key) as string ?? throw new FormatException($"'{key}' must be a string.");

        /// <summary>Required integer property (a JSON number with no fractional part).</summary>
        public static int Int(IReadOnlyDictionary<string, object> obj, string key)
        {
            if (!(Get(obj, key) is double d) || d != Math.Floor(d) || d < int.MinValue || d > int.MaxValue)
            {
                throw new FormatException($"'{key}' must be an integer.");
            }

            return (int)d;
        }

        /// <summary>Required object property.</summary>
        public static IReadOnlyDictionary<string, object> Object(IReadOnlyDictionary<string, object> obj, string key) =>
            Get(obj, key) as Dictionary<string, object> ?? throw new FormatException($"'{key}' must be an object.");

        /// <summary>Required array property.</summary>
        public static IReadOnlyList<object> Array(IReadOnlyDictionary<string, object> obj, string key) =>
            Get(obj, key) as List<object> ?? throw new FormatException($"'{key}' must be an array.");

        /// <summary>Required array of strings.</summary>
        public static IReadOnlyList<string> StringArray(IReadOnlyDictionary<string, object> obj, string key) =>
            ToStrings(Array(obj, key), key);

        /// <summary>Converts an array value to strings; throws if any element is not a string.</summary>
        public static IReadOnlyList<string> ToStrings(object value, string what)
        {
            if (!(value is List<object> list)) throw new FormatException($"'{what}' must be an array.");
            var result = new string[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                result[i] = list[i] as string ?? throw new FormatException($"'{what}'[{i}] must be a string.");
            }

            return result;
        }

        /// <summary>Whether <paramref name="key"/> is present (even if null).</summary>
        public static bool Has(IReadOnlyDictionary<string, object> obj, string key) => obj.ContainsKey(key);

        private static object Get(IReadOnlyDictionary<string, object> obj, string key)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (!obj.TryGetValue(key, out var value)) throw new FormatException($"Missing '{key}'.");
            return value;
        }
    }
}
