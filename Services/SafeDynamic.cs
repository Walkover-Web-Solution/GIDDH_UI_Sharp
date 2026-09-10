using System.Collections;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.Json;

namespace GiddhTemplate.Services
{
    /// <summary>
    /// Represents a payload value that is absent or null.
    /// Renders as empty text, enumerates as an empty sequence, reports no keys and
    /// participates in arithmetic as zero, so a template never throws for a missing key.
    /// </summary>
    public sealed class SafeMissing : DynamicObject, IDictionary<string, object>
    {
        public static readonly SafeMissing Instance = new SafeMissing();

        private SafeMissing() { }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = Instance;
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value) => true;

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            result = Instance;
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object? value) => true;

        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            result = Instance;
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object? result)
        {
            result = SafeConvert.DefaultOf(binder.Type, this);
            return true;
        }

        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object? result)
        {
            result = binder.Operation switch
            {
                ExpressionType.Not => true,
                ExpressionType.IsFalse => true,
                ExpressionType.IsTrue => false,
                ExpressionType.Negate or ExpressionType.UnaryPlus => 0m,
                _ => Instance
            };
            return true;
        }

        public override bool TryBinaryOperation(BinaryOperationBinder binder, object? arg, out object? result)
        {
            result = SafeConvert.MissingBinaryOperation(binder.Operation, arg);
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames() => Array.Empty<string>();

        public override string ToString() => string.Empty;

        public static implicit operator decimal(SafeMissing _) => 0m;
        public static implicit operator double(SafeMissing _) => 0d;
        public static implicit operator int(SafeMissing _) => 0;
        public static implicit operator long(SafeMissing _) => 0L;
        public static implicit operator bool(SafeMissing _) => false;
        public static implicit operator string(SafeMissing _) => string.Empty;

        // IDictionary<string, object> — keeps the `(IDictionary<string, object>)obj` checks
        // inside the templates working and makes `foreach` over a missing value a no-op.
        public object this[string key]
        {
            get => Instance;
            set { }
        }

        public ICollection<string> Keys => Array.Empty<string>();
        public ICollection<object> Values => Array.Empty<object>();
        public int Count => 0;
        public bool IsReadOnly => true;

        public bool ContainsKey(string key) => false;
        public bool TryGetValue(string key, out object value)
        {
            value = Instance;
            return false;
        }

        public void Add(string key, object value) { }
        public void Add(KeyValuePair<string, object> item) { }
        public void Clear() { }
        public bool Contains(KeyValuePair<string, object> item) => false;
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) { }
        public bool Remove(string key) => false;
        public bool Remove(KeyValuePair<string, object> item) => false;

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() =>
            Enumerable.Empty<KeyValuePair<string, object>>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Dynamic view over a payload object. Unknown keys resolve to <see cref="SafeMissing"/>
    /// instead of throwing a runtime binder exception. Key lookup falls back to a
    /// case-insensitive match so casing differences in the payload do not break a template.
    /// </summary>
    public sealed class SafeDynamicObject : DynamicObject, IDictionary<string, object>
    {
        private readonly Dictionary<string, object> _values;

        public SafeDynamicObject(Dictionary<string, object> values)
        {
            _values = values ?? new Dictionary<string, object>(StringComparer.Ordinal);
        }

        public object Lookup(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return SafeMissing.Instance;
            }

            if (_values.TryGetValue(key, out var value))
            {
                return value ?? SafeMissing.Instance;
            }

            foreach (var pair in _values)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value ?? SafeMissing.Instance;
                }
            }

            return SafeMissing.Instance;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            result = Lookup(binder.Name);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            _values[binder.Name] = value ?? SafeMissing.Instance;
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            result = indexes is { Length: 1 } && indexes[0] != null
                ? Lookup(indexes[0].ToString() ?? string.Empty)
                : SafeMissing.Instance;
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            // Unknown method on a payload object (for example `.Split(",")` on a non-string):
            // resolve to the missing value rather than failing the whole render.
            result = SafeMissing.Instance;
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object? result)
        {
            if (binder.Type.IsInstanceOfType(this))
            {
                result = this;
                return true;
            }

            result = SafeConvert.DefaultOf(binder.Type, this);
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames() => _values.Keys;

        public override string ToString() => string.Empty;

        public object this[string key]
        {
            get => Lookup(key);
            set => _values[key] = value ?? SafeMissing.Instance;
        }

        public ICollection<string> Keys => _values.Keys;
        public ICollection<object> Values => _values.Values;
        public int Count => _values.Count;
        public bool IsReadOnly => false;

        public bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (_values.ContainsKey(key))
            {
                return true;
            }

            foreach (var pair in _values)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetValue(string key, out object value)
        {
            value = Lookup(key);
            return ContainsKey(key);
        }

        public void Add(string key, object value) => _values[key] = value ?? SafeMissing.Instance;
        public void Add(KeyValuePair<string, object> item) => Add(item.Key, item.Value);
        public void Clear() => _values.Clear();
        public bool Contains(KeyValuePair<string, object> item) => ContainsKey(item.Key);
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) =>
            ((IDictionary<string, object>)_values).CopyTo(array, arrayIndex);
        public bool Remove(string key) => _values.Remove(key);
        public bool Remove(KeyValuePair<string, object> item) => _values.Remove(item.Key);

        // Empty on purpose: foreach over a payload *object* (when a list was expected)
        // must skip the section, not iterate dictionary keys and then crash on KeyValuePair.
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() =>
            Enumerable.Empty<KeyValuePair<string, object>>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Builds the safe dynamic model that every generic template is rendered against.
    /// </summary>
    public static class SafePayload
    {
        /// <summary>
        /// Converts an arbitrary payload (JsonElement, ExpandoObject, dictionary, list or scalar)
        /// into safe dynamic values. JSON numbers become <see cref="decimal"/> so mixed
        /// int/double payloads never break arithmetic inside a template.
        /// </summary>
        public static object From(object? value)
        {
            switch (value)
            {
                case null:
                    return SafeMissing.Instance;
                case SafeMissing:
                case SafeDynamicObject:
                case SafeValue:
                case SafeList:
                    return value;
                case JsonElement element:
                    return FromJsonElement(element);
                case JsonDocument document:
                    return FromJsonElement(document.RootElement);
                case string or bool or decimal:
                    return new SafeValue(value);
                case DateTime or DateTimeOffset or Guid:
                    return new SafeValue(value);
                case int or long or short or byte or sbyte or uint or ulong or ushort:
                    return new SafeValue(Convert.ToDecimal(value, CultureInfo.InvariantCulture));
                case float or double:
                    return new SafeValue(SafeConvert.ToDecimal(value));
                case IDictionary<string, object> dictionary:
                    return FromDictionary(dictionary);
                case IDictionary legacyDictionary:
                    {
                        var map = new Dictionary<string, object>(StringComparer.Ordinal);
                        foreach (DictionaryEntry entry in legacyDictionary)
                        {
                            map[entry.Key?.ToString() ?? string.Empty] = From(entry.Value);
                        }
                        return new SafeDynamicObject(map);
                    }
                case IEnumerable sequence:
                    {
                        var items = new List<object>();
                        foreach (var item in sequence)
                        {
                            items.Add(From(item));
                        }
                        return new SafeList(items);
                    }
                default:
                    return value;
            }
        }

        private static object FromDictionary(IDictionary<string, object> dictionary)
        {
            var map = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var pair in dictionary)
            {
                map[pair.Key] = From(pair.Value);
            }
            return new SafeDynamicObject(map);
        }

        private static object FromJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    {
                        var map = new Dictionary<string, object>(StringComparer.Ordinal);
                        foreach (var property in element.EnumerateObject())
                        {
                            map[property.Name] = FromJsonElement(property.Value);
                        }
                        return new SafeDynamicObject(map);
                    }
                case JsonValueKind.Array:
                    {
                        var items = new List<object>();
                        foreach (var item in element.EnumerateArray())
                        {
                            items.Add(FromJsonElement(item));
                        }
                        return new SafeList(items);
                    }
                case JsonValueKind.String:
                    return new SafeValue(element.GetString() ?? string.Empty);
                case JsonValueKind.Number:
                    if (element.TryGetDecimal(out var decimalValue))
                    {
                        return new SafeValue(decimalValue);
                    }
                    return element.TryGetDouble(out var doubleValue)
                        ? new SafeValue(SafeConvert.ToDecimal(doubleValue))
                        : new SafeValue(element.GetRawText());
                case JsonValueKind.True:
                    return new SafeValue(true);
                case JsonValueKind.False:
                    return new SafeValue(false);
                default:
                    return SafeMissing.Instance;
            }
        }
    }

    /// <summary>
    /// Type coercion used by the safe model and by the template helpers.
    /// Every conversion is total: an unexpected type yields the neutral value instead of throwing.
    /// </summary>
    public static class SafeConvert
    {
        public static bool IsMissing(object? value) => value is null || value is SafeMissing;

        public static decimal ToDecimal(object? value)
        {
            switch (value)
            {
                case SafeValue wrapped:
                    return ToDecimal(wrapped.Raw);
                case null:
                case SafeMissing:
                    return 0m;
                case decimal decimalValue:
                    return decimalValue;
                case bool boolValue:
                    return boolValue ? 1m : 0m;
                case double doubleValue:
                    return double.IsNaN(doubleValue) || double.IsInfinity(doubleValue)
                        ? 0m
                        : (decimal)Math.Clamp(doubleValue, (double)decimal.MinValue, (double)decimal.MaxValue);
                case float floatValue:
                    return ToDecimal((double)floatValue);
                case IConvertible convertible when value is not string:
                    try
                    {
                        return convertible.ToDecimal(CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        return 0m;
                    }
            }

            var text = value.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0m;
            }

            text = text.Trim().Replace(",", string.Empty).Replace("\u00A0", string.Empty);

            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0m;
        }

        public static int ToInt(object? value)
        {
            var number = ToDecimal(value);
            if (number > int.MaxValue) return int.MaxValue;
            if (number < int.MinValue) return int.MinValue;
            return (int)decimal.Truncate(number);
        }

        public static bool ToBool(object? value)
        {
            switch (value)
            {
                case SafeValue wrapped:
                    return ToBool(wrapped.Raw);
                case null:
                case SafeMissing:
                    return false;
                case bool boolValue:
                    return boolValue;
                case string text:
                    text = text.Trim();
                    if (bool.TryParse(text, out var parsedBool)) return parsedBool;
                    if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedNumber))
                        return parsedNumber != 0m;
                    return text.Equals("yes", StringComparison.OrdinalIgnoreCase)
                        || text.Equals("y", StringComparison.OrdinalIgnoreCase)
                        || text.Equals("on", StringComparison.OrdinalIgnoreCase);
                default:
                    return ToDecimal(value) != 0m;
            }
        }

        public static string ToText(object? value)
        {
            switch (value)
            {
                case SafeValue wrapped:
                    return ToText(wrapped.Raw);
                case null:
                case SafeMissing:
                    return string.Empty;
                case string text:
                    return text;
                case bool boolValue:
                    return boolValue ? "true" : "false";
                case decimal decimalValue:
                    return decimalValue.ToString(CultureInfo.InvariantCulture);
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return value.ToString() ?? string.Empty;
            }
        }

        public static IEnumerable<object> ToList(object? value)
        {
            switch (value)
            {
                case SafeValue:
                    return Array.Empty<object>();
                case null:
                case SafeMissing:
                case string:
                case SafeDynamicObject:
                    return Array.Empty<object>();
                case SafeList list:
                    return list;
                case IEnumerable<object> typed:
                    return typed;
                case IEnumerable sequence:
                    return sequence.Cast<object>();
                default:
                    return Array.Empty<object>();
            }
        }

        /// <summary>Neutral value for a cast applied to a missing payload value.</summary>
        public static object? DefaultOf(Type type, object self)
        {
            if (type.IsInstanceOfType(self))
            {
                return self;
            }

            if (type == typeof(string))
            {
                return string.Empty;
            }

            var target = Nullable.GetUnderlyingType(type);
            if (target != null)
            {
                return null;
            }

            if (type == typeof(bool)) return false;
            if (type == typeof(decimal)) return 0m;
            if (type == typeof(double)) return 0d;
            if (type == typeof(float)) return 0f;
            if (type == typeof(int)) return 0;
            if (type == typeof(long)) return 0L;
            if (type == typeof(short)) return (short)0;
            if (type == typeof(byte)) return (byte)0;

            if (type.IsAssignableFrom(typeof(List<object>)))
            {
                return new List<object>();
            }

            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        /// <summary>
        /// Result of an operator applied to a missing value. The missing value counts as
        /// zero (or empty text) and equals null so existing `!= null` guards keep working.
        /// </summary>
        public static object MissingBinaryOperation(ExpressionType operation, object? other)
        {
            var isNullLike = IsMissing(other);

            switch (operation)
            {
                case ExpressionType.Equal:
                    return isNullLike;
                case ExpressionType.NotEqual:
                    return !isNullLike;
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return other is string text ? text : ToDecimal(other);
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return -ToDecimal(other);
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                    return 0m;
                case ExpressionType.LessThan:
                    return 0m < ToDecimal(other);
                case ExpressionType.LessThanOrEqual:
                    return 0m <= ToDecimal(other);
                case ExpressionType.GreaterThan:
                    return 0m > ToDecimal(other);
                case ExpressionType.GreaterThanOrEqual:
                    return 0m >= ToDecimal(other);
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return false;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return ToBool(other);
                default:
                    return SafeMissing.Instance;
            }
        }
    }
}
