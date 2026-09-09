using System.Collections;
using System.Text.Json;

namespace GiddhTemplate.Services.GenericTemplate
{
    public static class GenericTemplateHelpers
    {
        public static bool HasKey(object? obj, string propertyName)
        {
            if (obj == null || obj is MissingKeyProxy || string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            if (obj is SafeDynamicObject safeDynamicObject)
            {
                return safeDynamicObject.ContainsKey(propertyName);
            }

            if (obj is IDictionary<string, object?> dictionary)
            {
                return dictionary.Keys.Any(key => string.Equals(key, propertyName, StringComparison.OrdinalIgnoreCase));
            }

            if (obj is IDictionary<string, object> legacyDictionary)
            {
                return legacyDictionary.Keys.Any(key => string.Equals(key, propertyName, StringComparison.OrdinalIgnoreCase));
            }

            if (obj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                return jsonElement.EnumerateObject().Any(property =>
                    string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }

        public static object? Get(object? obj, string propertyName, object? defaultValue = null)
        {
            if (obj == null || obj is MissingKeyProxy || string.IsNullOrWhiteSpace(propertyName))
            {
                return defaultValue;
            }

            if (obj is SafeDynamicObject safeDynamicObject &&
                safeDynamicObject.TryGetValue(propertyName, out var safeValue))
            {
                return Normalize(safeValue, defaultValue);
            }

            if (obj is IDictionary<string, object?> dictionary)
            {
                var match = dictionary.FirstOrDefault(pair =>
                    string.Equals(pair.Key, propertyName, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(match.Key))
                {
                    return Normalize(match.Value, defaultValue);
                }
            }

            if (obj is IDictionary<string, object> legacyDictionary)
            {
                var match = legacyDictionary.FirstOrDefault(pair =>
                    string.Equals(pair.Key, propertyName, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(match.Key))
                {
                    return Normalize(match.Value, defaultValue);
                }
            }

            if (obj is JsonElement jsonElement &&
                jsonElement.ValueKind == JsonValueKind.Object &&
                TryGetJsonProperty(jsonElement, propertyName, out var jsonValue))
            {
                return Normalize(GenericJsonModelConverter.Convert(jsonValue), defaultValue);
            }

            return defaultValue;
        }

        public static string AsString(object? value, string defaultValue = "")
        {
            if (IsMissingOrEmpty(value))
            {
                return defaultValue;
            }

            return value switch
            {
                string text => text,
                bool boolean => boolean.ToString().ToLowerInvariant(),
                IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture) ?? defaultValue,
                _ => value.ToString() ?? defaultValue
            };
        }

        public static bool AsBool(object? value, bool defaultValue = false)
        {
            if (IsMissingOrEmpty(value))
            {
                return defaultValue;
            }

            return value switch
            {
                bool boolean => boolean,
                string text when bool.TryParse(text, out var parsedBool) => parsedBool,
                string text when string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) => true,
                string text when string.Equals(text, "0", StringComparison.OrdinalIgnoreCase) => false,
                int number => number != 0,
                long number => number != 0,
                double number => Math.Abs(number) > double.Epsilon,
                decimal number => number != 0,
                _ => defaultValue
            };
        }

        public static double AsDouble(object? value, double defaultValue = 0)
        {
            if (IsMissingOrEmpty(value))
            {
                return defaultValue;
            }

            return value switch
            {
                double number => number,
                float number => number,
                decimal number => (double)number,
                int number => number,
                long number => number,
                string text when double.TryParse(text, out var parsed) => parsed,
                _ => defaultValue
            };
        }

        public static int Count(object? value)
        {
            if (IsMissingOrEmpty(value))
            {
                return 0;
            }

            return value switch
            {
                ICollection collection => collection.Count,
                IEnumerable enumerable => enumerable.Cast<object?>().Count(),
                _ => 0
            };
        }

        public static bool IsNotEmpty(object? value) => !IsMissingOrEmpty(value);

        public static IEnumerable AsList(object? value)
        {
            if (IsMissingOrEmpty(value))
            {
                return Array.Empty<object>();
            }

            return value switch
            {
                IEnumerable enumerable => enumerable,
                _ => Array.Empty<object>()
            };
        }

        public static bool EqualsIgnoreCase(object? value, string? expected)
        {
            if (expected == null)
            {
                return value == null;
            }

            return string.Equals(AsString(value), expected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Safe equality for template comparisons when either side may be missing or a different type.</summary>
        public static bool Same(object? left, object? right) =>
            string.Equals(AsString(left), AsString(right), StringComparison.Ordinal);

        /// <summary>Safe inequality for template comparisons when either side may be missing or a different type.</summary>
        public static bool NotSame(object? left, object? right) => !Same(left, right);

        /// <summary>Show description only when it is non-empty and differs from item/account names.</summary>
        public static bool ShouldShowDescription(object? description, object? primaryName, object? secondaryName = null) =>
            IsNotEmpty(description) && NotSame(description, primaryName) && NotSame(description, secondaryName);

        private static bool TryGetJsonProperty(JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static object? Normalize(object? value, object? defaultValue)
        {
            if (IsMissingOrEmpty(value))
            {
                return defaultValue;
            }

            return value;
        }

        private static bool IsMissingOrEmpty(object? value)
        {
            return value is null or MissingKeyProxy ||
                   (value is string text && string.IsNullOrWhiteSpace(text));
        }
    }
}
