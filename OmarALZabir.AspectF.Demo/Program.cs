using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmarALZabir.AspectF.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            //Action<Action> actionsChain;

            //actionsChain = work =>
            //                             {
            //                                 Console.WriteLine("1");
            //                                 work();
            //                             };



            //Action<int> squareAction = val => Console.WriteLine(val * val);
            //squareAction(10);

            //return;
            var consoleLogger = new ConsoleLogger();
            //Let.Us
            //    .TrapLog(consoleLogger)
            //    .Retry(consoleLogger)
            //    .Return(() =>
            //                {
            //                    Console.WriteLine("[code]");
            //                    return "ret";
            //                });

            Let.Logger = () => consoleLogger;
            Let.Us
                .TrapLog()
                .Retry()
                .Return(() =>
                            {
                                Console.WriteLine("[code]");
                                return "ret";
                            });
        }
    }

    public class ConsoleLogger : ILogger
    {
        #region Implementation of ILogger

        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void Log(string[] categories, string message)
        {
            Console.WriteLine("{0} [{1}]", message, string.Join(", ", categories));

        }

        public void LogException(Exception x)
        {
            Console.WriteLine("[Exception]: {0}", x.Message);
        }

        #endregion
    }
}
