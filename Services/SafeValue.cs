using System.Collections;
using System.Dynamic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace GiddhTemplate.Services
{
    /// <summary>
    /// Dynamic wrapper around a scalar payload value (string / number / bool).
    /// Casts, arithmetic and equality never throw: an unexpected counterpart
    /// is coerced or treated as the neutral value so a template can keep rendering.
    /// </summary>
    public sealed class SafeValue : DynamicObject, IConvertible, IComparable, IEnumerable
    {
        public SafeValue(object raw)
        {
            Raw = raw ?? string.Empty;
        }

        public object Raw { get; }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (binder.Name.Equals("Count", StringComparison.OrdinalIgnoreCase)
                || binder.Name.Equals("Length", StringComparison.OrdinalIgnoreCase))
            {
                result = 0;
                return true;
            }

            result = SafeMissing.Instance;
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            result = SafeMissing.Instance;
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object? result)
        {
            result = ConvertTo(binder.Type);
            return true;
        }

        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object? result)
        {
            switch (binder.Operation)
            {
                case ExpressionType.Not:
                    result = !SafeConvert.ToBool(Raw);
                    return true;
                case ExpressionType.IsFalse:
                    result = !SafeConvert.ToBool(Raw);
                    return true;
                case ExpressionType.IsTrue:
                    result = SafeConvert.ToBool(Raw);
                    return true;
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    result = -SafeConvert.ToDecimal(Raw);
                    return true;
                case ExpressionType.UnaryPlus:
                    result = SafeConvert.ToDecimal(Raw);
                    return true;
                default:
                    result = this;
                    return true;
            }
        }

        public override bool TryBinaryOperation(BinaryOperationBinder binder, object? arg, out object? result)
        {
            var other = Unwrap(arg);

            if (SafeConvert.IsMissing(other))
            {
                result = MissingOperand(binder.Operation);
                return true;
            }

            switch (binder.Operation)
            {
                case ExpressionType.Equal:
                    result = AreEqual(other);
                    return true;
                case ExpressionType.NotEqual:
                    result = !AreEqual(other);
                    return true;
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    result = Add(other);
                    return true;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    result = SafeConvert.ToDecimal(Raw) - SafeConvert.ToDecimal(other);
                    return true;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    result = SafeConvert.ToDecimal(Raw) * SafeConvert.ToDecimal(other);
                    return true;
                case ExpressionType.Divide:
                    {
                        var divisor = SafeConvert.ToDecimal(other);
                        result = divisor == 0m ? 0m : SafeConvert.ToDecimal(Raw) / divisor;
                        return true;
                    }
                case ExpressionType.Modulo:
                    {
                        var divisor = SafeConvert.ToDecimal(other);
                        result = divisor == 0m ? 0m : SafeConvert.ToDecimal(Raw) % divisor;
                        return true;
                    }
                case ExpressionType.LessThan:
                    result = SafeConvert.ToDecimal(Raw) < SafeConvert.ToDecimal(other);
                    return true;
                case ExpressionType.LessThanOrEqual:
                    result = SafeConvert.ToDecimal(Raw) <= SafeConvert.ToDecimal(other);
                    return true;
                case ExpressionType.GreaterThan:
                    result = SafeConvert.ToDecimal(Raw) > SafeConvert.ToDecimal(other);
                    return true;
                case ExpressionType.GreaterThanOrEqual:
                    result = SafeConvert.ToDecimal(Raw) >= SafeConvert.ToDecimal(other);
                    return true;
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    result = SafeConvert.ToBool(Raw) && SafeConvert.ToBool(other);
                    return true;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    result = SafeConvert.ToBool(Raw) || SafeConvert.ToBool(other);
                    return true;
                default:
                    result = this;
                    return true;
            }
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            result = Invoke(binder.Name, args ?? Array.Empty<object?>());
            return true;
        }

        public override string ToString()
        {
            return Raw switch
            {
                string text => text,
                IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture) ?? string.Empty,
                _ => Raw.ToString() ?? string.Empty
            };
        }

        public static implicit operator decimal(SafeValue value) => SafeConvert.ToDecimal(value?.Raw);
        public static implicit operator double(SafeValue value) => (double)SafeConvert.ToDecimal(value?.Raw);
        public static implicit operator int(SafeValue value) => SafeConvert.ToInt(value?.Raw);
        public static implicit operator long(SafeValue value) => (long)decimal.Truncate(SafeConvert.ToDecimal(value?.Raw));
        public static implicit operator bool(SafeValue value) => SafeConvert.ToBool(value?.Raw);
        public static implicit operator string(SafeValue? value) => value?.ToString() ?? string.Empty;

        public override bool Equals(object? obj) => AreEqual(Unwrap(obj));

        public override int GetHashCode() => Raw.GetHashCode();

        public int CompareTo(object? obj)
        {
            var other = Unwrap(obj);
            if (SafeConvert.IsMissing(other))
            {
                return 1;
            }

            return SafeConvert.ToDecimal(Raw).CompareTo(SafeConvert.ToDecimal(other));
        }

        public IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();

        TypeCode IConvertible.GetTypeCode() => TypeCode.Object;
        bool IConvertible.ToBoolean(IFormatProvider? provider) => SafeConvert.ToBool(Raw);
        byte IConvertible.ToByte(IFormatProvider? provider) => (byte)Math.Clamp(SafeConvert.ToInt(Raw), byte.MinValue, byte.MaxValue);
        char IConvertible.ToChar(IFormatProvider? provider) => ToString().FirstOrDefault();
        DateTime IConvertible.ToDateTime(IFormatProvider? provider) => DateTime.TryParse(ToString(), provider, DateTimeStyles.None, out var date) ? date : DateTime.MinValue;
        decimal IConvertible.ToDecimal(IFormatProvider? provider) => SafeConvert.ToDecimal(Raw);
        double IConvertible.ToDouble(IFormatProvider? provider) => (double)SafeConvert.ToDecimal(Raw);
        short IConvertible.ToInt16(IFormatProvider? provider) => (short)Math.Clamp(SafeConvert.ToInt(Raw), short.MinValue, short.MaxValue);
        int IConvertible.ToInt32(IFormatProvider? provider) => SafeConvert.ToInt(Raw);
        long IConvertible.ToInt64(IFormatProvider? provider) => (long)decimal.Truncate(SafeConvert.ToDecimal(Raw));
        sbyte IConvertible.ToSByte(IFormatProvider? provider) => (sbyte)Math.Clamp(SafeConvert.ToInt(Raw), sbyte.MinValue, sbyte.MaxValue);
        float IConvertible.ToSingle(IFormatProvider? provider) => (float)SafeConvert.ToDecimal(Raw);
        string IConvertible.ToString(IFormatProvider? provider) => ToString();
        object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ConvertTo(conversionType) ?? SafeConvert.DefaultOf(conversionType, this)!;
        ushort IConvertible.ToUInt16(IFormatProvider? provider) => (ushort)Math.Clamp(SafeConvert.ToInt(Raw), ushort.MinValue, ushort.MaxValue);
        uint IConvertible.ToUInt32(IFormatProvider? provider) => (uint)Math.Max(0, SafeConvert.ToInt(Raw));
        ulong IConvertible.ToUInt64(IFormatProvider? provider) => (ulong)Math.Max(0, (long)decimal.Truncate(SafeConvert.ToDecimal(Raw)));

        internal object? ConvertTo(Type type)
        {
            if (type.IsInstanceOfType(this))
            {
                return this;
            }

            if (type.IsInstanceOfType(Raw))
            {
                return Raw;
            }

            var target = Nullable.GetUnderlyingType(type) ?? type;

            if (target == typeof(string)) return ToString();
            if (target == typeof(bool)) return SafeConvert.ToBool(Raw);
            if (target == typeof(decimal)) return SafeConvert.ToDecimal(Raw);
            if (target == typeof(double)) return (double)SafeConvert.ToDecimal(Raw);
            if (target == typeof(float)) return (float)SafeConvert.ToDecimal(Raw);
            if (target == typeof(int)) return SafeConvert.ToInt(Raw);
            if (target == typeof(long)) return (long)decimal.Truncate(SafeConvert.ToDecimal(Raw));
            if (target == typeof(short)) return (short)Math.Clamp(SafeConvert.ToInt(Raw), short.MinValue, short.MaxValue);
            if (target == typeof(byte)) return (byte)Math.Clamp(SafeConvert.ToInt(Raw), byte.MinValue, byte.MaxValue);
            if (target == typeof(object)) return Raw;

            return SafeConvert.DefaultOf(type, this);
        }

        private object Invoke(string name, object?[] args)
        {
            try
            {
                if (name.Equals("ToString", StringComparison.OrdinalIgnoreCase))
                {
                    return ToString();
                }

                if (name.Equals("Equals", StringComparison.OrdinalIgnoreCase))
                {
                    return AreEqual(args.Length > 0 ? args[0] : null);
                }

                if (Raw is string text)
                {
                    if (name.Equals("Split", StringComparison.OrdinalIgnoreCase))
                    {
                        var separator = args.Length > 0 ? args[0]?.ToString() ?? "," : ",";
                        return text.Split(separator, StringSplitOptions.None);
                    }

                    if (name.Equals("Contains", StringComparison.OrdinalIgnoreCase))
                    {
                        return text.Contains(args.FirstOrDefault()?.ToString() ?? string.Empty, StringComparison.Ordinal);
                    }

                    if (name.Equals("StartsWith", StringComparison.OrdinalIgnoreCase))
                    {
                        return text.StartsWith(args.FirstOrDefault()?.ToString() ?? string.Empty, StringComparison.Ordinal);
                    }

                    if (name.Equals("EndsWith", StringComparison.OrdinalIgnoreCase))
                    {
                        return text.EndsWith(args.FirstOrDefault()?.ToString() ?? string.Empty, StringComparison.Ordinal);
                    }

                    if (name.Equals("Trim", StringComparison.OrdinalIgnoreCase))
                    {
                        return text.Trim();
                    }
                }

                var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;
                var methods = Raw.GetType().GetMethods(flags)
                    .Where(method => method.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length != args.Length)
                    {
                        continue;
                    }

                    try
                    {
                        return method.Invoke(Raw, args) ?? SafeMissing.Instance;
                    }
                    catch
                    {
                        // try the next overload
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SafeValue] Suppressed '{name}' on {Raw.GetType().Name}: {ex.Message}");
            }

            return SafeMissing.Instance;
        }

        private object MissingOperand(ExpressionType operation)
        {
            return operation switch
            {
                ExpressionType.Equal => false,
                ExpressionType.NotEqual => true,
                ExpressionType.Add or ExpressionType.AddChecked => Raw is string ? ToString() : SafeConvert.ToDecimal(Raw),
                ExpressionType.Subtract or ExpressionType.SubtractChecked => SafeConvert.ToDecimal(Raw),
                ExpressionType.Multiply or ExpressionType.MultiplyChecked
                    or ExpressionType.Divide or ExpressionType.Modulo => 0m,
                ExpressionType.LessThan => SafeConvert.ToDecimal(Raw) < 0m,
                ExpressionType.LessThanOrEqual => SafeConvert.ToDecimal(Raw) <= 0m,
                ExpressionType.GreaterThan => SafeConvert.ToDecimal(Raw) > 0m,
                ExpressionType.GreaterThanOrEqual => SafeConvert.ToDecimal(Raw) >= 0m,
                ExpressionType.And or ExpressionType.AndAlso => false,
                ExpressionType.Or or ExpressionType.OrElse => SafeConvert.ToBool(Raw),
                _ => this
            };
        }

        private object Add(object other)
        {
            if (LooksNumeric(Raw) && LooksNumeric(other))
            {
                return new SafeValue(SafeConvert.ToDecimal(Raw) + SafeConvert.ToDecimal(other));
            }

            if (Raw is string || other is string)
            {
                return new SafeValue(ToString() + SafeConvert.ToText(other));
            }

            return new SafeValue(SafeConvert.ToDecimal(Raw) + SafeConvert.ToDecimal(other));
        }

        private static bool LooksNumeric(object? value)
        {
            if (IsNumeric(value))
            {
                return true;
            }

            if (value is string text)
            {
                text = text.Trim().Replace(",", string.Empty).Replace("\u00A0", string.Empty);
                return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
            }

            return false;
        }

        private bool AreEqual(object? other)
        {
            other = Unwrap(other);

            if (SafeConvert.IsMissing(other))
            {
                return false;
            }

            if (Raw is bool || other is bool)
            {
                return SafeConvert.ToBool(Raw) == SafeConvert.ToBool(other);
            }

            if (IsNumeric(Raw) && IsNumeric(other))
            {
                return SafeConvert.ToDecimal(Raw) == SafeConvert.ToDecimal(other);
            }

            if ((Raw is bool || IsNumeric(Raw)) && other is string
                || Raw is string && (other is bool || IsNumeric(other)))
            {
                if (other is bool || Raw is bool)
                {
                    return SafeConvert.ToBool(Raw) == SafeConvert.ToBool(other);
                }

                return SafeConvert.ToDecimal(Raw) == SafeConvert.ToDecimal(other);
            }

            return string.Equals(SafeConvert.ToText(Raw), SafeConvert.ToText(other), StringComparison.Ordinal);
        }

        private static bool IsNumeric(object? value)
        {
            if (value is SafeValue wrapped)
            {
                return IsNumeric(wrapped.Raw);
            }

            return value is sbyte or byte or short or ushort or int or uint or long or ulong
                or float or double or decimal;
        }

        private static object? Unwrap(object? value) => value is SafeValue wrapped ? wrapped.Raw : value;
    }
}
