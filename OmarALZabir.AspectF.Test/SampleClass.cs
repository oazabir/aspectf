using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Moq;

namespace OmarALZabir.AspectF
{
    class SampleClass
    {
        static SampleClass()
        {
            Let.Logger = () => new SampleLogger();
            Let.Cache = () => new SampleCache();
        }
                
        public void InsertCustomerTheAspectFWay(string firstName, string lastName, int age,
            Dictionary<string, string> attributes)
        {
            Let.Us
                .Log("Inserting customer the easy way")
                .HowLong("Starting customer insert", "Inserted customer in {1} seconds")
                .Retry()
                .Do(() =>
                    {
                        CustomerData data = new CustomerData();
                        data.Insert(firstName, lastName, age, attributes);
                    });
        }

        public bool InsertCustomerTheOldFashionedWay(string firstName, string lastName, int age,
            Dictionary<string, string> attributes)
        {
            if (string.IsNullOrEmpty(firstName))
                throw new ApplicationException("first name cannot be empty");

            if (string.IsNullOrEmpty(lastName))
                throw new ApplicationException("last name cannot be empty");

            if (age < 0)
                throw new ApplicationException("Age must be non-zero");

            if (null == attributes)
                throw new ApplicationException("Attributes must not be null");

            // Log customer inserts and time the execution
            SampleLogger.Writer.WriteLine("Inserting customer data...");
            DateTime start = DateTime.Now;

            try
            {
                CustomerData data = new CustomerData();
                bool result = data.Insert(firstName, lastName, age, attributes);
                if (result == true)
                {
                    SampleLogger.Writer.Write("Successfully inserted customer data in "
                        + (DateTime.Now - start).TotalSeconds + " seconds");
                }
                return result;
            }
            catch (Exception x)
            {
                // Try once more, may be it was a network blip or some temporary downtime
                try
                {
                    CustomerData data = new CustomerData();
                    bool result = data.Insert(firstName, lastName, age, attributes);
                    if (result == true)
                    {
                        SampleLogger.Writer.Write("Successfully inserted customer data in "
                            + (DateTime.Now - start).TotalSeconds + " seconds");
                    }
                    return result;
                }
                catch
                {
                    // Failed on retry, safe to assume permanent failure.

                    // Log the exceptions produced
                    Exception current = x;
                    int indent = 0;
                    while (current != null)
                    {
                        string message = new string(Enumerable.Repeat('\t', indent).ToArray())
                            + current.Message;
                        Debug.WriteLine(message);
                        SampleLogger.Writer.WriteLine(message);
                        current = current.InnerException;
                        indent++;
                    }
                    Debug.WriteLine(x.StackTrace);
                    SampleLogger.Writer.WriteLine(x.StackTrace);

                    return false;
                }
            }

        }
    }

    public class CustomerData
    {
        public bool Insert(string firstName, string lastName, int age,
            Dictionary<string, string> attributes)
        {
            // Do something
            return true;
        }
    }
}
