// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Data.AnsiSql
{
    /// <summary>
    /// ANSI SQL scalar data types.
    /// </summary>
    public enum AnsiSqlType
    {
        /// <summary>
        /// A 64-bit signed integer.
        /// </summary>
        BigInt = 0,

        /// <summary>
        /// A fixed-length stream of binary data ranging between 1 and 8,000 bytes.
        /// </summary>
        Binary = 1,

        /// <summary>
        /// An unsigned numeric value that can be 0, 1, or null.
        /// </summary>
        Bit = 2,

        /// <summary>
        ///  A fixed-length stream of non-Unicode characters ranging between 1 and 8,000 characters.
        Char = 3,

        /// <summary>
        /// Date and time data ranging in value from January 1, 1753 to December 31, 9999 to an accuracy of 3.33 milliseconds.
        /// </summary>
        DateTime = 4,

        /// <summary>
        /// A fixed precision and scale numeric value between -10 38 -1 and 10 38 -1.
        /// </summary>
        Decimal = 5,

        /// <summary>
        /// A floating point number within the range of -1.79E +308 through 1.79E +308.
        /// </summary>
        Float = 6,

        /// <summary>
        /// A variable-length stream of binary data ranging from 0 to 2 31 -1 (or 2,147,483,647) bytes.
        /// </summary>
        Image = 7,

        /// <summary>
        /// A 32-bit signed integer.
        /// </summary>
        Integer = 8,

        /// <summary>
        /// A currency value ranging from -2 63 (or -9,223,372,036,854,775,808)
        /// to 2 63 -1 (or +9,223,372,036,854,775,807) with an accuracy to a ten-thousandth
        /// of a currency unit.
        /// </summary>
        Money = 9,

        /// <summary>
        /// A fixed-length stream of Unicode characters ranging between 1 and 4,000 characters.
        /// </summary> 
        NChar = 10,

        /// <summary>
        /// System.String. A variable-length stream of Unicode data with a maximum length
        /// of 2 30 - 1 (or 1,073,741,823) characters.
        /// </summary>
        NText = 11,

        /// <summary>
        /// A variable-length stream of Unicode characters ranging between
        /// 1 and 4,000 characters. Implicit conversion fails if the string is greater than
        /// 4,000 characters. Explicitly set the object when working with strings longer
        /// than 4,000 characters. Use System.Data.SqlDbType.NVarChar when the database column
        /// is nvarchar(max).
        /// </summary>
        NVarChar = 12,

        /// <summary>
        /// A floating point number within the range of -3.40E +38 through 3.40E +38.
        /// </summary>
        Real = 13,

        /// <summary>
        /// A globally unique identifier (or GUID).
        /// </summary>
        UniqueIdentifier = 14,

        /// <summary>
        /// Date and time data ranging in value from January 1, 1900 to
        /// June 6, 2079 to an accuracy of one minute.
        /// </summary>
        SmallDateTime = 15,

        /// <summary>
        /// A 16-bit signed integer.
        /// </summary>
        SmallInt = 16,

        /// <summary>
        /// A currency value ranging from -214,748.3648 to +214,748.3647
        /// with an accuracy to a ten-thousandth of a currency unit.
        /// </summary>
        SmallMoney = 17,

        /// <summary>
        /// A variable-length stream of non-Unicode data with a maximum length
        /// of 2 31 -1 (or 2,147,483,647) characters.
        /// </summary>
        Text = 18,

        /// <summary>
        /// Automatically generated binary numbers, which
        /// are guaranteed to be unique within a database. timestamp is used typically as
        /// a mechanism for version-stamping table rows. The storage size is 8 bytes.
        /// </summary>
        Timestamp = 19,

        /// <summary>
        /// An 8-bit unsigned integer.
        /// </summary>
        TinyInt = 20,

        /// <summary>
        /// A variable-length stream of binary data ranging
        /// between 1 and 8,000 bytes. Implicit conversion fails if the byte array is greater
        /// than 8,000 bytes. Explicitly set the object when working with byte arrays larger
        /// than 8,000 bytes.
        /// </summary>
        VarBinary = 21,

        /// <summary>
        /// A variable-length stream of non-Unicode characters ranging between
        /// 1 and 8,000 characters. Use System.Data.SqlDbType.VarChar when the database column
        /// is varchar(max).
        /// </summary>
        VarChar = 22,

        /// <summary>
        /// A special data type that can contain numeric, string, binary,
        /// or date data as well as the SQL Server values Empty and Null, which is assumed
        /// if no other type is declared.
        /// </summary>
        Variant = 23,

        /// <summary>
        /// An XML value. 
        /// </summary>
        Xml = 25,

        /// <summary>
        /// A SQL Server user-defined type (UDT).
        /// </summary>
        Udt = 29,

        /// <summary>
        /// A special data type for specifying structured data contained in table-valued parameters.
        /// </summary>
        Structured = 30,

        /// <summary>
        /// Date data ranging in value from January 1,1 AD through December 31, 9999 AD.
        /// </summary>
        Date = 31,

        /// <summary>
        /// Time data based on a 24-hour clock. Time value range is 00:00:00 through 23:59:59.9999999.
        /// </summary>
        Time = 32,

        /// <summary>
        /// Date and time data. Date value range is from January 1,1 AD through December
        /// 31, 9999 AD. Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy
        /// of 100 nanoseconds.
        /// </summary>
        DateTime2 = 33,

        /// <summary>
        /// Date and time data with time zone awareness. 
        /// Date value range is from January 1,1 AD through December 31, 9999 AD. 
        /// Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds. 
        /// Time zone value range is -14:00 through +14:00.
        /// </summary>
        DateTimeOffset = 34
    }
}