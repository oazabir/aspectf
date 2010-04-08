using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using System.Web.UI;
using Xunit;

namespace OmarALZabir.AspectF
{
    public class AspectTest
    {
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

        [Fact]
        public void Log_should_call_ILogger_Log_method_with_categories_and_message()
        {
            var categories = new string[] { "Category1", "Category2" };
            var logger1 = new Mock<ILogger>();
            logger1.Expect(l => l.Log(categories, "Test Log 1")).Verifiable();

            AspectF.Define
                .Log(logger1.Object, categories, "Test Log 1")
                .Do(AspectExtensions.DoNothing);

            logger1.Verify();
        }

        [Fact]
        public void Log_should_call_ILogger_Log_method_before_and_after_executing_code()
        {
            var categories = new string[] { "Category1", "Category2" };
            var logger2 = new Mock<ILogger>();
            var loggedBefore = false;
            var loggedAfter = false;

            logger2.Expect(l => l.Log(categories, "Before Log"))
                .Callback(() => loggedBefore = true)
                .AtMostOnce()
                .Verifiable();
            logger2.Expect(l => l.Log(categories, "After Log"))
                .Callback(() => loggedAfter = true)
                .AtMostOnce()
                .Verifiable();

            AspectF.Define
                .Log(logger2.Object, categories, "Before Log", "After Log")
                .Do(() =>
                {
                    Assert.True(loggedBefore);
                    Assert.False(loggedAfter);
                });

            logger2.VerifyAll();
        }

        [Fact]
        public void Retry_will_execute_code_again_if_any_exception_thrown_on_first_invocation()
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
        public void Retry_with_duration_will_retry_code_after_the_duration_when_first_invocation_throws_exception()
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
        public void Retry_will_retry_N_times_when_exception_is_thrown()
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
        public void Delay_will_execute_code_after_given_delay()
        {
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;

            AspectF.Define.Delay(5000).Do(() => { end = DateTime.Now; });

            TimeSpan delay = end - start;
            Assert.InRange<double>(delay.TotalSeconds, 4.9d, 5.1d);
        }

        [Fact]
        public void MustBeNonNull_will_execute_code_only_when_all_parameters_are_non_null()
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
        public void MustBeNonNull_will_throw_exception_when_any_parameter_is_null()
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
        public void Until_will_keep_executing_code_until_the_condition_returns_true()
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
        public void While_will_keep_executing_code_while_the_condition_function_returns_true()
        {
            int counter = 10;
            int called = 0;
            bool callbackFired = false;

            AspectF.Define
                .While(() =>
                {
                    Assert.InRange(counter, 0, 10);

                    return counter-- > 0;
                })
                .Do(() =>
                {
                    callbackFired = true;
                    called++;                    
                });

            Assert.True(callbackFired, "Assert.While never fired the callback");
            Assert.Equal<int>(10, called);
        }

        [Fact]
        public void When_true_will_execute_code_when_all_conditions_are_true()
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
        public void Log_should_log_once_and_retry_should_retry_once()
        {
            bool exceptionThrown = false;
            bool retried = false;
            
            var ex = new ApplicationException("First exception thrown which should be ignored");
            var logger = MockLoggerForException(ex);
            logger.Expect(l => l.Log("TestRetryAndLog")).AtMostOnce().Verifiable();

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
        }

        [Fact]
        public void Log_before_once_and_retry_once_and_after_than_log_after_once()
        {
            bool exceptionThrown = false;
            bool retried = false;
            
            var ex = new ApplicationException("First exception thrown which should be ignored");
            
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
            Assert.True(exceptionThrown, "Aspect.Retry did not call the function at all");
            Assert.True(retried, "Aspect.Retry did not retry when exception was thrown first time");
        }

        [Fact]
        public void Return_should_return_the_value_returned_from_code()
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
        public void First_attempt_to_call_the_cache_will_return_the_object_as_is_and_store_it_in_cache()
        {
            var cacheResolver = new Mock<ICache>();
            var key = "TestObject.Key";
            var testObject = new TestObject
            {
                Age = 27,
                Name = "Omar AL Zabir",
                BirthDate = DateTime.Parse("9/5/1982")
            };

            cacheResolver.Expect(c => c.Get(It.Is<string>(cacheKey => cacheKey == key)))
                .Returns(default(TestObject)).AtMostOnce().Verifiable();
            cacheResolver.Expect(c => c.Add(
                It.Is<string>(cacheKey => cacheKey == key),
                It.Is<TestObject>(cacheObject => object.Equals(cacheObject, testObject))))
                    .AtMostOnce().Verifiable();

            var result = AspectF.Define.Cache<TestObject>(cacheResolver.Object, key).Return(() => testObject);

            cacheResolver.VerifyAll();
            Assert.Same(testObject, result);
        }

