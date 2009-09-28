using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using Xunit;
using Moq;
using System.Web.UI;
using System.Collections;

namespace OmarALZabir.AspectF
{
    internal class AspectTest
    {
        private Mock<ILogger> MockLoggerForException(params Exception[] exceptions)
        {
            var logger = new Mock<ILogger>();
            Queue<Exception> queue = new Queue<Exception>(exceptions);
                
            logger.Expect(l => l.LogException(It.Is<Exception>(x => x == queue.Dequeue()))).Verifiable();
            return logger;
        }

        [Fact]
        public void TestCache()
        {
            var cacheResolver = new Mock<ICacheResolver>();
            var key = "TestObject.Key";
            var testObject = new TestObject 
            { 
                Age = 27, 
                Name = "Omar AL Zabir", 
                BirthDate = DateTime.Parse("9/5/1982") 
            };

            // Test 1. First attempt to call the cache will return the object as is and store it 
            // in cache.
            cacheResolver.Expect(c => c.Get(It.Is<string>(cacheKey => cacheKey == key)))
                .Returns(default(TestObject)).AtMostOnce().Verifiable();
            cacheResolver.Expect(c => c.Add(
                It.Is<string>(cacheKey => cacheKey == key), 
                It.Is<TestObject>(cacheObject => object.Equals(cacheObject, testObject))))
                    .AtMostOnce().Verifiable();
            
            var result = AspectF.Define.Cache<TestObject>(cacheResolver.Object, key).Return(() => testObject);

            cacheResolver.VerifyAll();
            Assert.Same(testObject, result);

            // Test 2. If object is in cache, it will return the object from cache, not the real object
            var cacheResolver2 = new Mock<ICacheResolver>();
            var cachedObject = new TestObject { Name = "Omar Cached" };
            cacheResolver2.Expect(c => c.Get(It.Is<string>(cacheKey => cacheKey == key)))
                .Returns(cachedObject).AtMostOnce().Verifiable();

            var result2 = AspectF.Define.Cache<TestObject>(cacheResolver2.Object, key).Return(() => testObject);

            Assert.Same(cachedObject, result2);
        }

        [Fact]
        public void TestCacheWithRetryAndLog()
        {
            var cacheResolver = new Mock<ICacheResolver>();
            var key = "TestObject.Key";
            var testObject = new TestObject
            {
                Age = 27,
                Name = "Omar AL Zabir",
                BirthDate = DateTime.Parse("9/5/1982")
            };

            var ex = new ApplicationException("Some Exception");
            var logger = MockLoggerForException(ex);
            logger.Expect(l => l.Log("Log1")).AtMostOnce().Verifiable();

            // Test 1. First attempt to call the cache will return the object as is and store it 
            // in cache.
            cacheResolver.Expect(c => c.Get(It.Is<string>(cacheKey => cacheKey == key)))
                .Returns(default(TestObject)).Verifiable();
            cacheResolver.Expect(c => c.Add(
                It.Is<string>(cacheKey => cacheKey == key),
                It.Is<TestObject>(cacheObject => object.Equals(cacheObject, testObject))))
                    .AtMostOnce();

            bool exceptionThrown = false;
            var result = AspectF.Define
                .Log(logger.Object, "Log1")
                .Retry(logger.Object)
                .Cache<TestObject>(cacheResolver.Object, key)                
                .Return(() => 
                    {
                        if (!exceptionThrown)
                        {
                            exceptionThrown = true;
                            throw ex;
                        }
                        else if (exceptionThrown)
                        {
                            return testObject;
                        }
                        else
                        {
                            Assert.True(false, "AspectF.Retry should not retry twice");
                            return default(TestObject);
                        }
                    });
            
            logger.VerifyAll();
            cacheResolver.VerifyAll();
            Assert.Same(testObject, result);

            // Test 2. If object is in cache, it will return the object from cache, not the real object
            var cacheResolver2 = new Mock<ICacheResolver>();
            var cachedObject = new TestObject { Name = "Omar Cached" };

            exceptionThrown = false;
            cacheResolver2.Expect(c => c.Get(It.Is<string>(cacheKey => cacheKey == key)))
                .Returns(() =>
                    {
                        // Fail ICacheResolver.Get call on first attempt to simulate cache service
                        // unavailability.
                        if (!exceptionThrown)
                        {
                            exceptionThrown = true;
                            throw ex;
                        }
                        else if (exceptionThrown)
                        {
                            return cachedObject;
                        }
                        else
                        {
                            throw new ApplicationException("ICacheResolver.Get should not be called thrice");
                        }
                    }).Verifiable();

            var logger2 = new Mock<ILogger>();

            // When ICacheResolver.Get is called, it will raise an exception on first attempt
            logger2.Expect(l => l.LogException(It.Is<Exception>(x => object.Equals(x.InnerException, ex))))
                .AtMostOnce().Verifiable();
            logger2.Expect(l => l.Log("Log2"))
                .AtMostOnce().Verifiable();

            var result2 = AspectF.Define
                .Log(logger2.Object, "Log2")
                .Retry(logger2.Object)
                .CacheRetry<TestObject>(cacheResolver2.Object, logger2.Object, key)                
                .Return(() => testObject);

            cacheResolver2.VerifyAll();
            logger2.VerifyAll();
            Assert.Same(cachedObject, result2);
        }

