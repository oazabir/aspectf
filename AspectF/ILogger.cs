using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmarALZabir.AspectF
{
    public interface ILogger
    {
        void Log(string message);
        void Log(string[] categories, string message);
        void LogException(Exception x);
    }
}