        [Fact]
        public void If_object_is_in_cache_it_will_return_the_object_from_cache_not_the_real_object()
        {
            var key = "TestObject.Key";
            var cacheResolver = new Mock<ICache>();
            var cachedObject = new TestObject { Name = "Omar Cached" };
            var testObject = new TestObject
            {
                Age = 27,
                Name = "Omar AL Zabir",
                BirthDate = DateTime.Parse("9/5/1982")
            };

            cacheResolver.Expect(c => c.Get(It.Is<string>(cacheKey => cacheKey == key)))
                .Returns(cachedObject).AtMostOnce().Verifiable();

            var result2 = AspectF.Define.Cache<TestObject>(cacheResolver.Object, key).Return(() => testObject);

            Assert.Same(cachedObject, result2);
        }

        [Fact]
        public void Cache_will_fail_if_loading_from_source_throws_exception_and_retry_will_retry_the_cache_operation()
        {
            var cacheResolver = new Mock<ICache>();
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
        }

        [Fact]
        public void When_cache_fails_to_get_from_cache_retry_will_retry_the_operation()
        {
            // Test 2. If object is in cache, it will return the object from cache, not the real object
            var cacheResolver = new Mock<ICache>();
            var cachedObject = new TestObject { Name = "Omar Cached" };
            var key = "TestObject.Key";
            var testObject = new TestObject
            {
                Age = 27,
                Name = "Omar AL Zabir",
                BirthDate = DateTime.Parse("9/5/1982")
            };

            var ex = new ApplicationException("Some Exception");

            bool exceptionThrown = false;
            cacheResolver.Expect(c => c.Get(It.Is<string>(cacheKey => cacheKey == key)))
                .Returns(() =>
                {
                    // Fail ICache.Get call on first attempt to simulate cache service
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
                        throw new ApplicationException("ICache.Get should not be called thrice");
                    }
                }).Verifiable();

            var logger2 = new Mock<ILogger>();

            // When ICache.Get is called, it will raise an exception on first attempt
            logger2.Expect(l => l.LogException(It.Is<Exception>(x => object.Equals(x.InnerException, ex))))
                .AtMostOnce().Verifiable();
            logger2.Expect(l => l.Log("Log2"))
                .AtMostOnce().Verifiable();

            var result2 = AspectF.Define
                .Log(logger2.Object, "Log2")
                .Retry(logger2.Object)
                .CacheRetry<TestObject>(cacheResolver.Object, logger2.Object, key)
                .Return(() => testObject);

            cacheResolver.VerifyAll();
            logger2.VerifyAll();
            Assert.Same(cachedObject, result2);
        }

        [Fact]
        public void When_Collection_not_cached_after_getting_the_collection_every_object_in_collection_will_be_stored_in_cache_individually()
        {
            List<TestObject> testObjects = new List<TestObject>();
            TestObject newTestObject1 = new TestObject { Age = 10, BirthDate = DateTime.Parse("1/1/1999"), Name = "User A" };
            testObjects.Add(newTestObject1);
            TestObject newTestObject2 = new TestObject { Age = 11, BirthDate = DateTime.Parse("1/1/1998"), Name = "User B" };
            testObjects.Add(newTestObject2);
            TestObject newTestObject3 = new TestObject { Age = 12, BirthDate = DateTime.Parse("1/1/1997"), Name = "User C" };
            testObjects.Add(newTestObject3);

            string collectionKey = "TestObjectCollectionKey";

            var cacheResolver = new Mock<ICache>();
            var objectQueue = new Queue(testObjects);
            var keyQueue = new Queue<string>(new string[] { "TestObject10", "TestObject11", "TestObject12" });

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

            var collection = AspectF.Define.CacheList<TestObject, List<TestObject>>(cacheResolver.Object, collectionKey,
                obj => "TestObject" + obj.Age)
                .Return<List<TestObject>>(() => testObjects);

            Assert.Same(testObjects, collection);
            cacheResolver.VerifyAll();
            Assert.Equal(0, objectQueue.Count);
            Assert.Equal(0, keyQueue.Count);
        }

        [Fact]
        public void When_Collection_is_cached_each_item_in_cached_collection_will_be_loaded_individually_from_cache()
        {
            List<TestObject> testObjects = new List<TestObject>();
            TestObject newTestObject1 = new TestObject { Age = 10, BirthDate = DateTime.Parse("1/1/1999"), Name = "User A" };
            testObjects.Add(newTestObject1);
            TestObject newTestObject2 = new TestObject { Age = 11, BirthDate = DateTime.Parse("1/1/1998"), Name = "User B" };
            testObjects.Add(newTestObject2);
            TestObject newTestObject3 = new TestObject { Age = 12, BirthDate = DateTime.Parse("1/1/1997"), Name = "User C" };
            testObjects.Add(newTestObject3);

            var cacheResolver = new Mock<ICache>();

            string collectionKey = "TestObjectCollectionKey";

            Dictionary<string, object> map = new Dictionary<string, object>();
            map.Add(collectionKey, testObjects);
            map.Add("TestObject10", newTestObject1);
            map.Add("TestObject11", newTestObject2);
            map.Add("TestObject12", newTestObject3);

            // CacheList will check if the collection exists in the cache
            // It finds in the cache, then it will query individual objects from cache
            // Let's assume all cache calls return cached object
            cacheResolver.Expect(c => c.Get(It.IsAny<string>()))
                .Returns<string>(key => map[key])
                .Verifiable();

            var collection = AspectF.Define.CacheList<TestObject, List<TestObject>>(cacheResolver.Object, collectionKey,
                obj => "TestObject" + obj.Age)
                .Return<List<TestObject>>(() =>
                {
                    Assert.True(false, "Item should be in cache and must not be fetched from source.");
                    return default(List<TestObject>);
                });

            // Returned collection is different. It's newly constructed from all the individual
            // items in the cache
            Assert.NotSame(collection, testObjects);

            // Every item in original collection should match with the newly returned collection
            for (int i = 0; i < testObjects.Count; i++)
                Assert.Same(testObjects[i], collection[i]);

            cacheResolver.VerifyAll();
        }