        [Fact]
        public void TestCacheList()
        {
            List<TestObject> testObjects = new List<TestObject>();
            TestObject newTestObject1 = new TestObject { Age = 10, BirthDate = DateTime.Parse("1/1/1999"), Name = "User A" };
            testObjects.Add(newTestObject1);
            TestObject newTestObject2 = new TestObject { Age = 11, BirthDate = DateTime.Parse("1/1/1998"), Name = "User B" };
            testObjects.Add(newTestObject2);
            TestObject newTestObject3 = new TestObject { Age = 12, BirthDate = DateTime.Parse("1/1/1997"), Name = "User C" };
            testObjects.Add(newTestObject3);

            string collectionKey = "TestObjectCollectionKey";

            var objectQueue = new Queue(testObjects);
            
            var keyQueue = new Queue<string>(new string[] { 
                "TestObject10", "TestObject11", "TestObject12"});            

            // Scenario 1: Collection is not cached. So, after getting the collection, every
            // object in the collection will be stored in cache using individual item key
            var cacheResolver = new Mock<ICacheResolver>();

            // CacheList will check if the collection exists in the cache
            cacheResolver.Expect(c => c.Get(It.Is<string>(cacheKey => cacheKey == collectionKey)))
                .Returns(default(List<TestObject>)).AtMostOnce().Verifiable();

            // It won't find it in the cache, so it will add the collection in cache
            cacheResolver.Expect(c => c.Add(It.Is<string>(cacheKey => cacheKey == collectionKey),
                It.Is<List<TestObject>>(toCache => object.Equals(toCache, testObjects))))
                .AtMostOnce()
                .Verifiable();                
            
            // Then it will store each item inside the collection one by one
            cacheResolver.Expect(c => 
                c.Set(It.Is<string>(cacheKey => cacheKey == keyQueue.Dequeue()), 
                It.Is<object>(o => object.Equals(o, objectQueue.Dequeue()))))
                .Verifiable();
            
            var collection = AspectF.Define.CacheList<TestObject>(cacheResolver.Object, collectionKey,
                obj => "TestObject" + obj.Age)
                .Return<IEnumerable<TestObject>>(() => testObjects);
            
            Assert.Same(testObjects, collection);
            cacheResolver.VerifyAll();
            Assert.Equal(0, objectQueue.Count);
            Assert.Equal(0, keyQueue.Count);

            // Scenario 2: Collection is cached. So, the collection will be returned from cache

            // Scenario 3: Collection is cached. The collection will be loaded from cache, but
            // each item in the cache will be individually queries from cache. If not found, it 
            // will be loaded from source and then updated in cache. The returned collection will
            // be a new collection, which contains all individual items in the same order, and the
            // individual items will be loaded from source to give a fresh representation.
        }

