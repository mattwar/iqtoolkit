// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Reflection;

namespace IQToolkit.Utils
{
    public class TypeConverter
    {
        public static readonly TypeConverter Default =
            new TypeConverter();

        public virtual object? Convert(object? value, Type type)
        {
            if (value == null)
            {
                return TypeHelper.GetDefault(type);
            }

            type = TypeHelper.GetNonNullableType(type);
            Type vtype = value.GetType();

            if (type != vtype)
            {
                if (type.GetTypeInfo().IsEnum)
                {
                    if (vtype == typeof(string)
                        && Enum.TryParse(type, (string)value, out var enumValue))
                    {
                        return enumValue;
                    }
                    else
                    {
                        Type utype = Enum.GetUnderlyingType(type);

                        if (utype != vtype)
                        {
                            value = System.Convert.ChangeType(value, utype);
                        }

                        return Enum.ToObject(type, value);
                    }
                }

                return System.Convert.ChangeType(value, type);
            }

            return value;
        }
    }
}