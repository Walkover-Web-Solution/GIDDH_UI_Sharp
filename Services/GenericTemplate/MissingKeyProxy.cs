using System.Collections;
using System.Dynamic;
using System.Linq.Expressions;

namespace GiddhTemplate.Services.GenericTemplate
{
    /// <summary>
    /// Returned for missing dictionary keys so chained access (<c>Model?.company?.name</c>),
    /// <c>foreach</c>, string concatenation, and comparisons do not throw at render time.
    /// </summary>
    public sealed class MissingKeyProxy : DynamicObject, IEnumerable
    {
        public static readonly MissingKeyProxy Instance = new();

        private MissingKeyProxy()
        {
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (string.Equals(binder.Name, "Count", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(binder.Name, "Length", StringComparison.OrdinalIgnoreCase))
            {
                result = 0;
                return true;
            }

            result = Instance;
            return true;
        }

        public override bool TryConvert(ConvertBinder binder, out object? result)
        {
            if (binder.Type == typeof(string))
            {
                result = string.Empty;
                return true;
            }

            if (binder.Type == typeof(bool))
            {
                result = false;
                return true;
            }

            if (binder.Type == typeof(int) || binder.Type == typeof(long) ||
                binder.Type == typeof(double) || binder.Type == typeof(decimal) ||
                binder.Type == typeof(float))
            {
                result = Activator.CreateInstance(binder.Type);
                return true;
            }

            result = null;
            return binder.Type.IsClass || Nullable.GetUnderlyingType(binder.Type) != null;
        }

        public override bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object? result)
        {
            if (binder.Operation == ExpressionType.Add)
            {
                result = string.Concat(string.Empty, arg?.ToString() ?? string.Empty);
                return true;
            }

            if (binder.Operation == ExpressionType.Equal)
            {
                result = EqualsMissingValue(arg);
                return true;
            }

            if (binder.Operation == ExpressionType.NotEqual)
            {
                result = !EqualsMissingValue(arg);
                return true;
            }

            result = null;
            return false;
        }

        public override bool TryUnaryOperation(UnaryOperationBinder binder, out object? result)
        {
            if (binder.Operation == ExpressionType.Not)
            {
                result = true;
                return true;
            }

            result = null;
            return false;
        }

        public IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();

        public override string ToString() => string.Empty;

        public override bool Equals(object? obj) => EqualsMissingValue(obj);

        public override int GetHashCode() => 0;

        public static bool operator ==(MissingKeyProxy? left, MissingKeyProxy? right) => true;

        public static bool operator !=(MissingKeyProxy? left, MissingKeyProxy? right) => false;

        public static bool operator ==(MissingKeyProxy? left, string? right) =>
            string.IsNullOrEmpty(right);

        public static bool operator !=(MissingKeyProxy? left, string? right) =>
            !string.IsNullOrEmpty(right);

        public static bool operator ==(string? left, MissingKeyProxy? right) =>
            string.IsNullOrEmpty(left);

        public static bool operator !=(string? left, MissingKeyProxy? right) =>
            !string.IsNullOrEmpty(left);

        public static bool operator ==(MissingKeyProxy? left, object? right) =>
            EqualsMissingValue(right);

        public static bool operator !=(MissingKeyProxy? left, object? right) =>
            !EqualsMissingValue(right);

        public static bool operator ==(object? left, MissingKeyProxy? right) =>
            EqualsMissingValue(left);

        public static bool operator !=(object? left, MissingKeyProxy? right) =>
            !EqualsMissingValue(left);

        private static bool EqualsMissingValue(object? value)
        {
            return value switch
            {
                null or MissingKeyProxy => true,
                string text => string.IsNullOrEmpty(text),
                IEnumerable enumerable => !enumerable.Cast<object?>().Any(),
                _ => false
            };
        }
    }
}