        [Fact]
        public void TestRetry()
        {
            bool result = false;
            bool exceptionThrown = false;

            var ex = new ApplicationException("Test exception");
            var mockLoggerForException = MockLoggerForException(ex);
                    
            Assert.DoesNotThrow(() =>
                {
                    AspectF.Define.Retry(mockLoggerForException.Object).Do(() =>
                    {
                        if (!exceptionThrown)
                        {
                            exceptionThrown = true;
                            throw ex;
                        }
                        else if (exceptionThrown)
                        {
                            result = true;
                        }
                        else
                        {
                            Assert.True(false, "AspectF.Retry should not retry more than once");
                        }
                    });

                });
            mockLoggerForException.VerifyAll();

            Assert.True(exceptionThrown, "Assert.Retry did not invoke the function at all");
            Assert.True(result, "Assert.Retry did not retry the function after exception was thrown");
        }

        [Fact]
        public void TestRetryWithDuration()
        {
            bool result = false;
            DateTime firstCallAt = DateTime.Now;
            DateTime secondCallAt = DateTime.Now;
            bool exceptionThrown = false;

            var ex = new ApplicationException("Test exception");
            var logger = MockLoggerForException(ex);
            Assert.DoesNotThrow(() =>
                {
                    AspectF.Define.Retry(5000, logger.Object).Do(() =>
                    {
                        if (!exceptionThrown)
                        {
                            firstCallAt = DateTime.Now;
                            exceptionThrown = true;
                            throw ex;
                        }
                        else if (exceptionThrown)
                        {
                            secondCallAt = DateTime.Now;
                            result = true;
                        }
                        else
                        {
                            Assert.True(false, "Aspect.Retry should not retry more than once.");
                        }
                    });
                });
            logger.VerifyAll();

            Assert.True(exceptionThrown, "AspectF.Retry did not invoke the function at all");
            Assert.True(result, "AspectF.Retry did not retry the function after exception was thrown");
            Assert.InRange<Double>((secondCallAt - firstCallAt).TotalSeconds, 4.9d, 5.1d);
        }

        [Fact]
        public void TestRetryWithDurationExceptionHandlerAndFinallyFailing()
        {
            DateTime firstCallAt = DateTime.Now;
            DateTime secondCallAt = DateTime.Now;
            bool exceptionThrown = false;
            bool firstRetry = false;
            bool secondRetry = false;
            bool expectedExceptionFound = false;
            bool allRetryFailed = false;

            var ex1 = new ApplicationException("First exception");
            var ex2 = new ApplicationException("Second exception");
            var ex3 = new ApplicationException("Third exception");

            var logger = MockLoggerForException(ex1, ex2, ex3);
            Assert.DoesNotThrow(() =>
                {
                    AspectF.Define.Retry(5000, 2,
                        x => { expectedExceptionFound = (x == ex1 || x == ex2 || x == ex3); },
                        errors => { allRetryFailed = true; },
                        logger.Object)
                        .Do(() =>
                    {
                        if (!exceptionThrown)
                        {
                            firstCallAt = DateTime.Now;
                            exceptionThrown = true;
                            throw ex1;
                        }
                        else if (!firstRetry)
                        {
                            secondCallAt = DateTime.Now;
                            firstRetry = true;
                            throw ex2;
                        }
                        else if (!secondRetry)
                        {
                            secondRetry = true;
                            throw ex3;
                        }
                        else
                        {
                            Assert.True(false, "Aspect.Retry should not retry more than twice.");
                        }
                    });
                });
            logger.VerifyAll();

            Assert.True(exceptionThrown, "Assert.Retry did not invoke the function at all");
            Assert.True(firstRetry, "Assert.Retry did not retry the function after exception was thrown");
            Assert.True(secondRetry, "Assert.Retry did not retry the function second time after exception was thrown");
            Assert.InRange<Double>((secondCallAt - firstCallAt).TotalSeconds, 4.9d, 5.1d); 
            Assert.True(allRetryFailed, "Assert.Retry did not call the final fail handler");
        }

