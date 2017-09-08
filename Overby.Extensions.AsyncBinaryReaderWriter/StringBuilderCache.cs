using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Overby.Extensions.AsyncBinaryReaderWriter
{
    /// <summary>
    /// Proxy to the internal StringBuilderCache via cached delegates
    /// </summary>
    internal static class StringBuilderCache
    {
        static readonly Type type = typeof(object).Assembly.GetType("System.Text.StringBuilderCache");

        public static StringBuilder Acquire(int capacity = 16) =>
            _acquire.Value(capacity);

        private static readonly Lazy<Func<int, StringBuilder>> _acquire = new Lazy<Func<int, StringBuilder>>(CreateAcquireDelegate);

        private static Func<int, StringBuilder> CreateAcquireDelegate()
        {            
            var exp_capacity = Expression.Parameter(typeof(int));
            var acquireInfo = type.GetMethod(nameof(Acquire),
                BindingFlags.Static | BindingFlags.Public);

            var exp_call_acquire = Expression.Call(acquireInfo, exp_capacity);
            var lambda = Expression.Lambda(exp_call_acquire, exp_capacity);
            return (Func<int, StringBuilder>)lambda.Compile();
        }     

        public static string GetStringAndRelease(StringBuilder sb) =>
            _getStringAndRelease.Value(sb);

        private static readonly Lazy<Func<StringBuilder, string>> _getStringAndRelease =
            new Lazy<Func<StringBuilder, string>>(CreateGetStringAndReleaseDelegate);

        private static Func<StringBuilder, string> CreateGetStringAndReleaseDelegate()
        {
            var exp_sb = Expression.Parameter(typeof(StringBuilder));
            var releaseMethod = type.GetMethod(nameof(GetStringAndRelease),
                BindingFlags.Static | BindingFlags.Public);
            var exp_call = Expression.Call(releaseMethod, exp_sb);
            var lambda = Expression.Lambda(exp_call, exp_sb);
            return (Func<StringBuilder, string>)lambda.Compile();
        }
    }

}
