// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace IQToolkit.Data.Dynamic
{
    using Common;
    using Mapping;
    using System.IO;

    /// <summary>
    /// API's to construct a <see cref="DbEntityProvider"/>'s either by recognizing the connection string, 
    /// or by dynamically loading a provider's implementation library based on a naming pattern.
    /// </summary>
    public static class DynamicProvider
    {
        /// <summary>
        /// Create a <see cref="QueryMapping"/> from the specified mapping string.
        /// </summary>
        /// <param name="mapping">The path to an <see cref="XmlMapping"/> mapping file, 
        /// or the name of a context class with mapping attributes to initialize a <see cref="AttributeMapping"/>.</param>
        public static QueryMapping CreateMapping(string mapping)
        {
            if (mapping != null)
            {
                if (mapping == "Attribute" || mapping == "AttributeMapping")
                {
                    return new AttributeMapping();
                }

                Type type = FindLoadedType(mapping);
                if (type != null)
                {
                    if (type.IsSubclassOf(typeof(QueryMapping)))
                    {
                        // the type of a QueryMapping?
                        return (QueryMapping)Activator.CreateInstance(type);
                    }
                    else
                    {
                        // assume this is a context type for attribute mapping
                        return new AttributeMapping(type);
                    }
                }

                // if this is a file name, try to load it as an XmlMapping.
                if (File.Exists(mapping))
                {
                    return XmlMapping.FromXml(File.ReadAllText(mapping));
                }
            }

            // no mapping identified
            return null;
        }

        /// <summary>
        /// Create a new <see cref="DbEntityProvider"/> from the specified <see cref="EntityProviderSettings"/>.
        /// If not specified, it uses the running application's current settings.
        /// </summary>
        public static DbEntityProvider CreateFromSettings(EntityProviderSettings settings = null)
        {
            settings = settings ?? EntityProviderSettings.FromApplicationSettings();

            DbEntityProvider provider = settings.Provider != null
                ? Create(settings.Provider, settings.Connection)
                : Create(settings.Connection);

            if (settings.Mapping != null)
            {
                provider = provider.WithMapping(CreateMapping(settings.Mapping));
            }

            return provider;
        }

        /// <summary>
        /// Create a new <see cref="DbEntityProvider"/>.
        /// </summary>
        /// <param name="connectionStringOrDatabaseFile">A connection string or path to a database file.</param>
        public static DbEntityProvider Create(string connectionStringOrDatabaseFile)
        {
            return Create(InferProviderName(connectionStringOrDatabaseFile), connectionStringOrDatabaseFile);
        }

        /// <summary>
        /// Create a new <see cref="DbEntityProvider"/>.
        /// </summary>
        /// <param name="providerName">The simple name of an assembly that implements a query provider.</param>
        /// <param name="connectionStringOrDatabaseFile">A connection string or path to a database file.</param>
        public static DbEntityProvider Create(string providerName, string connectionStringOrDatabaseFile)
        {
            if (providerName == null)
                throw new ArgumentNullException(nameof(providerName));
            if (connectionStringOrDatabaseFile == null)
                throw new ArgumentNullException(nameof(connectionStringOrDatabaseFile));

            Type providerType = GetProviderType(providerName);
            if (providerType == null)
                throw new InvalidOperationException(string.Format("Unable to find query provider '{0}'", providerName));

            return Create(providerType, connectionStringOrDatabaseFile);
        }

        /// <summary>
        /// Infers the known query provider by examining the connection string.
        /// </summary>
        private static string InferProviderName(string connectionString)
        {
            var clower = connectionString.ToLower();

            // try sniffing connection to figure out provider
            if (clower.Contains(".mdb") || clower.Contains(".accdb"))
            {
                return "IQToolkit.Data.Access";
            }
            else if (clower.Contains(".sdf"))
            {
                return "IQToolkit.Data.SqlServerCe";
            }
            else if (clower.Contains(".sl3") || clower.Contains(".db3"))
            {
                return "IQToolkit.Data.SQLite";
            }
            else if (clower.Contains(".mdf"))
            {
                return "IQToolkit.Data.SqlClient";
            }
            else
            {
                throw new InvalidOperationException(string.Format("Query provider not specified and cannot be inferred."));
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="DbEntityProvider"/>.
        /// </summary>
        /// <param name="queryProviderType">The query provider type.</param>
        /// <param name="connectionStringOrDatabaseFile">A connection string or path to a database file.</param>
        public static DbEntityProvider Create(Type queryProviderType, string connectionStringOrDatabaseFile)
        {
            if (queryProviderType == null)
                throw new ArgumentNullException(nameof(queryProviderType));
            if (connectionStringOrDatabaseFile == null)
                throw new ArgumentNullException(nameof(connectionStringOrDatabaseFile));

            Type adoConnectionType = GetAdoConnectionType(queryProviderType);
            if (adoConnectionType == null)
                throw new InvalidOperationException(string.Format("Unable to deduce DbConnection type for '{0}'", queryProviderType.Name));

            DbConnection connection = (DbConnection)Activator.CreateInstance(adoConnectionType);

            // is the connection string just a filename?
            if (!connectionStringOrDatabaseFile.Contains('='))
            {
                MethodInfo gcs = queryProviderType.GetMethod("GetConnectionString", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
                if (gcs != null)
                {
                    var getConnectionString = (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), gcs);
                    connectionStringOrDatabaseFile = getConnectionString(connectionStringOrDatabaseFile);
                }
            }

            connection.ConnectionString = connectionStringOrDatabaseFile;

            return (DbEntityProvider)Activator.CreateInstance(queryProviderType, new object[] { connection, null, null });
        }

        private static Type GetAdoConnectionType(Type providerType)
        {
            // sniff constructors 
            foreach (var con in providerType.GetConstructors())
            {
                foreach (var arg in con.GetParameters())
                {
                    if (arg.ParameterType.IsSubclassOf(typeof(DbConnection)))
                        return arg.ParameterType;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the <see cref="Type"/> of the <see cref="EntityProvider"/> given the name of the provider assembly.
        /// </summary>
        /// <param name="providerAssemblyName">The name of the provider assembly.</param>
        public static Type GetProviderType(string providerAssemblyName)
        {
            if (!string.IsNullOrEmpty(providerAssemblyName))
            {
                var providers = FindInstancesInAssembly(typeof(EntityProvider), providerAssemblyName).ToList();

                Type type = null;

                if (providers.Count == 1)
                {
                    type = providers[0];
                }
                else if (providers.Count > 1)
                {
                    // if more than one, pick the provider in the same namespace that matches the assembly name
                    type = providers.Where(t => string.Compare(t.Namespace, providerAssemblyName, true) == 0)
                            .FirstOrDefault();
                }

                if (type != null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException(string.Format("Unable to find query provider '{0}'", providerAssemblyName));
        }

        private static Type FindLoadedType(string typeName)
        {
            foreach (var assem in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assem.GetType(typeName, false, true);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static IEnumerable<Type> FindInstancesInAssembly(Type type, string assemblyName)
        {
            Assembly assembly = GetOrLoadAssembly(assemblyName);
            if (assembly != null)
            {
                foreach (var atype in assembly.GetTypes())
                {
                    // find types in the same namespace 
                    if (type.IsAssignableFrom(atype))
                    {
                        yield return atype;
                    }
                }
            }
        }

        private static Assembly GetOrLoadAssembly(string assemblyName)
        {
            foreach (var assem in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assem.FullName.Contains(assemblyName))
                {
                    return assem;
                }
            }

            return Load(assemblyName + ".dll");
        }

        private static Assembly Load(string assemblyName)
        {
            // try to load it.
            try
            {
                var fullName = Path.GetFullPath(assemblyName);
                return Assembly.LoadFrom(fullName);
            }
            catch
            {
            }
            return null;
        }
    }
}