        [Fact]
        public void TestDelay()
        {
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;

            AspectF.Define.Delay(5000).Do(() => { end = DateTime.Now; });

            TimeSpan delay = end - start;
            Assert.InRange<double>(delay.TotalSeconds, 4.9d, 5.1d);
        }

        [Fact]
        public void TestMustBeNonNullWithValidParameters()
        {
            bool result = false;

            Assert.DoesNotThrow(delegate
            {
                AspectF.Define
                .MustBeNonNull(1, DateTime.Now, string.Empty, "Hello", new object())
                .Do(delegate
                {
                    result = true;
                });
            });

            Assert.True(result, "Assert.MustBeNonNull did not call the function although all parameters were non-null");
        }

        [Fact]
        public void TestMustBeNonNullWithInvalidParameters()
        {
            bool result = false;

            Assert.Throws(typeof(ArgumentException), delegate
            {
                AspectF.Define
                    .MustBeNonNull(1, DateTime.Now, string.Empty, null, "Hello", new object())
                    .Do(() =>
                {
                    result = true;
                });

                Assert.True(result, "Assert.MustBeNonNull must not call the function when there's a null parameter");
            });
            
        }

        [Fact]
        public void TestUntil()
        {
            int counter = 10;
            bool callbackFired = false;

            AspectF.Define
                .Until(() =>
                {
                    counter--;
                    
                    Assert.InRange<int>(counter, 0, 9);

                    return counter == 0;
                })
                .Do(() =>
                {
                    callbackFired = true;
                    Assert.Equal<int>(0, counter);
                });

            Assert.True(callbackFired, "Assert.Until never fired the callback");
        }

        [Fact]
        public void TestWhile()
        {
            int counter = 10;
            bool callbackFired = false;

            AspectF.Define
                .While(() =>
                {
                    counter--;
                    
                    Assert.InRange(counter, 0, 9);

                    return counter > 0;
                })
                .Do(() =>
                {
                    callbackFired = true;
                    Assert.Equal<int>(0, counter);
                });

            Assert.True(callbackFired, "Assert.While never fired the callback");
        }

        [Fact]
        public void TestWhenTrue()
        {
            bool callbackFired = false;
            AspectF.Define.WhenTrue(
                () => 1 == 1,
                () => null == null,
                () => 1 > 0)
                .Do(() => 
                    {
                        callbackFired = true;
                    });

            Assert.True(callbackFired, "Assert.WhenTrue did not fire callback although all conditions were true");

            bool callbackFired2 = false;
            AspectF.Define.WhenTrue(
                () => 1 == 0, // fail
                () => null == null,
                () => 1 > 0)
                .Do(() => 
                    {
                        callbackFired2 = true;
                    });

            Assert.False(callbackFired2, "Assert.WhenTrue did not fire callback although all conditions were true");
        }

        [Fact]
        public void TestLog()
        {
            var categories = new string[] { "Category1", "Category2" };
            var logger1 = new Mock<ILogger>();
            logger1.Expect(l => l.Log(categories, "Test Log 1")).Verifiable();
            // Attempt 1: Test one time logging
            AspectF.Define
                .Log(logger1.Object, categories, "Test Log 1")
                .Do(AspectExtensions.DoNothing);
            logger1.Verify();

            // Attempt 2: Test before and after logging
            var logger2 = new Mock<ILogger>();
            logger2.Expect(l => l.Log(categories, "Before Log")).Verifiable();
            logger2.Expect(l => l.Log(categories, "After Log")).Verifiable();
            AspectF.Define
                .Log(logger2.Object, categories, "Before Log", "After Log")
                .Do(AspectExtensions.DoNothing);
            logger2.VerifyAll();
        }

