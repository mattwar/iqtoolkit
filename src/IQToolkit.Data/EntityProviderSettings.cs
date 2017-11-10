// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Data
{
    /// <summary>
    /// entity provider settings found in the application configuration (app.config)
    /// </summary>
    public class EntityProviderSettings
    {
        /// <summary>
        /// The <see cref="EntityProvider"/> assembly name.
        /// </summary>
        public string Provider { get; }

        /// <summary>
        /// A string that designates the database connection or file name.
        /// </summary>
        public string Connection { get; }

        /// <summary>
        /// A string that designates the mapping to use for the <see cref="EntityProvider"/>.
        /// </summary>
        public string Mapping { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="EntityProviderSettings"/>
        /// </summary>
        /// <param name="provider">The <see cref="EntityProvider"/> assembly name.</param>
        /// <param name="connection">A string that designates the database connection or file name.</param>
        /// <param name="mapping">A string that designates the mapping to use for the <see cref="EntityProvider"/>.</param>
        public EntityProviderSettings(string provider, string connection, string mapping)
        {
            this.Provider = provider;
            this.Connection = connection;
            this.Mapping = mapping;
        }

        /// <summary>
        /// Gets settings from the application configuration (app.config file)
        /// </summary>
        public static EntityProviderSettings FromApplicationSettings()
        {
            throw new System.NotSupportedException();
#if false
            return new EntityProviderSettings(
                provider: System.Configuration.ConfigurationManager.AppSettings["Provider"],
                connection: System.Configuration.ConfigurationManager.AppSettings["Connection"],
                mapping: System.Configuration.ConfigurationManager.AppSettings["Mapping"]);
#endif
        }
    }
}