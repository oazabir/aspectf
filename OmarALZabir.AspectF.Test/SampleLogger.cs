using System;
using System.IO;

namespace OmarALZabir.AspectF
{
    internal class SampleLogger : ILogger
    {
        public static readonly TextWriter Writer = Console.Out;

        #region ILogger Members

        public void Log(string message)
        {
            
        }

        public void Log(string[] categories, string message)
        {
            
        }

        public void LogException(Exception x)
        {
            
        }

        #endregion
    }
}