        [Fact]
        public void While_loading_collection_if_any_individual_item_is_not_in_cache_whole_collection_will_be_loaded_from_source()
        {
            List<TestObject> testObjects = new List<TestObject>();
            TestObject newTestObject1 = new TestObject { Age = 10, BirthDate = DateTime.Parse("1/1/1999"), Name = "User A" };
            testObjects.Add(newTestObject1);
            TestObject newTestObject2 = new TestObject { Age = 11, BirthDate = DateTime.Parse("1/1/1998"), Name = "User B" };
            testObjects.Add(newTestObject2);
            TestObject newTestObject3 = new TestObject { Age = 12, BirthDate = DateTime.Parse("1/1/1997"), Name = "User C" };
            testObjects.Add(newTestObject3);

            var cacheResolver = new Mock<ICache>();

            string collectionKey = "TestObjectCollectionKey";

            Dictionary<string, object> map = new Dictionary<string, object>();
            map.Add(collectionKey, testObjects);
            map.Add("TestObject10", newTestObject1);
            map.Add("TestObject11", null);  // Make one item missing from cache
            map.Add("TestObject12", newTestObject3);

            // CacheList will check if the collection exists in the cache
            // It finds in the cache, then it will query individual objects from cache
            // Let's assume all cache calls return cached object
            cacheResolver.Expect(c => c.Get(It.IsAny<string>()))
                .Returns<string>(key => map[key])
                .Verifiable();

            var objectQueue = new Queue(testObjects);
            var keyQueue = new Queue<string>(new string[] { "TestObject10", "TestObject11", "TestObject12" });

            // Collection will be reloaded from source and added to the cache again.
            cacheResolver.Expect(c => c.Add(It.Is<string>(cacheKey => cacheKey == collectionKey),
                It.Is<List<TestObject>>(toCache => object.Equals(toCache, testObjects))))
                .AtMostOnce()
                .Verifiable();

            bool isCollectionLoadedFromSource = false;
            var collection = AspectF.Define.CacheList<TestObject, List<TestObject>>(cacheResolver.Object, collectionKey,
                obj => "TestObject" + obj.Age)
                .Return<List<TestObject>>(() =>
                {
                    isCollectionLoadedFromSource = true;
                    return testObjects;
                });

            // Ensure the collection was really loaded from source
            Assert.True(isCollectionLoadedFromSource);

            // The returned collection si same as the one source provides
            Assert.Same(collection, testObjects);

            cacheResolver.VerifyAll();
        }


        [Fact]
        public void When_an_operation_inside_transaction_fails_it_would_rollback_the_tansaction()
        {
            using (var scope = new TransactionScope())
            {
                AspectF.Define
                    .Expected<ApplicationException>()
                    .Transaction()
                    .Do(() =>
                        {
                            throw new ApplicationException("Fail the transaction");
                        });

                Assert.Equal(TransactionStatus.Aborted, 
                    Transaction.Current.TransactionInformation.Status);
            }
        }

        [Fact]
        public void When_an_operation_inside_transaction_succeeds_it_would_commit_the_transaction()
        {
            using (var scope = new TransactionScope())
            {
                AspectF.Define
                    .Transaction()
                    .Do(() =>
                    {
                        // Do nothing
                    });

                Assert.Equal(TransactionStatus.Active, 
                    Transaction.Current.TransactionInformation.Status);
                scope.Complete();
            }
        }

        private Mock<ILogger> MockLoggerForException(params Exception[] exceptions)
        {
            var logger = new Mock<ILogger>();
            Queue<Exception> queue = new Queue<Exception>(exceptions);

            logger.Expect(l => l.LogException(It.Is<Exception>(x => x == queue.Dequeue()))).Verifiable();
            return logger;
        }

        [Fact]
        public void Use_Should_ReflectTheChangesMadeInsideTheScope()
        {
            var span = new LiteralControl();
            var visibility = false;
            var contents = "<b>AspectF</b> rocks";

            AspectF.Define
            .Use<LiteralControl>(span, control =>
            {
                span.Visible = visibility;
                span.Text = contents;
            });

            Assert.Equal<bool>(span.Visible, visibility);
            Assert.Equal<string>(span.Text, contents);
        }
    }
}
