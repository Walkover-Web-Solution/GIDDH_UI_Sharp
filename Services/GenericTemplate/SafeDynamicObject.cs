using System.Collections;
using System.Dynamic;

namespace GiddhTemplate.Services.GenericTemplate
{
    public sealed class SafeDynamicObject : DynamicObject, IDictionary<string, object?>
    {
        private readonly Dictionary<string, object?> _data;

        public SafeDynamicObject(IEnumerable<KeyValuePair<string, object?>>? data = null, bool ignoreCase = true)
        {
            _data = data == null
                ? new Dictionary<string, object?>(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                : new Dictionary<string, object?>(data, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (_data.TryGetValue(binder.Name, out result))
            {
                return true;
            }

            result = MissingKeyProxy.Instance;
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            _data[binder.Name] = value;
            return true;
        }

        public bool ContainsKey(string key) => _data.ContainsKey(key);

        public bool TryGetValue(string key, out object? value) => _data.TryGetValue(key, out value);

        public ICollection<string> Keys => _data.Keys;

        public ICollection<object?> Values => _data.Values;

        public object? this[string key]
        {
            get => _data.TryGetValue(key, out var value) ? value : MissingKeyProxy.Instance;
            set => _data[key] = value;
        }

        public void Add(string key, object? value) => _data.Add(key, value);

        public bool Remove(string key) => _data.Remove(key);

        public void Clear() => _data.Clear();

        public int Count => _data.Count;

        public bool IsReadOnly => false;

        public void Add(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)_data).Add(item);

        public bool Contains(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)_data).Contains(item);

        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) =>
            ((IDictionary<string, object?>)_data).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)_data).Remove(item);

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
