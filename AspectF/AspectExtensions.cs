using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;

namespace OmarALZabir.AspectF
{
    public static class AspectExtensions
    {
        [DebuggerStepThrough]
        public static void DoNothing()
        {
        }

        [DebuggerStepThrough]
        public static void DoNothing(params object[] whatever)
        {
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, ILogger logger)
        {
            return aspects.Combine(work =>
                                   Retry(1000, 1, error => DoNothing(error), x => DoNothing(), work, logger));
        }


        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects)
        {
            return Retry(aspects, Let.Logger());
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, Action<IEnumerable<Exception>> failHandler, ILogger logger)
        {
            return aspects.Combine(work =>
                                   Retry(1000, 1, error => DoNothing(error), x => DoNothing(), work, logger));
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, Action<IEnumerable<Exception>> failHandler)
        {
            return Retry(aspects, failHandler, Let.Logger());
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, int retryDuration, ILogger logger)
        {
            return aspects.Combine(work =>
                                   Retry(retryDuration, 1, error => DoNothing(error), x => DoNothing(), work, logger));
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, int retryDuration)
        {
            return Retry(aspects, retryDuration, Let.Logger());
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, int retryDuration,
                                    Action<Exception> errorHandler, ILogger logger)
        {
            return aspects.Combine(work =>
                                   Retry(retryDuration, 1, errorHandler, x => DoNothing(), work, logger));
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, int retryDuration,
                                    Action<Exception> errorHandler)
        {
            return Retry(aspects, retryDuration, errorHandler, Let.Logger());
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, int retryDuration,
                                    int retryCount, Action<Exception> errorHandler, ILogger logger)
        {
            return aspects.Combine(work =>
                                   Retry(retryDuration, retryCount, errorHandler, x => DoNothing(), work, logger));
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, int retryDuration,
                                    int retryCount, Action<Exception> errorHandler)
        {
            return Retry(aspects, retryDuration, retryCount, errorHandler, Let.Logger());
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, int retryDuration,
                                    int retryCount, Action<Exception> errorHandler, Action<IEnumerable<Exception>> retryFailed, ILogger logger)
        {
            return aspects.Combine(work =>
                                   Retry(retryDuration, retryCount, errorHandler, retryFailed, work, logger));
        }

        [DebuggerStepThrough]
        public static AspectF Retry(this AspectF aspects, int retryDuration,
                                    int retryCount, Action<Exception> errorHandler, Action<IEnumerable<Exception>> retryFailed)
        {
            return Retry(aspects, retryDuration, retryCount, errorHandler, retryFailed, Let.Logger());
        }

        [DebuggerStepThrough]
        public static void Retry(int retryDuration, int retryCount,
                                 Action<Exception> errorHandler, Action<IEnumerable<Exception>> retryFailed, Action work, ILogger logger)
        {
            List<Exception> errors = null;
            do
            {
                try
                {
                    work();
                    return;
                }
                catch (Exception x)
                {
                    if (null == errors)
                        errors = new List<Exception>();
                    errors.Add(x);
                    logger.LogException(x);
                    errorHandler(x);
                    System.Threading.Thread.Sleep(retryDuration);
                }
            } while (retryCount-- > 0);
            retryFailed(errors);
        }

