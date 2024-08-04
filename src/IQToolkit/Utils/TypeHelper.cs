// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Utils
{
    /// <summary>
    /// Type related helper methods
    /// </summary>
    public static class TypeHelper
    {
        /// <summary>
        /// Gets the generic type corresponding to the generic type definition
        /// inherited from or implemented by the type.
        /// </summary>
        public static bool TryGetGenericType(
            this Type type, 
            Type genericDefinition, 
            out Type genericType)
        {
            if (type.IsGenericType
                && type.GetGenericTypeDefinition() == genericDefinition)
            {
                genericType = type;
                return true;
            }

            if (genericDefinition.IsInterface)
            {
                foreach (Type iface in type.GetInterfaces())
                {
                    if (TryGetGenericType(iface, genericDefinition, out genericType))
                        return true;
                }
            }

            if (type.BaseType is Type baseType 
                && baseType != typeof(object))
            {
                return TryGetGenericType(baseType, genericDefinition, out genericType);
            }

            genericType = default!;
            return false;
        }

        /// <summary>
        /// Returns true if the type implements or inherits from some concrete from of the generic type.
        /// </summary>
        public static bool HasGenericType(
            this Type type,
            Type genericTypeDefinition)
        {
            return type.TryGetGenericType(genericTypeDefinition, out _);
        }

        /// <summary>
        /// Returns true if the type is assignable to some form of the generic type definition.
        /// </summary>
        public static bool IsAssignableToGeneric(
            this Type type,
            Type genericTypeDefinition)
        {
            return type.TryGetGenericType(genericTypeDefinition, out _);
        }          

        /// <summary>
        /// Returns true if the type is a collection type (not scalar).
        /// </summary>
        public static bool IsSequenceType(this Type type)
        {
            return IsSequenceType(type, out _);
        }

        /// <summary>
        /// Returns true if the type is a collection type (not scalar) and outputs the element type.
        /// </summary>
        public static bool IsSequenceType(this Type type, out Type elementType)
        {
            if (type != typeof(string))
            {
                if (type.IsArray)
                {
                    elementType = type.GetElementType();
                    return true;
                }

                if (TryGetGenericType(type, typeof(IEnumerable<>), out var ienumT))
                {
                    elementType = ienumT.GetGenericArguments()[0];
                    return true;
                }
            }

            elementType = default!;
            return false;
        }


        /// <summary>
        /// Returns the element type of the sequence type if the type is a sequence type,
        /// otherwise just returns the type.
        /// </summary>
        public static Type GetSequenceElementType(this Type possibleSequenceType)
        {
            if (IsSequenceType(possibleSequenceType, out var elementType))
                return elementType;
            return possibleSequenceType;
        }

        /// <summary>
        /// Returns true if the type is a <see cref="Nullable{T}"/>.
        /// </summary>
        public static bool IsNullableType(Type type)
        {
            return type != null 
                && type.IsGenericType 
                && type.GetGenericTypeDefinition() == typeof(Nullable<>);
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
                return type.GenericTypeArguments[0];
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
        public static Type GetMemberType(MemberInfo mi) =>
            mi is FieldInfo fi ? fi.FieldType
            : mi is PropertyInfo pi ? pi.PropertyType
            : mi is MethodInfo mth ? mth.ReturnType
            : typeof(object);

        /// <summary>
        /// Gets the type of the member of type of the member's collection.
        /// </summary>
        public static Type GetEntityType(MemberInfo mi) =>
            GetSequenceElementType(GetMemberType(mi));

        /// <summary>
        /// Gets the default value of the specified type.
        /// </summary>
        public static object? GetDefault(Type type)
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

        private static BindingFlags GetDeclaredBindingFlags(bool includeNonPublic) =>
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy
                | (includeNonPublic ? BindingFlags.NonPublic : BindingFlags.Default);

        /// <summary>
        /// Returns the declared instance fields and properties of the type.
        /// Does not include indexer properties.
        /// </summary>
        public static IReadOnlyList<MemberInfo> GetDeclaredFieldsAndProperties(
            this Type type,
            Func<MemberInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            var flags = GetDeclaredBindingFlags(includeNonPublic);
            return
                type.GetProperties(flags)
                    .Where(p => p.CanRead
                    && p.GetIndexParameters().Length == 0
                    && (fnMatch == null || fnMatch(p))
                    )
                .Cast<MemberInfo>().Concat(
                    type.GetFields(flags)
                        .Where(f => fnMatch == null || fnMatch(f))
                        )
                .ToReadOnly();
        }

        /// <summary>
        /// Returns the declared instance fields and properties of the type.
        /// Does not include indexer properties.
        /// </summary>
        public static IReadOnlyList<MemberInfo> GetDeclaredFieldsAndProperties(
            this Type type, 
            string name, 
            Func<MemberInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return GetDeclaredFieldsAndProperties(
                type,
                m => m.Name == name
                    && (fnMatch == null || fnMatch(m)),
                includeNonPublic
                )
                .ToReadOnly();               
        }

        /// <summary>
        /// Returns the matching instance field or property or null if not found.
        /// Does not include indexer properties.
        /// </summary>
        public static MemberInfo? FindDeclaredFieldOrProperty(
            this Type type,
            string name,
            Func<MemberInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return GetDeclaredFieldsAndProperties(type, name, fnMatch, includeNonPublic)
                .FirstOrDefault();
        }

        private static readonly char[] dotSeparator = new char[] { '.' };

        /// <summary>
        /// Returns the matching field or property identified by the dotted path, or null if not found.
        /// </summary>
        public static MemberInfo? FindDeclaredFieldOrPropertyFromPath(this Type type, string path)
        {
            MemberInfo? member = null;
            string[] names = path.Split(dotSeparator);

            foreach (string name in names)
            {
                member = FindDeclaredFieldOrProperty(type, name, includeNonPublic: true);
                if (member == null)
                    return null;

                type = GetEntityType(member);
            }

            return member;
        }

        /// <summary>
        /// Gets the value of the field or property.
        /// </summary>
        public static bool TryGetFieldOrPropertyValue(
            this MemberInfo fieldOrProperty, 
            object? instance, 
            out object? value)
        {
            if (fieldOrProperty is PropertyInfo prop
                && prop.CanRead
                && (instance != null || prop.GetGetMethod().IsStatic))
            {
                value = prop.GetValue(instance);
                return true;
            }
            else if (fieldOrProperty is FieldInfo field
                && (instance != null || field.IsStatic))
            {
                value = field.GetValue(instance);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Gets the value of the field or property.
        /// </summary>
        public static object? GetFieldOrPropertyValue(
            this MemberInfo fieldOrProperty, 
            object? instance)
        {
            return TryGetFieldOrPropertyValue(fieldOrProperty, instance, out var value)
                ? value
                : throw new InvalidOperationException($"Cannot get the value of '{fieldOrProperty.DeclaringType.Name}.{fieldOrProperty.Name}'.");
        }

        /// <summary>
        /// Sets the value of the field or property.
        /// </summary>
        public static bool TrySetFieldOrPropertyValue(
            this MemberInfo fieldOrProperty, 
            object? instance, 
            object? value)
        {
            if (fieldOrProperty is PropertyInfo prop
                && prop.CanWrite)
            {
                if (instance == null && !prop.GetGetMethod().IsStatic)
                    return false;
                prop.SetValue(instance, value);
                return true;
            }
            else if (fieldOrProperty is FieldInfo field)
            {
                if (instance == null && !field.IsStatic)
                    return false;
                field.SetValue(instance, value);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Sets the value of the field or property.
        /// </summary>
        public static void SetFieldOrPropertyValue(
            this MemberInfo fieldOrProperty, 
            object? instance, 
            object? value)
        {
            if (!fieldOrProperty.TrySetFieldOrPropertyValue(instance, value))
                throw new InvalidOperationException($"Cannot set the value of '{fieldOrProperty.DeclaringType.Name}.{fieldOrProperty.Name}'.");
        }

        /// <summary>
        /// Returns the matching declared instance properties of the type.
        /// Does not include indexer properties.
        /// </summary>
        public static IReadOnlyList<PropertyInfo> GetDeclaredProperties(
            this Type type,
            Func<PropertyInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return type.GetProperties(GetDeclaredBindingFlags(includeNonPublic))
                .Where(p => p.CanRead
                    && p.GetIndexParameters().Length == 0
                    && (fnMatch == null || fnMatch(p))
                    )
                .ToReadOnly();
        }

        /// <summary>
        /// Returns the matching declared instance property of the type or null if not found.
        /// Does not include indexer properties.
        /// </summary>
        public static PropertyInfo? FindDeclaredProperty(
            this Type type,
            Func<PropertyInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return type.GetProperties(GetDeclaredBindingFlags(includeNonPublic))
                .FirstOrDefault(p => p.CanRead
                    && p.GetIndexParameters().Length == 0
                    && (fnMatch == null || fnMatch(p))
                    );
        }

        /// <summary>
        /// Returns the matching declared instance property of the type or null if not found.
        /// Does not include indexer properties.
        /// </summary>
        public static PropertyInfo? FindDeclaredProperty(
            this Type type,
            string name,
            Func<PropertyInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return FindDeclaredProperty(
                type,
                p => name == p.Name && (fnMatch == null || fnMatch(p)),
                includeNonPublic
                );
        }

        /// <summary>
        /// Returns the matching declared instance indexer properties.
        /// </summary>
        public static IReadOnlyList<PropertyInfo> GetDeclaredIndexer(
            this Type type,
            Func<PropertyInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return type.GetProperties(GetDeclaredBindingFlags(includeNonPublic))
                .Where(p => p.CanRead
                    && p.GetIndexParameters().Length > 0
                    && (fnMatch == null || fnMatch(p))
                    )
                .ToReadOnly();
        }

        /// <summary>
        /// Returns the matching declared instance indexer property or null if not found.
        /// </summary>
        public static PropertyInfo? FindDeclaredIndexer(
            this Type type,
            Func<PropertyInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return type.GetProperties(GetDeclaredBindingFlags(includeNonPublic))
                .FirstOrDefault(p => p.CanRead
                    && p.GetIndexParameters().Length > 0
                    && (fnMatch == null || fnMatch(p))
                    );
        }

        /// <summary>
        /// Returns the matching declared instance indexer property or null if not found.
        /// </summary>
        public static PropertyInfo? FindDeclaredIndexer(
            this Type type,
            string name,
            Func<PropertyInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return FindDeclaredIndexer(
                type,
                p => name == p.Name && (fnMatch == null || fnMatch(p)),
                includeNonPublic
                );
        }

        /// <summary>
        /// Returns the matching declared instance methods.
        /// </summary>
        public static IReadOnlyList<MethodInfo> GetDeclaredMethods(
            this Type type,
            Func<MethodInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return type.GetMethods(GetDeclaredBindingFlags(includeNonPublic))
                .Where(m => (fnMatch == null || fnMatch(m)))
                .ToReadOnly();
        }

        /// <summary>
        /// Returns the first matching declared instance method or null.
        /// </summary>
        public static MethodInfo? FindDeclaredMethod(
            this Type type,
            Func<MethodInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return type.GetMethods(GetDeclaredBindingFlags(includeNonPublic))
                .FirstOrDefault(m => fnMatch == null || fnMatch(m));
        }

        /// <summary>
        /// Returns the matching declared instance methods.
        /// </summary>
        public static IReadOnlyList<MethodInfo> GetDeclaredMethods(
            this Type type,
            string name,
            Func<MethodInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return GetDeclaredMethods(
                type,
                m => m.Name == name && (fnMatch == null || fnMatch(m)),
                includeNonPublic
                );
        }

        /// <summary>
        /// Returns the first matching declared instance method or null.
        /// </summary>
        public static MethodInfo? FindDeclaredMethod(
            this Type type,
            string name,
            Func<MethodInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return FindDeclaredMethod(
                type,
                m => m.Name == name && (fnMatch == null || fnMatch(m)),
                includeNonPublic
                );
        }

        /// <summary>
        /// Returns the matching declared instance methods.
        /// </summary>
        public static IReadOnlyList<MethodInfo> GetDeclaredMethods(
            this Type type,
            string name,
            IReadOnlyList<Type> parameterTypes,
            Func<MethodInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return GetDeclaredMethods(type,
                name,
                m => !m.IsGenericMethod
                    && ParametersMatch(m.GetParameters(), parameterTypes) 
                    && (fnMatch == null || fnMatch(m)),
                includeNonPublic
                );
        }

        /// <summary>
        /// Returns the first matching declared instance method or null.
        /// </summary>
        public static MethodInfo? FindDeclaredMethod(
            this Type type,
            string name,
            IReadOnlyList<Type> parameterTypes,
            Func<MethodInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return FindDeclaredMethod(type,
                name,
                m => !m.IsGenericMethod
                    && ParametersMatch(m.GetParameters(), parameterTypes)
                    && (fnMatch == null || fnMatch(m)),
                includeNonPublic
                );
        }

        /// <summary>
        /// Returns the matching declared instance methods.
        /// </summary>
        public static IReadOnlyList<MethodInfo> GetDeclaredMethods(
            this Type type,
            string name,
            IReadOnlyList<Type> typeArguments,
            IReadOnlyList<Type> parameterTypes,
            Func<MethodInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            if (!(typeArguments is Type[] argsArray))
                argsArray = typeArguments.ToArray();

            return GetDeclaredMethods(
                type,
                m => m.IsGenericMethodDefinition
                    && m.Name == name
                    && m.GetGenericArguments().Length == typeArguments.Count
                    && m.GetParameters().Length == parameterTypes.Count
                    && m.MakeGenericMethod(argsArray) is { } constructedMethod
                    && ParametersMatch(constructedMethod.GetParameters(), parameterTypes)
                    && (fnMatch == null || fnMatch(constructedMethod)),
                includeNonPublic
                )
                .Select(m => m.MakeGenericMethod(argsArray))
                .ToReadOnly();
        }

        /// <summary>
        /// Returns the first matching declared instance method or null.
        /// </summary>
        public static MethodInfo? FindDeclaredMethod(
            this Type type,
            string name,
            IReadOnlyList<Type> typeArguments,
            IReadOnlyList<Type> parameterTypes,
            Func<MethodInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            if (!(typeArguments is Type[] argsArray))
                argsArray = typeArguments.ToArray();

            var method = FindDeclaredMethod(
                type,
                m => m.IsGenericMethodDefinition
                    && m.Name == name
                    && m.GetGenericArguments().Length == typeArguments.Count
                    && m.GetParameters().Length == parameterTypes.Count
                    && m.MakeGenericMethod(argsArray) is { } constructedMethod
                    && ParametersMatch(constructedMethod.GetParameters(), parameterTypes)
                    && (fnMatch == null || fnMatch(constructedMethod)),
                includeNonPublic
                );

            return method != null
                ? method.MakeGenericMethod(argsArray)
                : null;
        }

        /// <summary>
        /// Returns the matching declared instance constructors.
        /// </summary>
        public static IReadOnlyList<ConstructorInfo> GetDeclaredConstructors(
            this Type type,
            Func<ConstructorInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return type.GetConstructors(GetDeclaredBindingFlags(includeNonPublic))
                .Where(c => fnMatch == null || fnMatch(c))
                .ToReadOnly();
        }

        /// <summary>
        /// Returns the first matching declared instance constructor.
        /// </summary>
        public static ConstructorInfo? FindDeclaredConstructor(
            this Type type,
            Func<ConstructorInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return type.GetConstructors(GetDeclaredBindingFlags(includeNonPublic))
                .FirstOrDefault(c => fnMatch == null || fnMatch(c));
        }

        /// <summary>
        /// Returns the first matching declared instance constructor.
        /// </summary>
        public static ConstructorInfo? FindDeclaredConstructor(
            this Type type, 
            IReadOnlyList<Type> parameterTypes,
            Func<ConstructorInfo, bool>? fnMatch = null,
            bool includeNonPublic = false)
        {
            return FindDeclaredConstructor(
                type, 
                c => ParametersMatch(c.GetParameters(), parameterTypes) 
                    && (fnMatch == null || fnMatch(c)), 
                includeNonPublic
                );
        }

        /// <summary>
        /// Returns true if the lists of types match.
        /// </summary>
        private static bool TypesMatch(
            IReadOnlyList<Type> a, 
            IReadOnlyList<Type> b)
        {
            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the types of the list of parameters
        /// matches the list of types.
        /// </summary>
        private static bool ParametersMatch(
            IReadOnlyList<ParameterInfo> parameters, 
            IReadOnlyList<Type> parameterTypes)
        {
            if (parameters.Count != parameterTypes.Count)
            {
                return false;
            }

            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].ParameterType != parameterTypes[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if the method is an extension method.
        /// </summary>
        public static bool IsExtensionMethod(this MethodInfo method)
        {
            return method.GetCustomAttribute(typeof(System.Runtime.CompilerServices.ExtensionAttribute))
                != null;
        }
    }
}
