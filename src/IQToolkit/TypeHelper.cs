// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit
{
    /// <summary>
    /// Type related helper methods
    /// </summary>
    public static class TypeHelper
    {
        /// <summary>
        /// Finds the type's implemented <see cref="IEnumerable{T}"/> type.
        /// </summary>
        public static Type FindIEnumerable(Type type)
        {
            if (type == null || type == typeof(string))
                return null;

            if (type.IsArray)
                return typeof(IEnumerable<>).MakeGenericType(type.GetElementType());

            if (type.IsGenericType)
            {
                foreach (Type arg in type.GetGenericArguments())
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(type))
                    {
                        return ienum;
                    }
                }
            }

            Type[] ifaces = type.GetInterfaces();
            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (Type iface in ifaces)
                {
                    Type ienum = FindIEnumerable(iface);
                    if (ienum != null) return ienum;
                }
            }

            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                return FindIEnumerable(type.BaseType);
            }

            return null;
        }

        /// <summary>
        /// Returns true if the type is a sequence type.
        /// </summary>
        public static bool IsSequenceType(Type type)
        {
            return FindIEnumerable(type) != null;
        }

        /// <summary>
        /// Gets the constructed <see cref="IEnumerable{T}"/> for the given element type.
        /// </summary>
        public static Type GetSequenceType(Type elementType)
        {
            return typeof(IEnumerable<>).MakeGenericType(elementType);
        }

        /// <summary>
        /// Gets the element type given the sequence type.
        /// If the type is not a sequence, returns the type itself.
        /// </summary>
        public static Type GetElementType(Type sequenceType)
        {
            Type ienum = FindIEnumerable(sequenceType);
            if (ienum == null) return sequenceType;
            return ienum.GetGenericArguments()[0];
        }

        /// <summary>
        /// Returns true if the type is a <see cref="Nullable{T}"/>.
        /// </summary>
        public static bool IsNullableType(Type type)
        {
            return type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Returns true if the type can be assigned the value null.
        /// </summary>
        public static bool IsNullAssignable(Type type)
        {
            return !type.IsValueType || IsNullableType(type);
        }

        /// <summary>
        /// Gets the underlying type if the specified type is a <see cref="Nullable{T}"/>,
        /// otherwise just returns given type.
        /// </summary>
        public static Type GetNonNullableType(Type type)
        {
            if (IsNullableType(type))
            {
                return type.GetGenericArguments()[0];
            }

            return type;
        }

        /// <summary>
        /// Gets a null-assignable variation of the type.
        /// Returns a <see cref="Nullable{T}"/> type if the given type is a value type.
        /// </summary>
        public static Type GetNullAssignableType(Type type)
        {
            if (!IsNullAssignable(type))
            {
                return typeof(Nullable<>).MakeGenericType(type);
            }

            return type;
        }

        /// <summary>
        /// Gets the <see cref="ConstantExpression"/> for null of the specified type.
        /// </summary>
        public static ConstantExpression GetNullConstant(Type type)
        {
            return Expression.Constant(null, GetNullAssignableType(type));
        }

        /// <summary>
        /// Gets the type of the <see cref="MemberInfo"/>.
        /// </summary>
        public static Type GetMemberType(MemberInfo mi)
        {
            FieldInfo fi = mi as FieldInfo;
            if (fi != null) return fi.FieldType;
            PropertyInfo pi = mi as PropertyInfo;
            if (pi != null) return pi.PropertyType;
            EventInfo ei = mi as EventInfo;
            if (ei != null) return ei.EventHandlerType;
            MethodInfo meth = mi as MethodInfo;  // property getters really
            if (meth != null) return meth.ReturnType;
            return null;
        }

        /// <summary>
        /// Gets the default value of the specified type.
        /// </summary>
        public static object GetDefault(Type type)
        {
            bool isNullable = !type.IsValueType || TypeHelper.IsNullableType(type);
            if (!isNullable)
                return Activator.CreateInstance(type);
            return null;
        }

        /// <summary>
        /// Returns true if the member is either a read-only field or get-only property.
        /// </summary>
        public static bool IsReadOnly(MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return (((FieldInfo)member).Attributes & FieldAttributes.InitOnly) != 0;
                case MemberTypes.Property:
                    PropertyInfo pi = (PropertyInfo)member;
                    return !pi.CanWrite || pi.GetSetMethod() == null;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Return true if the type is a kind of integer.
        /// </summary>
        public static bool IsInteger(Type type)
        {
            Type nnType = GetNonNullableType(type);
            switch (Type.GetTypeCode(nnType))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }
    }
}
