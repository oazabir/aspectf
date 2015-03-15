AspectF offers you separation of concern, without the bells and whistles. You can put Aspects into your code without using any third party framework, or DynamicProxy or ContextBoundObject or any other mumbo jumbo. It's plain simple C# code, clever use of Delegate in a fluent manner.

Here's an example of AspectF usage:

```
        public void InsertCustomerTheEasyWay(string firstName, string lastName, int age,
            Dictionary<string, string> attributes)
        {
            AspectF.Define
                .Log(Logger.Writer, "Inserting customer the easy way")
                .HowLong(Logger.Writer, "Starting customer insert", "Inserted customer in {1} seconds")
                .Retry()
                .Do(() =>
                    {
                        CustomerData data = new CustomerData();
                        data.Insert(firstName, lastName, age, attributes);
                    });
        }

```

Learn details about how to create aspects from my blog: http://msmvps.com/omar

Read the CodeProject article to get more details:
[AspectF Fluent way to add Aspects into your code](http://www.codeproject.com/KB/tips/aspectf.aspx)

Get the latest source code from "source" tab.

Download latest releases from "downloads" tab.

## Aspects ##
  * **Log**: Logs before calling code.
  * **Retry**: Retries given code N times, in N delay.
  * **Delay**: Waits N ms before callling code.
  * **MustBeNonDefault**: When all given parameters have non-default value, execute the code
  * **MustBeNonNull**: When all given parameters have non-null value, execute the code
  * **Util**: When a certain condition is true, execute the code
  * **While**: While a certain condition is true, keep executing the code
  * **WhenTrue**: When a certain condition is true, execute the code
  * **HowLong**: Measure how long execution of the code takes and then log it.
  * **TrapLog**: Capture exception and log the exception. Do not throw the exception.
  * **TrapLogThrow**: Capture exception, log it and throw it again.
  * **RunAsync**: Execute the code asynchronously.
  * **Cache**: Cache the result of the code execution so that next call returns result from cache
  * **CacheList**: Cache the whole collection and each item in the collection separately from the code execution so that next call returns the collection from cache and ensure each item is loaded fresh from the cache.
  * **Expected**: When a certain exception is produced, capture it and do nothing. Any other exception, throw it.
  * **Transaction**: Execute the code within System.Transactions.TransactionScope.

[![](http://omaralzabir.com/plea.png)](http://omaralzabir.com/charity)