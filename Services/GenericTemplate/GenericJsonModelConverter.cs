using System.Collections;
using System.Text.Json;

namespace GiddhTemplate.Services.GenericTemplate
{
    public static class GenericJsonModelConverter
    {
        public static object? Convert(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => ConvertObject(element),
                JsonValueKind.Array => ConvertArray(element),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => ConvertNumber(element),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.GetRawText()
            };
        }

        public static object Normalize(object? model)
        {
            if (model is JsonElement jsonElement)
            {
                return Convert(jsonElement) ?? MissingKeyProxy.Instance;
            }

            if (model is SafeDynamicObject or MissingKeyProxy)
            {
                return model;
            }

            if (model is IDictionary<string, object?> dictionary)
            {
                return ConvertDictionary(dictionary);
            }

            if (model is IDictionary<string, object> legacyDictionary)
            {
                return ConvertDictionary(legacyDictionary.ToDictionary(
                    pair => pair.Key,
                    pair => (object?)pair.Value));
            }

            return model ?? MissingKeyProxy.Instance;
        }

        private static SafeDynamicObject ConvertObject(JsonElement element)
        {
            var entries = new List<KeyValuePair<string, object?>>();

            foreach (var property in element.EnumerateObject())
            {
                entries.Add(new KeyValuePair<string, object?>(property.Name, Convert(property.Value)));
            }

            return new SafeDynamicObject(entries);
        }

        private static List<object?> ConvertArray(JsonElement element)
        {
            return element.EnumerateArray().Select(item => Convert(item)).ToList();
        }

        private static object? ConvertNumber(JsonElement element)
        {
            if (element.TryGetInt64(out var longValue))
            {
                if (longValue is >= int.MinValue and <= int.MaxValue)
                {
                    return (int)longValue;
                }

                return longValue;
            }

            return element.GetDouble();
        }

        private static SafeDynamicObject ConvertDictionary(IEnumerable<KeyValuePair<string, object?>> dictionary)
        {
            var entries = new List<KeyValuePair<string, object?>>();

            foreach (var pair in dictionary)
            {
                entries.Add(new KeyValuePair<string, object?>(pair.Key, NormalizeValue(pair.Value)));
            }

            return new SafeDynamicObject(entries);
        }

        private static object? NormalizeValue(object? value)
        {
            if (value is null or string or SafeDynamicObject or MissingKeyProxy)
            {
                return value;
            }

            if (value is JsonElement jsonElement)
            {
                return Convert(jsonElement);
            }

            if (value is IDictionary<string, object?> childDictionary)
            {
                return ConvertDictionary(childDictionary);
            }

            if (value is IDictionary<string, object> legacyDictionary)
            {
                return ConvertDictionary(legacyDictionary.ToDictionary(
                    pair => pair.Key,
                    pair => (object?)pair.Value));
            }

            if (value is IList list)
            {
                return list.Cast<object?>().Select(NormalizeValue).ToList();
            }

            return value;
        }
    }
}