        [Fact]
        public void TestRetryAndLog()
        {
            // Attempt 1: Test log and Retry together
            bool exceptionThrown = false;
            bool retried = false;
            
            var ex = new ApplicationException("First exception thrown which should be ignored");
            var logger = MockLoggerForException(ex);
            logger.Expect(l => l.Log("TestRetryAndLog"));

            AspectF.Define
                .Log(logger.Object, "TestRetryAndLog")
                .Retry(logger.Object)
                .Do(() => 
            {
                if (!exceptionThrown)
                {
                    exceptionThrown = true;
                    throw ex;
                }
                else
                {
                    retried = true;
                }
            });
            logger.VerifyAll();

            Assert.True(exceptionThrown, "Aspect.Retry did not call the function at all");
            Assert.True(retried, "Aspect.Retry did not retry when exception was thrown first time");
         
            // Attempt 2: Test Log Before and After with Retry together            
            var logger2 = MockLoggerForException(ex);
            logger2.Expect(l => l.Log("BeforeLog"));
            logger2.Expect(l => l.Log("AfterLog"));

            exceptionThrown = false;
            AspectF.Define
                .Log(logger2.Object, "BeforeLog", "AfterLog")
                .Retry(logger2.Object)
                .Do(() =>
                {
                    if (!exceptionThrown)
                    {
                        exceptionThrown = true;
                        throw ex;
                    }
                    else
                    {
                        retried = true;
                    }
                });
            logger2.VerifyAll();
        }

        [Fact]
        public void TestAspectReturn()
        {
            int result = AspectF.Define.Return<int>(() =>
                {
                    return 1;
                });

            Assert.Equal(1, result);
        }

        [Fact]
        public void TestAspectReturnWithOtherAspects()
        {
            var logger = new Mock<ILogger>();
            logger.Expect(l => l.Log("Test Logging")).Verifiable();

            int result = AspectF.Define
                .Log(logger.Object, "Test Logging")
                .Retry(2, new Mock<ILogger>().Object)
                .MustBeNonNull(1, DateTime.Now, string.Empty)
                .Return<int>(() =>
                {
                    return 1;
                });

            logger.VerifyAll();
            Assert.Equal(1, result);
        }

        [Fact]
        public void TestAspectAsync()
        {
            bool callExecutedImmediately = false;
            bool callbackFired = false;
            AspectF.Define.RunAsync().Do(() =>
                {
                    callbackFired = true;
                    Assert.True(callExecutedImmediately, "Aspect.RunAsync Call did not execute asynchronously");
                });
            callExecutedImmediately = true;

            // wait until the async function completes
            while (!callbackFired) Thread.Sleep(100);

            bool callCompleted = false;
            bool callReturnedImmediately = false;
            AspectF.Define.RunAsync(() => Assert.True(callCompleted, "Aspect.RunAsync Callback did not fire after the call has completed properly"))
                .Do(() =>
                    {
                        callCompleted = true;
                        Assert.True(callReturnedImmediately, "Aspect.RunAsync call did not run asynchronously");
                    });
            callReturnedImmediately = true;

            while (!callCompleted) Thread.Sleep(100);
        }

        [Fact]
        public void TestTrapLog()
        {
            var exception = new ApplicationException("Parent Exception",
                                new ApplicationException("Child Exception",
                                    new ApplicationException("Grandchild Exception")));
            var logger = new Mock<ILogger>();
            logger.Expect(l => l.LogException(exception)).Verifiable();

            Assert.DoesNotThrow(() =>
                {
                    AspectF.Define.TrapLog(logger.Object).Do(() =>
                        {
                            throw exception;
                        });
                });
            logger.VerifyAll();            
        }

        [Fact]
        public void TestTrapLogThrow()
        {
            var exception = new ApplicationException("Parent Exception",
                                new ApplicationException("Child Exception",
                                    new ApplicationException("Grandchild Exception")));
            var logger = MockLoggerForException(exception);
            
            Assert.Throws(typeof(ApplicationException), () =>
            {
                AspectF.Define.TrapLogThrow(logger.Object).Do(() =>
                {
                    throw exception;
                });
            });

            logger.VerifyAll();            
        }
    }

    

}
