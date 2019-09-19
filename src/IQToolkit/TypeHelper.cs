// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

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

            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType)
            {
                foreach (Type arg in typeInfo.GenericTypeArguments)
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.GetTypeInfo().IsAssignableFrom(typeInfo))
                    {
                        return ienum;
                    }
                }
            }

            foreach (Type iface in typeInfo.ImplementedInterfaces)
            {
                Type ienum = FindIEnumerable(iface);
                if (ienum != null) return ienum;
            }

            if (typeInfo.BaseType != null && typeInfo.BaseType != typeof(object))
            {
                return FindIEnumerable(typeInfo.BaseType);
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
            return ienum.GetTypeInfo().GenericTypeArguments[0];
        }

        /// <summary>
        /// Returns true if the type is a <see cref="Nullable{T}"/>.
        /// </summary>
        public static bool IsNullableType(Type type)
        {
            return type != null && type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Returns true if the type can be assigned the value null.
        /// </summary>
        public static bool IsNullAssignable(Type type)
        {
            return !type.GetTypeInfo().IsValueType || IsNullableType(type);
        }

        /// <summary>
        /// Gets the underlying type if the specified type is a <see cref="Nullable{T}"/>,
        /// otherwise just returns given type.
        /// </summary>
        public static Type GetNonNullableType(Type type)
        {
            if (IsNullableType(type))
            {
                return type.GetTypeInfo().GenericTypeArguments[0];
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
            bool isNullable = !type.GetTypeInfo().IsValueType || TypeHelper.IsNullableType(type);
            if (!isNullable)
                return Activator.CreateInstance(type);
            return null;
        }

        /// <summary>
        /// Returns true if the member is either a read-only field or get-only property.
        /// </summary>
        public static bool IsReadOnly(MemberInfo member)
        {
            var pi = member as PropertyInfo;
            if (pi != null)
            {
                return !pi.CanWrite || pi.SetMethod == null;
            }

            var fi = member as FieldInfo;
            if (fi != null)
            {
                return (fi.Attributes & FieldAttributes.InitOnly) != 0;
            }

            return true;
        }

        /// <summary>
        /// Return true if the type is a kind of integer.
        /// </summary>
        public static bool IsInteger(Type type)
        {
            Type nnType = GetNonNullableType(type);

            switch (GetTypeCode(nnType))
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

        /// <summary>
        /// Gets the <see cref="TypeCode"/> for the specified type.
        /// </summary>
        public static TypeCode GetTypeCode(Type type)
        {
            if (type == typeof(bool))
            {
                return TypeCode.Boolean;
            }
            else if (type == typeof(byte))
            {
                return TypeCode.Byte;
            }
            else if (type == typeof(sbyte))
            {
                return TypeCode.SByte;
            }
            else if (type == typeof(short))
            {
                return TypeCode.Int16;
            }
            else if (type == typeof(ushort))
            {
                return TypeCode.UInt16;
            }
            else if (type == typeof(int))
            {
                return TypeCode.Int32;
            }
            else if (type == typeof(uint))
            {
                return TypeCode.UInt32;
            }
            else if (type == typeof(long))
            {
                return TypeCode.Int64;
            }
            else if (type == typeof(ulong))
            {
                return TypeCode.UInt64;
            }
            else if (type == typeof(float))
            {
                return TypeCode.Single;
            }
            else if (type == typeof(double))
            {
                return TypeCode.Double;
            }
            else if (type == typeof(decimal))
            {
                return TypeCode.Decimal;
            }
            else if (type == typeof(string))
            {
                return TypeCode.String;
            }
            else if (type == typeof(char))
            {
                return TypeCode.Char;
            }
            else if (type == typeof(DateTime))
            {
                return TypeCode.DateTime;
            }
            else
            {
                return TypeCode.Object;
            }
        }

        /// <summary>
        /// True if the type is assignable from the other type.
        /// </summary>
        public static bool IsAssignableFrom(this Type type, Type otherType)
        {
            return type.GetTypeInfo().IsAssignableFrom(otherType.GetTypeInfo());
        }

        /// <summary>
        /// Gets the data members of the type (non-static properties and fields)
        /// </summary>
        public static IEnumerable<MemberInfo> GetDataMembers(this Type type, string name = null, bool includeNonPublic = false)
        {
            return GetInheritedProperites(type)
                    .Where(p => p.CanRead && !p.GetMethod.IsStatic && (p.GetMethod.IsPublic || includeNonPublic) && (name == null || p.Name == name))
                .Cast<MemberInfo>().Concat(
                    GetInheritedFields(type)
                        .Where(f => !f.IsStatic && (f.IsPublic || includeNonPublic) && (name == null || f.Name == name)));
        }

        /// <summary>
        /// Gets the data member with the specified name (non-static properties and fields).
        /// </summary>
        public static MemberInfo GetDataMember(this Type type, string name, bool includeNonPublic = false)
        {
            return GetDataMembers(type, name, includeNonPublic).FirstOrDefault();
        }

        /// <summary>
        /// Finds the matching method declared on the specified type, or inherited from a base type.
        /// </summary>
        public static MethodInfo FindMethod(this Type type, string name, Type[] typeArguments, Type[] parameterTypes)
        {
            int typeArgumentCount = typeArguments != null ? typeArguments.Length : 0;

            foreach (var method in type.GetInheritedMethods())
            {
                if (method.IsGenericMethodDefinition != (typeArgumentCount > 0))
                    continue;

                if (method.Name != name)
                    continue;

                if (method.IsGenericMethodDefinition)
                {
                    if (method.GetGenericArguments().Length != typeArgumentCount)
                        continue;

                    var constructedMethod = method.MakeGenericMethod(typeArguments);

                    if (ParametersMatch(constructedMethod.GetParameters(), parameterTypes))
                    {
                        return constructedMethod;
                    }
                }

                if (ParametersMatch(method.GetParameters(), parameterTypes))
                {
                    return method;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the matching constructor declared on the specified type.
        /// </summary>
        public static ConstructorInfo FindConstructor(this Type type, Type[] parameterTypes)
        {
            foreach (var constructor in type.GetTypeInfo().DeclaredConstructors)
            {
                if (ParametersMatch(constructor.GetParameters(), parameterTypes))
                {
                    return constructor;
                }
            }

            return null;
        }

        private static bool TypesMatch(Type[] a, Type[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        private static bool ParametersMatch(ParameterInfo[] parameters, Type[] parameterTypes)
        {
            if (parameters.Length != parameterTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType != parameterTypes[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a list of the type and all its base types (recursive)
        /// </summary>
        private static IEnumerable<TypeInfo> GetInheritedTypeInfos(this Type type)
        {
            var info = type.GetTypeInfo();

            yield return info;

            // interfaces include their inherited interfaces
            // todo: remove duplicates 
            if (info.IsInterface)
            {
                foreach (var ii in info.ImplementedInterfaces)
                {
                    foreach (var iface in GetInheritedTypeInfos(ii))
                    {
                        yield return iface;
                    }
                }
            }
            else
            {
                for (var i = info.BaseType?.GetTypeInfo(); i != null; i = i.BaseType?.GetTypeInfo())
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Gets a list of all properties, including inherited properties.
        /// </summary>
        public static IEnumerable<PropertyInfo> GetInheritedProperites(this Type type)
        {
            // to do: remove duplicates
            foreach(var info in GetInheritedTypeInfos(type))
            {
                foreach (var p in info.DeclaredProperties)
                {
                    yield return p;
                }
            }
        }

        /// <summary>
        /// Gets a list of all fields, including inherited fields.
        /// </summary>
        public static IEnumerable<FieldInfo> GetInheritedFields(this Type type)
        {
            // to do: remove duplicates
            foreach (var info in GetInheritedTypeInfos(type))
            {
                foreach (var p in info.DeclaredFields)
                {
                    yield return p;
                }
            }
        }

        /// <summary>
        /// Gets a list of all methods, including inherited methods.
        /// </summary>
        public static IEnumerable<MethodInfo> GetInheritedMethods(this Type type)
        {
            // to do: remove duplicates
            foreach (var info in GetInheritedTypeInfos(type))
            {
                foreach (var p in info.DeclaredMethods)
                {
                    yield return p;
                }
            }
        }

        // holds a delegate to the runtime implemented API
        private static Func<Type, object> fnGetUninitializedObject;

        /// <summary>
        /// Gets an unitialized instance of an object of the specified type.
        /// </summary>
        public static object GetUninitializedObject(this Type type)
        {
            if (fnGetUninitializedObject == null)
            {
                var a = typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetTypeInfo().Assembly;
                var fs = a.DefinedTypes.FirstOrDefault(t => t.FullName == "System.Runtime.Serialization.FormatterServices");
                var guo = fs?.DeclaredMethods.FirstOrDefault(m => m.Name == nameof(GetUninitializedObject));
                if (guo == null)
                    throw new NotSupportedException($"The runtime does not support the '{nameof(GetUninitializedObject)}' API.");
                System.Threading.Interlocked.CompareExchange(ref fnGetUninitializedObject, (Func<Type, object>)guo.CreateDelegate(typeof(Func<Type, object>)), null);
            }

            return fnGetUninitializedObject(type);
        }
    }
}
