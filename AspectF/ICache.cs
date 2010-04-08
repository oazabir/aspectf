using System;

namespace OmarALZabir.AspectF
{
    public interface ICache
    {
        void Add(string key, object value);
        void Add(string key, object value, TimeSpan timeout);
        void Set(string key, object value);
        void Set(string key, object value, TimeSpan timeout);
        bool Contains(string key);
        void Flush();
        object Get(string key);
        void Remove(string key);
    }
}
