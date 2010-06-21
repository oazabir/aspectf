using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmarALZabir.AspectF
{
    internal class SampleCache : ICache
    {
        #region ICache Members

        public void Add(string key, object value)
        {
            
        }

        public void Add(string key, object value, TimeSpan timeout)
        {
            
        }

        public void Set(string key, object value)
        {
            
        }

        public void Set(string key, object value, TimeSpan timeout)
        {
            
        }

        public bool Contains(string key)
        {
            return false;
        }

        public void Flush()
        {
            
        }

        public object Get(string key)
        {
            return null;
        }

        public void Remove(string key)
        {
            
        }

        #endregion
    }
}
