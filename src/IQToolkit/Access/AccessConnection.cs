// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.IO;

namespace IQToolkit.Access
{
    public static class AccessConnection
    {
        /// <summary>
        /// Gets a connection string appropriate for connecting to the Access database file.
        /// </summary>
        public static string GetOdbcConnectionString(string filePath)
        {
            var fullPath = Path.GetFullPath(filePath);
            return string.Format("Driver={{Microsoft Access Driver (*.mdb, *.accdb)}};DBQ={0}", fullPath);
        }

        /// <summary>
        /// Gets a connection string appropriate for openning the specified dadtabase file.
        /// </summary>
        public static string GetOleDbConnectionString(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            var fullPath = Path.GetFullPath(filePath);

            if (string.Compare(extension, ".mdb", true) == 0)
            {
                return GetOleDbConnectionString(AccessOleDbProvider2000, filePath);
            }
            else if (string.Compare(extension, ".accdb", true) == 0)
            {
                return GetOleDbConnectionString(AccessOleDbProvider2007, filePath);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Unrecognized file extension on database file '{0}'", filePath));
            }
        }

        private static string GetOleDbConnectionString(string provider, string databaseFile)
        {
            return string.Format("Provider={0};ole db services=0;Data Source={1}", provider, databaseFile);
        }

        public static readonly string AccessOleDbProvider2000 = "Microsoft.Jet.OLEDB.4.0";
        public static readonly string AccessOleDbProvider2007 = "Microsoft.ACE.OLEDB.12.0";

    }
}