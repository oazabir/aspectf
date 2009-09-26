using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmarALZabir.AspectF
{
    public interface ICacheResolver
    {
        object Get(string key);
        void Put(string key, object item);
    }
}
