// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Reflection;

namespace IQToolkit
{
    /// <summary>
    /// Make a strongly-typed delegate to a weakly typed method (one that takes single object[] argument)
    /// (up to 8 arguments)
    /// </summary>
    public class StrongDelegate
    {
        private readonly Func<object[], object> fn;

        private StrongDelegate(Func<object[], object> fn)
        {
            this.fn = fn;
        }

        private static MethodInfo[] _meths;

        static StrongDelegate()
        {
            // find all the various M<> methods
            _meths = new MethodInfo[9];

            foreach (var gm in typeof(StrongDelegate).GetTypeInfo().DeclaredMethods)
            {
                if (gm.Name.StartsWith("M"))
                {
                    var tas = gm.GetGenericArguments();
                    _meths[tas.Length - 1] = gm;
                }
            }
        }

        /// <summary>
        /// Create a strongly typed delegate over a method with a weak signature
        /// </summary>
        /// <param name="delegateType">The strongly typed delegate's type</param>
        /// <param name="target">The target instance for the delegate. This can be specified as null if the method is static.</param>
        /// <param name="method">Any method that takes a single array of objects and returns an object.</param>
        /// <returns></returns>
        public static Delegate CreateDelegate(Type delegateType, object target, MethodInfo method)
        {
            if (delegateType == null)
                throw new ArgumentNullException(nameof(delegateType));

            if (!delegateType.GetTypeInfo().IsSubclassOf(typeof(Delegate)))
                throw new ArgumentException(string.Format("The type '{0}' is not a delegate type.", delegateType.FullName));

            if (method == null)
                throw new ArgumentNullException(nameof(method));

            if (target == null && !method.IsStatic)
                throw new ArgumentException(string.Format("The method '{0}' requires a non-null target.", method.Name));

            return CreateDelegate(delegateType, (Func<object[], object>)method.CreateDelegate(typeof(Func<object[], object>), target));
        }

        /// <summary>
        /// Create a strongly typed delegate over a Func delegate with weak signature
        /// </summary>
        /// <param name="delegateType"></param>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static Delegate CreateDelegate(Type delegateType, Func<object[], object> fn)
        {
            if (delegateType == null)
                throw new ArgumentNullException(nameof(delegateType));

            if (!delegateType.GetTypeInfo().IsSubclassOf(typeof(Delegate)))
                throw new ArgumentException(string.Format("The type '{0}' is not a delegate type.", delegateType.FullName));

            if (fn == null)
                throw new ArgumentNullException(nameof(fn));

            MethodInfo invoke = delegateType.GetTypeInfo().GetDeclaredMethod("Invoke");
            var parameters = invoke.GetParameters();
            Type[] typeArgs = new Type[1 + parameters.Length];

            for (int i = 0, n = parameters.Length; i < n; i++)
            {
                typeArgs[i] = parameters[i].ParameterType;
            }

            typeArgs[typeArgs.Length - 1] = invoke.ReturnType;

            if (typeArgs.Length <= _meths.Length)
            {
                var gm = _meths[typeArgs.Length - 1];
                var m = gm.MakeGenericMethod(typeArgs);
                return m.CreateDelegate(delegateType, new StrongDelegate(fn));
            }

            throw new NotSupportedException(string.Format("The function has more than {0} arguments.", _meths.Length - 1));
        }

        public R M<R>()
        {
            return (R)fn(null);
        }

        public R M<A1, R>(A1 a1)
        {
            return (R)fn(new object[] { a1 });
        }

        public R M<A1, A2, R>(A1 a1, A2 a2)
        {
            return (R)fn(new object[] { a1, a2 });
        }

        public R M<A1, A2, A3, R>(A1 a1, A2 a2, A3 a3)
        {
            return (R)fn(new object[] { a1, a2, a3 });
        }

        public R M<A1, A2, A3, A4, R>(A1 a1, A2 a2, A3 a3, A4 a4)
        {
            return (R)fn(new object[] { a1, a2, a3, a4 });
        }

        public R M<A1, A2, A3, A4, A5, R>(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5)
        {
            return (R)fn(new object[] { a1, a2, a3, a4, a5 });
        }

        public R M<A1, A2, A3, A4, A5, A6, R>(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6)
        {
            return (R)fn(new object[] { a1, a2, a3, a4, a5, a6 });
        }

        public R M<A1, A2, A3, A4, A5, A6, A7, R>(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7)
        {
            return (R)fn(new object[] { a1, a2, a3, a4, a5, a6, a7 });
        }

        public R M<A1, A2, A3, A4, A5, A6, A7, A8, R>(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6, A7 a7, A8 a8)
        {
            return (R)fn(new object[] { a1, a2, a3, a4, a5, a6, a7, a8 });
        }
    }
}