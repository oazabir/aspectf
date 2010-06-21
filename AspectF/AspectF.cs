using System;

namespace OmarALZabir.AspectF
{
    public abstract class AspectF
    {
        /// <summary>
        ///  Create a composition of function e.g. f(g(x))
        ///  </summary><param name="newAspectDelegate">A delegate that offers an aspect's behavior. 
        ///  It's added into the aspect chain</param><returns></returns>
        public abstract AspectF Combine(Action<Action> newAspectDelegate);

        /// <summary>
        ///  Execute your real code applying the aspects over it
        ///  </summary><param name="work">The actual code that needs to be run</param>
        public abstract void Do(Action work);

        /// <summary>
        ///  Execute your real code applying aspects over it.
        ///  </summary><typeparam name="TReturnType"></typeparam><param name="work">The actual code that needs to be run</param><returns></returns>
        public abstract TReturnType Return<TReturnType>(Func<TReturnType> work);

        /// <summary>
        ///  Chain of aspects to invoke
        ///  </summary>
        public Action<Action> Chain { get; set; }

        /// <summary>
        ///  The acrual work delegate that is finally called
        ///  </summary>
        public Delegate WorkDelegate { get; set; }
    }
}