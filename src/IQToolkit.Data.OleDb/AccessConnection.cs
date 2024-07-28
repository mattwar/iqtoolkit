using System.IO;
using System;
// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Data.OleDb
{
    public static class AccessConnection
    {
        /// <summary>
        /// Gets a connection string appropriate for connecting to the Access database file.
        /// </summary>
        public static string GetConnectionString(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            switch (extension)
            {
                case ".mdb":
                    return GetConnectionString(AccessOleDbProvider2000, filePath);
                case ".accdb":
                    return GetConnectionString(AccessOleDbProvider2007, filePath);
                default:
                    throw new InvalidOperationException(string.Format("Unrecognized file extension on database file '{0}'", filePath));
            }
        }

        public static string GetConnectionString(string provider, string filePath)
        {
            var fullPath = Path.GetFullPath(filePath);
            return string.Format("Provider={0};ole db services=0;Data Source={1}", provider, fullPath);
        }

        public static readonly string AccessOleDbProvider2000 = "Microsoft.Jet.OLEDB.4.0";
        public static readonly string AccessOleDbProvider2007 = "Microsoft.ACE.OLEDB.12.0";
    }
}