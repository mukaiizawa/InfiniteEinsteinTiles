using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class ConcurrentSet<T>
{

    HashSet<T> _set = new HashSet<T>();
    object _lock = new object();

    public bool ContainsAll(IEnumerable<T> items)
    {
        lock (_lock)
        {
            foreach (T item in items)
                if (!_set.Contains(item)) return false;
            return true;
        }
    }

    public bool ContainsAny(IEnumerable<T> items)
    {
        lock (_lock)
        {
            foreach (T item in items)
                if (_set.Contains(item)) return true;
            return false;
        }
    }

    public bool TryAddAll(IEnumerable<T> items)
    {
        lock (_lock)
        {
            foreach (T item in items)
                if (_set.Contains(item)) return false;
            foreach (T item in items)
                _set.Add(item);
            return true;
        }
    }

    public void RemoveAll(IEnumerable<T> items)
    {
        lock (_lock)
        {
            foreach (T item in items)
                _set.Remove(item);
        }
    }

    public T[] ToArray()
    {
        lock (_lock)
        {
            var array = new T[_set.Count];
            _set.CopyTo(array);
            return array;
        }
    }

    public int Count()
    {
        lock (_lock)
        {
            return _set.Count;
        }
    }

}
