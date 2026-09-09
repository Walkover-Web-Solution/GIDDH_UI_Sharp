using System.Collections;
using System.Globalization;

namespace GiddhTemplate.Services
{
    /// <summary>
    /// Helpers imported into every generic template (`@using static`).
    /// All of them are total: a missing key or an unexpected payload type yields the
    /// neutral value ("" / 0 / false / empty list) instead of failing the render.
    /// </summary>
    public static class TemplateHelpers
    {
        /// <summary>True when the (dotted) path resolves to a non-null payload value.</summary>
        public static bool Has(object? source, string? path = null)
        {
            return !SafeConvert.IsMissing(Resolve(source, path));
        }

        /// <summary>
        /// Same as <see cref="Has"/> — kept so existing templates that declare a local
        /// <c>HasKey</c> keep compiling if that local helper is removed later.
        /// </summary>
        public static bool HasKey(object? source, string? propertyName = null)
        {
            return Has(source, propertyName);
        }

        /// <summary>Raw value at the (dotted) path, or the missing value when absent.</summary>
        public static dynamic Get(object? source, string? path = null)
        {
            return Resolve(source, path) ?? SafeMissing.Instance;
        }

        /// <summary>Text at the (dotted) path, never null.</summary>
        public static string Str(object? source, string? path = null)
        {
            return SafeConvert.ToText(Resolve(source, path));
        }

        /// <summary>Text at the (dotted) path, or <paramref name="fallback"/> when empty.</summary>
        public static string StrOr(object? source, string? path, string fallback)
        {
            var text = Str(source, path);
            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }

        /// <summary>Decimal at the (dotted) path. Accepts numbers, numeric strings and booleans.</summary>
        public static decimal Dec(object? source, string? path = null)
        {
            return SafeConvert.ToDecimal(Resolve(source, path));
        }

        /// <summary>Decimal at the (dotted) path rounded to <paramref name="decimals"/> places.</summary>
        public static decimal Dec(object? source, string? path, int decimals)
        {
            return Math.Round(Dec(source, path), decimals, MidpointRounding.AwayFromZero);
        }

        /// <summary>Integer at the (dotted) path.</summary>
        public static int Int(object? source, string? path = null)
        {
            return SafeConvert.ToInt(Resolve(source, path));
        }

        /// <summary>Boolean at the (dotted) path. Accepts true/false, "true"/"yes"/"1" and numbers.</summary>
        public static bool Bool(object? source, string? path = null)
        {
            return SafeConvert.ToBool(Resolve(source, path));
        }

        /// <summary>
        /// Enumerable at the (dotted) path, empty when absent or not a collection.
        /// Elements stay dynamic so `foreach (var item in Items(...))` keeps member access working.
        /// </summary>
        public static IEnumerable<dynamic> Items(object? source, string? path = null)
        {
            return SafeConvert.ToList(Resolve(source, path));
        }

        /// <summary>Number of elements at the (dotted) path.</summary>
        public static int CountOf(object? source, string? path = null)
        {
            var value = Resolve(source, path);

            if (value is SafeList safeList)
            {
                return safeList.Count;
            }

            if (value is ICollection collection)
            {
                return collection.Count;
            }

            return SafeConvert.ToList(value).Count();
        }

        /// <summary>True when <c>label.{key}</c> has non-empty text.</summary>
        public static bool HasLabel(object? source, string key)
        {
            return !string.IsNullOrWhiteSpace(Str(source, "label." + key));
        }

        /// <summary>
        /// Table column visibility. Uses the payload label when present; if the payload
        /// omitted every table column label, the standard invoice columns still show.
        /// </summary>
        public static bool ShowCol(object? source, string key)
        {
            return HasLabel(source, key) || !HasAnyTableLabel(source);
        }

        /// <summary>Column heading from <c>label.{key}</c>, or <paramref name="fallback"/>.</summary>
        public static string ColLbl(object? source, string key, string fallback)
        {
            return StrOr(source, "label." + key, fallback);
        }

