using System;
using System.Diagnostics;

namespace OmarALZabir.AspectF
{
    /// <summary>
    /// AspectF
    /// (C) Omar AL Zabir 2009 All rights reserved.
    /// 
    /// AspectF lets you add strongly typed Aspects within you code, 
    /// anywhere in the code, in a fluent way. In common AOP frameworks, 
    /// you define aspects as individual classes and you leave indication 
    /// in the code where the aspect needs to be injected. A weaver 
    /// then weaves it into the code for you. You can also implement AOP
    /// using Attributes and by inheriting your classes from MarshanByRef. 
    /// But that's not an option for you always to do so. There's also 
    /// another way of doing AOP using DynamicProxy.
    /// 
    /// AspectF tries to avoid all these special tricks. It has no need 
    /// for a weaver (or any post build tool). It also does not require
    /// extending classes from MarshalByRef or using DynamicProxy.
    /// 
    /// AspectF offers a plain vanilla way of putting aspects within 
    /// your methods. You can wrap your code using Aspects 
    /// by using standard wellknown C#/VB.NET code. 
    /// </summary>
    public class Let : AspectF
    {
        public Let()
        {
            Chain = null;
        }

        /// <summary>
        /// Gets and sets the <see cref="ILogger"/> instance and used when <see cref="ILogger"/> is not passed with <see cref="Let"/> class methods
        /// </summary>
        public static Func<ILogger> Logger { get; set; }
        /// <summary>
        /// Gets and sets the <see cref="ICache"/> instance and used when <see cref="ICache"/> is not passed with <see cref="Let"/> class methods
        /// </summary>
        public static Func<ICache> Cache { get; set; }

        /// <summary>
        /// Create a composition of function e.g. f(g(x))
        /// </summary>
        /// <param name="newAspectDelegate">A delegate that offers an aspect's behavior. 
        /// It's added into the aspect chain</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public override AspectF Combine(Action<Action> newAspectDelegate)
        {
            if (Chain == null)
            {
                Chain = newAspectDelegate;
            }
            else
            {
                Action<Action> existingChain = Chain;
                Action<Action> callAnother = work => existingChain(() => newAspectDelegate(work));
                Chain = callAnother;
            }
            return this;
        }

        /// <summary>
        /// Execute your real code applying the aspects over it
        /// </summary>
        /// <param name="work">The actual code that needs to be run</param>
        [DebuggerStepThrough]
        public override void Do(Action work)
        {
            if (Chain == null)
            {
                work();
            }
            else
            {
                Chain(work);
            }
        }

        /// <summary>
        /// Execute your real code applying aspects over it.
        /// </summary>
        /// <typeparam name="TReturnType"></typeparam>
        /// <param name="work">The actual code that needs to be run</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public override TReturnType Return<TReturnType>(Func<TReturnType> work)
        {
            WorkDelegate = work;

            if (Chain == null)
            {
                return work();
            }
            TReturnType returnValue = default(TReturnType);
            Chain(() =>
                      {
                          var workDelegate = WorkDelegate as Func<TReturnType>;
                          if (workDelegate != null) returnValue = workDelegate();
                      });
            return returnValue;
        }

        /// <summary>
        /// Handy property to start writing aspects using fluent style
        /// </summary>
        public static AspectF Us
        {
            [DebuggerStepThrough]
            get
            {
                return new Let();
            }
        }
    }
}
