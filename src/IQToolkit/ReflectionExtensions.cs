// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Reflection;

namespace IQToolkit
{
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Gets the value of the field or property of the instance.
        /// </summary>
        public static object GetValue(this MemberInfo member, object instance)
        {
            PropertyInfo pi = member as PropertyInfo;
            if (pi != null)
            {
                return pi.GetValue(instance, null);
            }

            FieldInfo fi = member as FieldInfo;
            if (fi != null)
            {
                return fi.GetValue(instance);
            }

            throw new InvalidOperationException();
        }

        /// <summary>
        /// Sets the value of the field or property of the instance.
        /// </summary>
        public static void SetValue(this MemberInfo member, object instance, object value)
        {
            var pi = member as PropertyInfo;
            if (pi != null)
            {
                pi.SetValue(instance, value, null);
                return;
            }

            var fi = member as FieldInfo;
            if (fi != null)
            {
                fi.SetValue(instance, value);
                return;
            }

            throw new InvalidOperationException();
         }
    }
}