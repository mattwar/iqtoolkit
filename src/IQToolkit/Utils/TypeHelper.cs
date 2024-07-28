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
        /// Gets the default value of the specified type.
        /// </summary>
        public static object? GetDefault(Type type)
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

        /// <summary>
        /// Gets the instance fields and properties of the type.
        /// </summary>
        public static IReadOnlyList<MemberInfo> GetFieldsAndProperties(
            this Type type, 
            string? name = null, 
            bool includeNonPublic = false)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy
                | (includeNonPublic ? BindingFlags.NonPublic : BindingFlags.Default);

            return
                type.GetProperties(flags)
                    .Where(p => p.CanRead
                    && (name == null || p.Name == name)
                    && p.GetIndexParameters().Length == 0)
                .Cast<MemberInfo>().Concat(
                    type.GetFields(flags)
                        .Where(f => name == null || f.Name == name))
                .ToReadOnly();
        }

        /// <summary>
        /// Gets the value of the field or property.
        /// </summary>
        public static bool TryGetFieldOrPropertyValue(
            this MemberInfo fieldOrProperty, object? instance, out object? value)
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
            this MemberInfo fieldOrProperty, object? instance)
        {
            return TryGetFieldOrPropertyValue(fieldOrProperty, instance, out var value)
                ? value
                : throw new InvalidOperationException($"Cannot get the value of '{fieldOrProperty.DeclaringType.Name}.{fieldOrProperty.Name}'.");
        }

        /// <summary>
        /// Sets the value of the field or property.
        /// </summary>
        public static bool TrySetFieldOrPropertyValue(
            this MemberInfo fieldOrProperty, object? instance, object? value)
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
            this MemberInfo fieldOrProperty, object? instance, object? value)
        {
            if (!fieldOrProperty.TrySetFieldOrPropertyValue(instance, value))
                throw new InvalidOperationException($"Cannot set the value of '{fieldOrProperty.DeclaringType.Name}.{fieldOrProperty.Name}'.");
        }

        /// <summary>
        /// Gets the instance field or property with the specified name.
        /// </summary>
        public static MemberInfo? FindFieldOrProperty(
            this Type type,
            string name,
            bool includeNonPublic = false)
        {
            return GetFieldsAndProperties(type, name, includeNonPublic).FirstOrDefault();
        }

        /// <summary>
        /// Finds the matching method declared on the specified type, or inherited from a base type.
        /// </summary>
        public static MethodInfo? FindMethod(
            this Type type, 
            string name, 
            IReadOnlyList<Type>? typeArguments, 
            IReadOnlyList<Type> parameterTypes)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy;

            var typeArgumentCount = typeArguments?.Count ?? 0;
            var typeArgumentsAsArray = typeArguments as Type[];

            foreach (var method in type.GetMethods(flags))
            {
                if (method.IsGenericMethodDefinition != (typeArgumentCount > 0))
                    continue;

                if (method.Name != name)
                    continue;

                if (method.IsGenericMethodDefinition && typeArguments != null)
                {
                    if (method.GetGenericArguments().Length != typeArgumentCount)
                        continue;

                    if (typeArgumentsAsArray == null)
                        typeArgumentsAsArray = typeArguments.ToArray();

                    var constructedMethod = method.MakeGenericMethod(typeArgumentsAsArray);

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
        public static ConstructorInfo? FindConstructor(
            this Type type, 
            IReadOnlyList<Type> parameterTypes)
        {
            return type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(c => ParametersMatch(c.GetParameters(), parameterTypes));
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

        // holds a delegate to the runtime implemented API
        private static Func<Type, object>? _fnGetUninitializedObject;

        /// <summary>
        /// Gets an unitialized instance of an object of the specified type.
        /// </summary>
        public static object GetUninitializedObject(this Type type)
        {
            if (_fnGetUninitializedObject == null)
            {
                var a = typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetTypeInfo().Assembly;
                var fs = a.DefinedTypes.FirstOrDefault(t => t.FullName == "System.Runtime.Serialization.FormatterServices");
                var guo = fs?.DeclaredMethods.FirstOrDefault(m => m.Name == nameof(GetUninitializedObject));
                if (guo == null)
                    throw new NotSupportedException($"The runtime does not support the '{nameof(GetUninitializedObject)}' API.");
                System.Threading.Interlocked.CompareExchange(ref _fnGetUninitializedObject, (Func<Type, object>)guo.CreateDelegate(typeof(Func<Type, object>)), null);
            }

            return _fnGetUninitializedObject(type);
        }
    }
}
