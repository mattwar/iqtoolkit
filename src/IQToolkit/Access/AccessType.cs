// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Access
{
    /// <summary>
    /// Microsoft Access SQL data types
    /// </summary>
    public enum AccessType
    {
        /// <summary>
        /// Any type of data may be stored in a field of this type. 
        /// No translation of the data (for example, to text) is made. 
        /// How the data is input in a binary field dictates how it will appear as output.
        /// </summary>
        Binary,

        /// <summary>
        /// Yes and No values and fields that contain only one of two values.
        /// </summary>
        Bit,

        /// <summary>
        /// An integer value between 0 and 255.
        /// </summary>
        TinyInt,

        /// <summary>
        /// An autoincrement integer.
        /// </summary>
        Counter,

        /// <summary>
        /// A scaled integer between – 922,337,203,685,477.5808 and 922,337,203,685,477.5807.
        /// </summary>
        Money,

        /// <summary>
        /// A date or time value between the years 100 and 9999.
        /// </summary>
        DateTime,

        /// <summary>
        /// A unique identification number used with remote procedure calls. (GUID)
        /// </summary>
        UniqueIdentifier,

        /// <summary>
        /// A single-precision floating-point value with a range of – 3.402823E38 to – 1.401298E-45 for negative values, 
        /// 1.401298E-45 to 3.402823E38 for positive values, and 0.
        /// </summary>
        Real,

        /// <summary>
        /// A double-precision floating-point value with a range of – 1.79769313486232E308 to – 4.94065645841247E-324 for negative values, 
        /// 4.94065645841247E-324 to 1.79769313486232E308 for positive values, and 0.
        /// </summary>
        Float,

        /// <summary>
        /// A short integer between –32,768 and 32,767.
        /// </summary>
        SmallInt,

        /// <summary>
        /// A long integer between – 2,147,483,648 and 2,147,483,647.
        /// </summary>
        Integer,

        /// <summary>
        /// An exact numeric data type that holds values from 1028 - 1 through - 1028 - 1. 
        /// You can define both precision (1 - 28) and scale (0 - defined precision). 
        /// The default precision and scale are 18 and 0, respectively.
        /// </summary>
        Decimal,

        /// <summary>
        /// Zero to a maximum of 2.14 gigabytes.
        /// </summary>
        Text,

        /// <summary>
        /// Zero to a maximum of 2.14 gigabytes.
        /// </summary>
        Image,

        /// <summary>
        /// Zero to 255 characters.
        /// </summary>
        Char
    }
}