        public static bool HasAnyTableLabel(object? source)
        {
            return HasLabel(source, "sNo")
                || HasLabel(source, "item")
                || HasLabel(source, "quantity")
                || HasLabel(source, "rate")
                || HasLabel(source, "total")
                || HasLabel(source, "hsnSac")
                || HasLabel(source, "date")
                || HasLabel(source, "discount")
                || HasLabel(source, "taxableValue")
                || HasLabel(source, "taxes");
        }

        /// <summary>True when the (dotted) path has no value, empty text or no elements.</summary>
        public static bool IsEmpty(object? source, string? path = null)
        {
            var value = Resolve(source, path);

            if (SafeConvert.IsMissing(value))
            {
                return true;
            }

            if (value is string text)
            {
                return string.IsNullOrWhiteSpace(text);
            }

            if (value is ICollection collection)
            {
                return collection.Count == 0;
            }

            return string.IsNullOrWhiteSpace(SafeConvert.ToText(value));
        }

        /// <summary>Splits a value on <paramref name="separator"/>; empty when the value is not text.</summary>
        public static IEnumerable<string> Split(object? source, string? path = null, string separator = ",")
        {
            var text = Str(source, path);

            return string.IsNullOrWhiteSpace(text)
                ? Array.Empty<string>()
                : text.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>Runs <paramref name="valueFactory"/> and falls back instead of failing the render.</summary>
        public static T Try<T>(Func<T> valueFactory, T fallback)
        {
            try
            {
                return valueFactory();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TemplateHelpers] Suppressed template expression error: {ex.Message}");
                return fallback;
            }
        }

        /// <summary>Runs <paramref name="action"/> and swallows any failure (block level isolation).</summary>
        public static void TryRun(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TemplateHelpers] Suppressed template block error: {ex.Message}");
            }
        }

        private static object? Resolve(object? source, string? path)
        {
            if (SafeConvert.IsMissing(source))
            {
                return SafeMissing.Instance;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return source;
            }

            object? current = source;

            foreach (var rawSegment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                var segment = rawSegment.Trim();

                if (SafeConvert.IsMissing(current))
                {
                    return SafeMissing.Instance;
                }

                current = ResolveSegment(current, segment);
            }

            return current ?? SafeMissing.Instance;
        }

        private static object? ResolveSegment(object? current, string segment)
        {
            if (current is SafeValue)
            {
                return SafeMissing.Instance;
            }

            if (current is SafeList list)
            {
                if (int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var listIndex))
                {
                    return listIndex >= 0 && listIndex < list.Count ? list[listIndex] : SafeMissing.Instance;
                }

                return SafeMissing.Instance;
            }

            if (current is SafeDynamicObject safeObject)
            {
                return safeObject.Lookup(segment);
            }

            if (current is IDictionary<string, object> dictionary)
            {
                if (dictionary.TryGetValue(segment, out var value))
                {
                    return value;
                }

                foreach (var pair in dictionary)
                {
                    if (string.Equals(pair.Key, segment, StringComparison.OrdinalIgnoreCase))
                    {
                        return pair.Value;
                    }
                }

                return SafeMissing.Instance;
            }

            if (int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                if (current is IList indexed)
                {
                    return index >= 0 && index < indexed.Count ? indexed[index] : SafeMissing.Instance;
                }

                if (current is IEnumerable sequence and not string)
                {
                    return sequence.Cast<object>().ElementAtOrDefault(index) ?? SafeMissing.Instance;
                }
            }

            if (current is IDictionary legacyDictionary && legacyDictionary.Contains(segment))
            {
                return legacyDictionary[segment];
            }

            try
            {
                var property = current!.GetType().GetProperty(
                    segment,
                    System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.IgnoreCase);

                return property != null ? property.GetValue(current) : SafeMissing.Instance;
            }
            catch
            {
                return SafeMissing.Instance;
            }
        }
    }
}
