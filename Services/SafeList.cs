using System.Collections;
using System.Dynamic;

namespace GiddhTemplate.Services
{
    /// <summary>
    /// List view over a payload array. Out-of-range indexers and unknown members
    /// return the missing value instead of throwing, so a template can keep rendering.
    /// </summary>
    public sealed class SafeList : DynamicObject, IList<object>, IReadOnlyList<object>
    {
        private readonly List<object> _items;

        public SafeList(IEnumerable<object>? items)
        {
            _items = items?.ToList() ?? new List<object>();
        }

        public object this[int index]
        {
            get => index >= 0 && index < _items.Count ? _items[index] : SafeMissing.Instance;
            set
            {
                if (index >= 0 && index < _items.Count)
                {
                    _items[index] = value ?? SafeMissing.Instance;
                }
            }
        }

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (binder.Name.Equals(nameof(Count), StringComparison.OrdinalIgnoreCase)
                || binder.Name.Equals("Length", StringComparison.OrdinalIgnoreCase))
            {
                result = _items.Count;
                return true;
            }

            result = SafeMissing.Instance;
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            if (indexes is { Length: 1 } && int.TryParse(indexes[0]?.ToString(), out var index))
            {
                result = this[index];
                return true;
            }

            result = SafeMissing.Instance;
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
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

        public void Add(object item) => _items.Add(item ?? SafeMissing.Instance);

        public void Clear() => _items.Clear();

        public bool Contains(object item) => _items.Contains(item);

        public void CopyTo(object[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        public IEnumerator<object> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int IndexOf(object item) => _items.IndexOf(item);

        public void Insert(int index, object item)
        {
            if (index < 0 || index > _items.Count)
            {
                return;
            }

            _items.Insert(index, item ?? SafeMissing.Instance);
        }

        public bool Remove(object item) => _items.Remove(item);

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < _items.Count)
            {
                _items.RemoveAt(index);
            }
        }
    }
}
