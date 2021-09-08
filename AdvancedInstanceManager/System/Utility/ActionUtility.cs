using System;

namespace InstanceManager.Utility
{

    /// <summary>Provides utility functions for working with <see cref="Action"/>.</summary>
    public static class ActionUtility
    {

        /// <summary>Runs the <see cref="Action"/> in a try catch block.</summary>
        /// <param name="action">The action to run.</param>
        /// <param name="hideError">If <see langword="true"/>, then the error won't be rethrown.</param>
        public static void Try(this Action action, bool hideError = false)
        {

            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                if (!hideError)
                    throw ex;
            }

        }

        /// <summary>Runs the <see cref="Func{TResult}"/> in a try catch block.</summary>
        /// <param name="action">The action to run.</param>
        /// <param name="hideError">If <see langword="true"/>, then the error won't be rethrown.</param>
        public static T Try<T>(this Func<T> func, bool hideError = false)
        {
            T obj = default;
            Try(() => obj = func.Invoke(), hideError);
            return obj;
        }

    }

}