        [DebuggerStepThrough]
        public static AspectF Delay(this AspectF aspect, int milliseconds)
        {
            return aspect.Combine(work =>
                                      {
                                          System.Threading.Thread.Sleep(milliseconds);
                                          work();
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF MustBeNonDefault<T>(this AspectF aspect, params T[] args)
            where T : IComparable
        {
            return aspect.Combine(work =>
                                      {
                                          for (var i = 0; i < args.Length; i++)
                                          {
                                              T arg = args[i];
                                              if (arg == null || arg.Equals(default(T)))
                                                  throw new ArgumentException(
                                                      string.Format("Parameter at index {0} is null", i));
                                          }

                                          work();
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF MustBeNonNull(this AspectF aspect, params object[] args)
        {
            return aspect.Combine(work =>
                                      {
                                          for (var i = 0; i < args.Length; i++)
                                          {
                                              var arg = args[i];
                                              if (arg == null)
                                                  throw new ArgumentException(
                                                      string.Format("Parameter at index {0} is null", i));
                                          }

                                          work();
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF Until(this AspectF aspect, Func<bool> test)
        {
            return aspect.Combine(work =>
                                      {
                                          while (!test())
                                          {
                                          }

                                          work();
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF While(this AspectF aspect, Func<bool> test)
        {
            return aspect.Combine(work =>
                                      {
                                          while (test())
                                              work();
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF WhenTrue(this AspectF aspect, params Func<bool>[] conditions)
        {
            return aspect.Combine(work =>
                                      {
                                          if (conditions.Any(condition => !condition()))
                                              return;

                                          work();
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF Log(this AspectF aspect, ILogger logger, string[] categories,
                                  string logMessage, params object[] arg)
        {
            return aspect.Combine(work =>
                                      {
                                          logger.Log(categories, logMessage);

                                          work();
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF Log(this AspectF aspect, string[] categories,
                                  string logMessage, params object[] arg)
        {
            return Log(aspect, Let.Logger(), categories, logMessage, arg);
        }


        [DebuggerStepThrough]
        public static AspectF Log(this AspectF aspect, ILogger logger,
                                  string logMessage, params object[] arg)
        {
            return aspect.Combine(work =>
                                      {
                                          logger.Log(string.Format(logMessage, arg));

                                          work();
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF Log(this AspectF aspect,
                                  string logMessage, params object[] arg)
        {
            return Log(aspect, Let.Logger(), logMessage, arg);
        }

        [DebuggerStepThrough]
        public static AspectF Log(this AspectF aspect, ILogger logger, string[] categories,
                                  string beforeMessage, string afterMessage)
        {
            return aspect.Combine(work =>
                                      {
                                          logger.Log(categories, beforeMessage);

                                          work();

                                          logger.Log(categories, afterMessage);
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF Log(this AspectF aspect, string[] categories,
                                  string beforeMessage, string afterMessage)
        {
            return Log(aspect, Let.Logger(), categories, beforeMessage, afterMessage);
        }

        [DebuggerStepThrough]
        public static AspectF Log(this AspectF aspect, ILogger logger,
                                  string beforeMessage, string afterMessage)
        {
            return aspect.Combine(work =>
                                      {
                                          logger.Log(beforeMessage);

                                          work();

                                          logger.Log(afterMessage);
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF Log(this AspectF aspect,
                                  string beforeMessage, string afterMessage)
        {
            return Log(aspect, Let.Logger(), beforeMessage, afterMessage);
        }

        [DebuggerStepThrough]
        public static AspectF HowLong(this AspectF aspect, ILogger logger,
                                      string startMessage, string endMessage)
        {
            return aspect.Combine(work =>
                                      {
                                          DateTime start = DateTime.Now;
                                          logger.Log(startMessage);

                                          work();

                                          DateTime end = DateTime.Now.ToUniversalTime();
                                          TimeSpan duration = end - start;

                                          logger.Log(string.Format(endMessage, duration.TotalMilliseconds,
                                                                   duration.TotalSeconds, duration.TotalMinutes, duration.TotalHours,
                                                                   duration.TotalDays));
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF HowLong(this AspectF aspect,
                                      string startMessage, string endMessage)
        {
            return HowLong(aspect, Let.Logger(), startMessage, endMessage);
        }

        [DebuggerStepThrough]
        public static AspectF TrapLog(this AspectF aspect, ILogger logger)
        {
            return aspect.Combine(work =>
                                      {
                                          try
                                          {
                                              work();
                                          }
                                          catch (Exception x)
                                          {
                                              logger.LogException(x);
                                          }
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF TrapLog(this AspectF aspect)
        {
            return TrapLog(aspect, Let.Logger());
        }

        [DebuggerStepThrough]
        public static AspectF TrapLogThrow(this AspectF aspect, ILogger logger)
        {
            return aspect.Combine(work =>
                                      {
                                          try
                                          {
                                              work();
                                          }
                                          catch (Exception x)
                                          {
                                              logger.LogException(x);
                                              throw;
                                          }
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF TrapLogThrow(this AspectF aspect)
        {
            return TrapLogThrow(aspect, Let.Logger());
        }

        [DebuggerStepThrough]
        public static AspectF RunAsync(this AspectF aspect, Action completeCallback)
        {
            return aspect.Combine(work => work.BeginInvoke(asyncresult =>
                                                               {
                                                                   work.EndInvoke(asyncresult); completeCallback();
                                                               }, null));
        }

        [DebuggerStepThrough]
        public static AspectF RunAsync(this AspectF aspect)
        {
            return aspect.Combine(work => work.BeginInvoke(work.EndInvoke, null));
        }

        [DebuggerStepThrough]
        public static AspectF Cache<TReturnType>(this AspectF aspect,
                                                 ICache cacheResolver, string key)
        {
            return aspect.Combine(work => Cache<TReturnType>(aspect, cacheResolver, key, work, cached => cached));
        }

        [DebuggerStepThrough]
        public static AspectF Cache<TReturnType>(this AspectF aspect,
                                                 string key)
        {
            return Cache<TReturnType>(aspect, Let.Cache(), key);
        }

        [DebuggerStepThrough]
        public static AspectF CacheList<TItemType, TListType>(this AspectF aspect,
                                                              ICache cacheResolver, string listCacheKey, Func<TItemType, string> getItemKey)
            where TListType : IList<TItemType>, new()
        {
            return aspect.Combine(work =>
                                      {
                                          var workDelegate = aspect.WorkDelegate as Func<TListType>;

                                          // Replace the actual work delegate with a new delegate so that
                                          // when the actual work delegate returns a collection, each item
                                          // in the collection is stored in cache individually.
                                          Func<TListType> newWorkDelegate = () =>
                                                                                {
                                                                                    if (workDelegate != null)
                                                                                    {
                                                                                        TListType collection = workDelegate();
                                                                                        foreach (TItemType item in collection)
                                                                                        {
                                                                                            string key = getItemKey(item);
                                                                                            cacheResolver.Set(key, item);
                                                                                        }
                                                                                        return collection;
                                                                                    }
                                                                                    return default(TListType);
                                                                                };
                                          aspect.WorkDelegate = newWorkDelegate;

                                          // Get the collection from cache or real source. If collection is returned
                                          // from cache, resolve each item in the collection from cache
                                          Cache<TListType>(aspect, cacheResolver, listCacheKey, work,
                                                           cached =>
                                                               {
                                                                   // Get each item from cache. If any of the item is not in cache
                                                                   // then discard the whole collection from cache and reload the 
                                                                   // collection from source.
                                                                   var itemList = new TListType();
                                                                   foreach (object cachedItem in cached.Select(item => cacheResolver.Get(getItemKey(item))))
                                                                   {
                                                                       if (null != cachedItem)
                                                                       {
                                                                           itemList.Add((TItemType)cachedItem);
                                                                       }
                                                                       else
                                                                       {
                                                                           // One of the item is missing from cache. So, discard the 
                                                                           // cached list.
                                                                           return default(TListType);
                                                                       }
                                                                   }

                                                                   return itemList;
                                                               });
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF CacheList<TItemType, TListType>(this AspectF aspect,
                                                              string listCacheKey, Func<TItemType, string> getItemKey)
            where TListType : IList<TItemType>, new()
        {
            return CacheList<TItemType, TListType>(aspect, Let.Cache(), listCacheKey, getItemKey);
        }

        [DebuggerStepThrough]
        public static AspectF CacheRetry<TReturnType>(this AspectF aspect,
                                                      ICache cacheResolver,
                                                      ILogger logger,
                                                      string key)
        {
            return aspect.Combine(work =>
                                      {
                                          try
                                          {
                                              Cache<TReturnType>(aspect, cacheResolver, key, work, cached => cached);
                                          }
                                          catch (Exception x)
                                          {
                                              logger.LogException(x);
                                              System.Threading.Thread.Sleep(1000);

                                              //Retry
                                              try
                                              {
                                                  Cache<TReturnType>(aspect, cacheResolver, key, work, cached => cached);
                                              }
                                              catch (Exception ex)
                                              {
                                                  logger.LogException(ex);
                                                  throw;
                                              }
                                          }
                                      });
        }

        [DebuggerStepThrough]
        public static AspectF CacheRetry<TReturnType>(this AspectF aspect,
                                                      string key)
        {
            return CacheRetry<TReturnType>(aspect, Let.Cache(), Let.Logger(), key);
        }

        private static void Cache<TReturnType>(AspectF aspect, ICache cacheResolver,
                                               string key, Action work, Func<TReturnType, TReturnType> foundInCache)
        {
            object cachedData = cacheResolver.Get(key);
            if (cachedData == null)
            {
                GetListFromSource<TReturnType>(aspect, cacheResolver, key);
            }
            else
            {
                // Give caller a chance to shape the cached item before it is returned
                TReturnType cachedType = foundInCache((TReturnType)cachedData);
                if (cachedType == null)
                {
                    GetListFromSource<TReturnType>(aspect, cacheResolver, key);
                }
                else
                {
                    aspect.WorkDelegate = new Func<TReturnType>(() => cachedType);
                }
            }

            work();
        }

        public static AspectF Expected<TException>(this AspectF aspect)
            where TException : Exception
        {
            return aspect.Combine(work =>
                                      {
                                          try
                                          {
                                              work();
                                          }
                                          catch (TException x)
                                          {
                                              Debug.WriteLine(x.ToString());
                                          }
                                      });
        }

        public static AspectF Transaction(this AspectF aspect)
        {
            return aspect.Combine(work =>
                                      {
                                          using (var scope = new TransactionScope(TransactionScopeOption.Required))
                                          {
                                              work();
                                              scope.Complete();
                                          }
                                      });
        }

        private static void GetListFromSource<TReturnType>(AspectF aspect, ICache cacheResolver,
                                                           string key)
        {
            var workDelegate = aspect.WorkDelegate as Func<TReturnType>;
            if (workDelegate != null)
            {
                TReturnType realObject = workDelegate();
                cacheResolver.Add(key, realObject);
                workDelegate = () => realObject;
            }
            aspect.WorkDelegate = workDelegate;
        }

        /// <summary>
        /// Returns the instance of old object with new operations applied on.
        /// </summary>
        /// <typeparam name="TReturnType">The type of the object new operations will be applied on.</typeparam>
        /// <param name="aspect"></param>
        /// <param name="item">The object need to be modified.</param>
        /// <param name="action">The delegate which performs on the object supplied.</param>
        /// <returns>Returns the old object with new operations applied on.</returns>
        [DebuggerStepThrough]
        public static TReturnType Use<TReturnType>(this AspectF aspect, TReturnType item, Action<TReturnType> action)
        {
            return aspect.Return(() =>
                                     {
                                         action(item);
                                         return item;
                                     });
        }
    }
}