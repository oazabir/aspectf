using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Transactions;
using Xunit;

namespace OmarALZabir.AspectF.Test
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
            logger.Setup(l => l.LogException(exception)).Verifiable();

            Let.Logger = () => logger.Object;
            Assert.DoesNotThrow(() => Let.Us.TrapLog().Do(() =>
                                                                           {
                                                                               throw exception;
                                                                           }));
            logger.VerifyAll();
        }

        [Fact]
        public void TestTrapLogThrow()
        {
            var exception = new ApplicationException("Parent Exception",
                                new ApplicationException("Child Exception",
                                    new ApplicationException("Grandchild Exception")));
            var logger = MockLoggerForException(exception);

            Let.Logger = () => logger.Object;

            Assert.Throws(typeof(ApplicationException), () => Let.Us.TrapLogThrow().Do(() =>
                                                                                           {
                                                                                               throw exception;
                                                                                           }));

            logger.VerifyAll();
        }

        [Fact]
        public void Log_should_call_ILogger_Log_method_with_categories_and_message()
        {
            var categories = new string[] { "Category1", "Category2" };
            var logger1 = new Mock<ILogger>();
            logger1.Setup(l => l.Log(categories, "Test Log 1")).Verifiable();

            Let.Logger = () => logger1.Object;

            Let.Us
                .Log(categories, "Test Log 1")
                .Do(AspectExtensions.DoNothing);

            logger1.Verify();
        }

        [Fact]
        public void Log_should_call_ILogger_Log_method_before_and_after_executing_code()
        {
            var categories = new[] { "Category1", "Category2" };
            var logger2 = new Mock<ILogger>();
            var loggedBefore = false;
            var loggedAfter = false;

            logger2.Setup(l => l.Log(categories, "Before Log"))
                .Callback(() => loggedBefore = true)
                .AtMostOnce()
                .Verifiable();
            logger2.Setup(l => l.Log(categories, "After Log"))
                .Callback(() => loggedAfter = true)
                .AtMostOnce()
                .Verifiable();

            Let.Logger = () => logger2.Object;

            Let.Us
                .Log(categories, "Before Log", "After Log")
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

            Let.Logger = () => mockLoggerForException.Object;
            Assert.DoesNotThrow(() => Let.Us.Retry().Do(() =>
                                                            {
                                                                if (!exceptionThrown)
                                                                {
                                                                    exceptionThrown = true;
                                                                    throw ex;
                                                                }
                                                                if (exceptionThrown)
                                                                    result = true;
                                                                else
                                                                    Assert.True(false,
                                                                                "AspectF.Retry should not retry more than once");
                                                            }));
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
            Let.Logger = () => logger.Object;
            Assert.DoesNotThrow(() => Let.Us.Retry(5000).Do(() =>
                                                                {
                                                                    if (!exceptionThrown)
                                                                    {
                                                                        firstCallAt = DateTime.Now;
                                                                        exceptionThrown = true;
                                                                        throw ex;
                                                                    }
                                                                    if (exceptionThrown)
                                                                    {
                                                                        secondCallAt = DateTime.Now;
                                                                        result = true;
                                                                    }
                                                                    else
                                                                        Assert.True(false,
                                                                                    "Aspect.Retry should not retry more than once.");
                                                                }));
            logger.VerifyAll();

            Assert.True(exceptionThrown, "AspectF.Retry did not invoke the function at all");
            Assert.True(result, "AspectF.Retry did not retry the function after exception was thrown");
            Assert.InRange((secondCallAt - firstCallAt).TotalSeconds, 4.9d, 5.1d);
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
            Let.Logger = () => logger.Object;
            Assert.DoesNotThrow(() => Let.Us.Retry(5000, 2,
                                                   x => { expectedExceptionFound = (x == ex1 || x == ex2 || x == ex3); },
                                                   errors => { allRetryFailed = true; })
                                          .Do(() =>
                                                  {
                                                      if (!exceptionThrown)
                                                      {
                                                          firstCallAt = DateTime.Now;
                                                          exceptionThrown = true;
                                                          throw ex1;
                                                      }
                                                      if (!firstRetry)
                                                      {
                                                          secondCallAt = DateTime.Now;
                                                          firstRetry = true;
                                                          throw ex2;
                                                      }
                                                      if (!secondRetry)
                                                      {
                                                          secondRetry = true;
                                                          throw ex3;
                                                      }
                                                      Assert.True(false,
                                                                  "Aspect.Retry should not retry more than twice.");
                                                  }));
            logger.VerifyAll();

            Assert.True(exceptionThrown, "Assert.Retry did not invoke the function at all");
            Assert.True(firstRetry, "Assert.Retry did not retry the function after exception was thrown");
            Assert.True(secondRetry, "Assert.Retry did not retry the function second time after exception was thrown");
            Assert.InRange((secondCallAt - firstCallAt).TotalSeconds, 4.9d, 5.1d);
            Assert.True(allRetryFailed, "Assert.Retry did not call the final fail handler");
        }

        [Fact]
        public void Delay_will_execute_code_after_given_delay()
        {
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;

            Let.Us.Delay(5000).Do(() => { end = DateTime.Now; });

            TimeSpan delay = end - start;
            Assert.InRange(delay.TotalSeconds, 4.9d, 5.1d);
        }

        [Fact]
        public void MustBeNonNull_will_execute_code_only_when_all_parameters_are_non_null()
        {
            bool result = false;

            Assert.DoesNotThrow(() => Let.Us
                                          .MustBeNonNull(1, DateTime.Now, string.Empty, "Hello", new object())
                                          .Do(() => result = true));

            Assert.True(result, "Assert.MustBeNonNull did not call the function although all parameters were non-null");
        }

        [Fact]
        public void MustBeNonNull_will_throw_exception_when_any_parameter_is_null()
        {
            bool result = false;

            Assert.Throws(typeof(ArgumentException), delegate
            {
                Let.Us
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

            Let.Us
                .Until(() =>
                {
                    counter--;

                    Assert.InRange(counter, 0, 9);

                    return counter == 0;
                })
                .Do(() =>
                {
                    callbackFired = true;
                    Assert.Equal(0, counter);
                });

            Assert.True(callbackFired, "Assert.Until never fired the callback");
        }

        [Fact]
        public void While_will_keep_executing_code_while_the_condition_function_returns_true()
        {
            int counter = 10;
            int called = 0;
            bool callbackFired = false;

            Let.Us
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
            Assert.Equal(10, called);
        }

        [Fact]
        public void When_true_will_execute_code_when_all_conditions_are_true()
        {
            bool callbackFired = false;
            Let.Us.WhenTrue(
                () => 1 == 1,
                () => null == null,
                () => 1 > 0)
                .Do(() =>
                    {
                        callbackFired = true;
                    });

            Assert.True(callbackFired, "Assert.WhenTrue did not fire callback although all conditions were true");

            bool callbackFired2 = false;
            Let.Us.WhenTrue(
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
            logger.Setup(l => l.Log("TestRetryAndLog")).AtMostOnce().Verifiable();

            Let.Logger = () => logger.Object;
            Let.Us
                .Log("TestRetryAndLog")
                .Retry()
                .Do(() =>
                        {
                            if (!exceptionThrown)
                            {
                                exceptionThrown = true;
                                throw ex;
                            }
                            retried = true;
                        });
            logger.VerifyAll();

            Assert.True(exceptionThrown, "Aspect.Retry did not call the function at all");
            Assert.True(retried, "Aspect.Retry did not retry when exception was thrown first time");
        }

        [Fact]
        public void Log_before_once_and_retry_once_and_after_than_log_after_once()
        {
            bool exceptionThrown;
            bool retried = false;

            var ex = new ApplicationException("First exception thrown which should be ignored");

            var logger2 = MockLoggerForException(ex);
            logger2.Setup(l => l.Log("BeforeLog"));
            logger2.Setup(l => l.Log("AfterLog"));

            exceptionThrown = false;
            Let.Logger = () => logger2.Object;
            Let.Us
                .Log("BeforeLog", "AfterLog")
                .Retry()
                .Do(() =>
                        {
                            if (!exceptionThrown)
                            {
                                exceptionThrown = true;
                                throw ex;
                            }
                            retried = true;
                        });
            logger2.VerifyAll();
            Assert.True(exceptionThrown, "Aspect.Retry did not call the function at all");
            Assert.True(retried, "Aspect.Retry did not retry when exception was thrown first time");
        }

        [Fact]
        public void Return_should_return_the_value_returned_from_code()
        {
            int result = Let.Us.Return(() => 1);

            Assert.Equal(1, result);
        }


        [Fact]
        public void TestAspectReturnWithOtherAspects()
        {
            var logger = new Mock<ILogger>();
            logger.Setup(l => l.Log("Test Logging")).Verifiable();

            Let.Logger = () => logger.Object;
            int result = Let.Us
                .Log("Test Logging")
                .Retry(2)
                .MustBeNonNull(1, DateTime.Now, string.Empty)
                .Return(() => 1);

            logger.VerifyAll();
            Assert.Equal(1, result);
        }

        [Fact]
        public void TestAspectAsync()
        {
            bool callExecutedImmediately = false;
            bool callbackFired = false;
            Let.Us.RunAsync().Do(() =>
                {
                    callbackFired = true;
                    Assert.True(callExecutedImmediately, "Aspect.RunAsync Call did not execute asynchronously");
                });
            callExecutedImmediately = true;

            // wait until the async function completes
            while (!callbackFired) Thread.Sleep(100);

            bool callCompleted = false;
            bool callReturnedImmediately = false;
            Let.Us.RunAsync(() => Assert.True(callCompleted, "Aspect.RunAsync Callback did not fire after the call has completed properly"))
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
            const string key = "TestObject.Key";
            var testObject = new TestObject
            {
                Age = 27,
                Name = "Omar AL Zabir",
                BirthDate = DateTime.Parse("9/5/1982")
            };

            cacheResolver.Setup(c => c.Get(It.Is<string>(cacheKey => cacheKey == key)))
                .Returns(default(TestObject)).AtMostOnce().Verifiable();
            cacheResolver.Setup(c => c.Add(
                It.Is<string>(cacheKey => cacheKey == key),
                It.Is<TestObject>(cacheObject => Equals(cacheObject, testObject))))
                    .AtMostOnce().Verifiable();

            Let.Cache = () => cacheResolver.Object;
            var result = Let.Us.Cache<TestObject>(key).Return(() => testObject);

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

            cacheResolver.Setup(c => c.Get(It.Is<string>(cacheKey => cacheKey == key)))
                .Returns(cachedObject).AtMostOnce().Verifiable();

            Let.Cache = () => cacheResolver.Object;

            var result2 = Let.Us.Cache<TestObject>(key).Return(() => testObject);

            Assert.Same(cachedObject, result2);
        }

        [Fact]
        public void Cache_will_fail_if_loading_from_source_throws_exception_and_retry_will_retry_the_cache_operation()
        {
            var cacheResolver = new Mock<ICache>();
            const string key = "TestObject.Key";
            var testObject = new TestObject
            {
                Age = 27,
                Name = "Omar AL Zabir",
                BirthDate = DateTime.Parse("9/5/1982")
            };

            var ex = new ApplicationException("Some Exception");
            var logger = MockLoggerForException(ex);
            logger.Setup(l => l.Log("Log1")).AtMostOnce().Verifiable();

            cacheResolver.Setup(c => c.Get(It.Is<string>(cacheKey => cacheKey == key)))
                .Returns(default(TestObject)).Verifiable();
            cacheResolver.Setup(c => c.Add(
                It.Is<string>(cacheKey => cacheKey == key),
                It.Is<TestObject>(cacheObject => Equals(cacheObject, testObject))))
                    .AtMostOnce();

            bool exceptionThrown = false;
            Let.Logger = () => logger.Object;
            Let.Cache = () => cacheResolver.Object;
            var result = Let.Us
                .Log("Log1")
                .Retry()
                .Cache<TestObject>(key)
                .Return(() =>
                            {
                                if (!exceptionThrown)
                                {
                                    exceptionThrown = true;
                                    throw ex;
                                }
                                if (exceptionThrown)
                                {
                                    return testObject;
                                }
                                Assert.True(false, "AspectF.Retry should not retry twice");
                                return default(TestObject);
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
            const string key = "TestObject.Key";
            var testObject = new TestObject
            {
                Age = 27,
                Name = "Omar AL Zabir",
                BirthDate = DateTime.Parse("9/5/1982")
            };

            var ex = new ApplicationException("Some Exception");

            bool exceptionThrown = false;
            cacheResolver.Setup(c => c.Get(It.Is<string>(cacheKey => cacheKey == key)))
                .Returns(() =>
                             {
                                 // Fail ICache.Get call on first attempt to simulate cache service
                                 // unavailability.
                                 if (!exceptionThrown)
                                 {
                                     exceptionThrown = true;
                                     throw ex;
                                 }
                                 if (exceptionThrown)
                                 {
                                     return cachedObject;
                                 }
                                 throw new ApplicationException("ICache.Get should not be called thrice");
                             }).Verifiable();

            var logger2 = new Mock<ILogger>();

            // When ICache.Get is called, it will raise an exception on first attempt
            logger2.Setup(l => l.LogException(It.Is<Exception>(x => Equals(x, ex))))
                .AtMostOnce().Verifiable();
            logger2.Setup(l => l.Log("Log2"))
                .AtMostOnce().Verifiable();

            Let.Logger = () => logger2.Object;
            Let.Cache = () => cacheResolver.Object;
            var result2 = Let.Us
                .Log("Log2")
                .Retry()
                .CacheRetry<TestObject>(key)
                .Return(() => testObject);

            cacheResolver.VerifyAll();
            logger2.VerifyAll();
            Assert.Same(cachedObject, result2);
        }

        [Fact]
        public void When_Collection_not_cached_after_getting_the_collection_every_object_in_collection_will_be_stored_in_cache_individually()
        {
            var testObjects = new List<TestObject>();
            var newTestObject1 = new TestObject { Age = 10, BirthDate = DateTime.Parse("1/1/1999"), Name = "User A" };
            testObjects.Add(newTestObject1);
            var newTestObject2 = new TestObject { Age = 11, BirthDate = DateTime.Parse("1/1/1998"), Name = "User B" };
            testObjects.Add(newTestObject2);
            var newTestObject3 = new TestObject { Age = 12, BirthDate = DateTime.Parse("1/1/1997"), Name = "User C" };
            testObjects.Add(newTestObject3);

            const string collectionKey = "TestObjectCollectionKey";

            var cacheResolver = new Mock<ICache>();
            var objectQueue = new Queue(testObjects);
            var keyQueue = new Queue<string>(new[] { "TestObject10", "TestObject11", "TestObject12" });

            // CacheList will check if the collection exists in the cache
            cacheResolver.Setup(c => c.Get(It.Is<string>(cacheKey => cacheKey == collectionKey)))
                .Returns(default(List<TestObject>)).AtMostOnce().Verifiable();

            // It won't find it in the cache, so it will add the collection in cache
            cacheResolver.Setup(c => c.Add(It.Is<string>(cacheKey => cacheKey == collectionKey),
                It.Is<List<TestObject>>(toCache => Equals(toCache, testObjects))))
                .AtMostOnce()
                .Verifiable();

            // Then it will store each item inside the collection one by one
            cacheResolver.Setup(c =>
                                c.Set(It.Is<string>(cacheKey => cacheKey == keyQueue.Peek()),
                                      It.Is<object>(o => Equals(o, objectQueue.Peek())))).Callback(() =>
                                                                                                              {
                                                                                                                  objectQueue.Dequeue();
                                                                                                                  keyQueue.Dequeue();
                                                                                                              }).Verifiable();
            Let.Cache = () => cacheResolver.Object;
            var collection = Let.Us.CacheList<TestObject, List<TestObject>>(collectionKey,
                obj => string.Format("TestObject{0}", obj.Age))
                .Return(() => testObjects);

            Assert.Same(testObjects, collection);
            cacheResolver.VerifyAll();
            Assert.Equal(0, objectQueue.Count);
            Assert.Equal(0, keyQueue.Count);
        }

        [Fact]
        public void When_Collection_is_cached_each_item_in_cached_collection_will_be_loaded_individually_from_cache()
        {
            var testObjects = new List<TestObject>();
            var newTestObject1 = new TestObject { Age = 10, BirthDate = DateTime.Parse("1/1/1999"), Name = "User A" };
            testObjects.Add(newTestObject1);
            var newTestObject2 = new TestObject { Age = 11, BirthDate = DateTime.Parse("1/1/1998"), Name = "User B" };
            testObjects.Add(newTestObject2);
            var newTestObject3 = new TestObject { Age = 12, BirthDate = DateTime.Parse("1/1/1997"), Name = "User C" };
            testObjects.Add(newTestObject3);

            var cacheResolver = new Mock<ICache>();

            const string collectionKey = "TestObjectCollectionKey";

            var map = new Dictionary<string, object>
                          {
                              {collectionKey, testObjects},
                              {"TestObject10", newTestObject1},
                              {"TestObject11", newTestObject2},
                              {"TestObject12", newTestObject3}
                          };

            // CacheList will check if the collection exists in the cache
            // It finds in the cache, then it will query individual objects from cache
            // Let's assume all cache calls return cached object
            cacheResolver.Setup(c => c.Get(It.IsAny<string>()))
                .Returns<string>(key => map[key])
                .Verifiable();

            Let.Cache = () => cacheResolver.Object;
            var collection = Let.Us.CacheList<TestObject, List<TestObject>>(collectionKey,
                obj => string.Format("TestObject{0}", obj.Age))
                .Return(() =>
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
            var testObjects = new List<TestObject>();
            var newTestObject1 = new TestObject { Age = 10, BirthDate = DateTime.Parse("1/1/1999"), Name = "User A" };
            testObjects.Add(newTestObject1);
            var newTestObject2 = new TestObject { Age = 11, BirthDate = DateTime.Parse("1/1/1998"), Name = "User B" };
            testObjects.Add(newTestObject2);
            var newTestObject3 = new TestObject { Age = 12, BirthDate = DateTime.Parse("1/1/1997"), Name = "User C" };
            testObjects.Add(newTestObject3);

            var cacheResolver = new Mock<ICache>();

            const string collectionKey = "TestObjectCollectionKey";

            var map = new Dictionary<string, object>
                          {
                              {collectionKey, testObjects}, 
                              {"TestObject10", newTestObject1}, 
                              {"TestObject11", null}, 
                              {"TestObject12", newTestObject3}
                          };

            // CacheList will check if the collection exists in the cache
            // It finds in the cache, then it will query individual objects from cache
            // Let's assume all cache calls return cached object
            cacheResolver.Setup(c => c.Get(It.IsAny<string>()))
                .Returns<string>(key => map[key])
                .Verifiable();

            // Collection will be reloaded from source and added to the cache again.
            cacheResolver.Setup(c => c.Add(It.Is<string>(cacheKey => cacheKey == collectionKey),
                It.Is<List<TestObject>>(toCache => Equals(toCache, testObjects))))
                .AtMostOnce()
                .Verifiable();

            var isCollectionLoadedFromSource = false;
            Let.Cache = () => cacheResolver.Object;
            var collection = Let.Us.CacheList<TestObject, List<TestObject>>(collectionKey,
                obj => "TestObject" + obj.Age)
                .Return(() =>
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
            using (new TransactionScope())
            {
                Let.Us
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
                Let.Us
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

        private static Mock<ILogger> MockLoggerForException(params Exception[] exceptions)
        {
            var logger = new Mock<ILogger>();
            var queue = new Queue<Exception>(exceptions);

            logger.Setup(l => l.LogException(It.Is<Exception>(x => x == queue.Peek()))).Callback(() => queue.Dequeue()).Verifiable();
            return logger;
        }

        [Fact]
        public void Use_Should_ReflectTheChangesMadeInsideTheScope()
        {
            var textBox = new TestObject();
            const string name = "AspectF rocks!";

            Let.Us
            .Use(textBox, c =>
            {
                c.Age = 15;
                c.BirthDate = new DateTime(1995, 1, 1);
                c.Name = name;
            });

            Assert.Equal(textBox.Age, 15);
            Assert.Equal(textBox.BirthDate, new DateTime(1995, 1, 1));
            Assert.Equal(textBox.Name, name);
        }

        [Fact]
        public void Test_MockLoggerForExceptionMethod()
        {
            var exception1 = new Exception("exception 1");
            var exception2 = new Exception("exception 2");
            var exception3 = new Exception("exception 3");
            var exception4 = new Exception("exception 4");
            Mock<ILogger> mockLoggerForException = MockLoggerForException(exception1, exception2, exception3, exception4);
            mockLoggerForException.Object.LogException(exception1);
            mockLoggerForException.Object.LogException(exception2);
            mockLoggerForException.Object.LogException(exception3);
            mockLoggerForException.Object.LogException(exception4);
            mockLoggerForException.VerifyAll();
        }
    }
}
