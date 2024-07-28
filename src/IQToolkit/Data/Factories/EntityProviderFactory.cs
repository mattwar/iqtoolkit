using System;

namespace IQToolkit.Data.Factories
{
    public abstract class EntityProviderFactory
    {
        /// <summary>
        /// The name of the factory.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Creates an <see cref="EntityProvider"/> for the connection string, 
        /// if the connection string is compatible.
        /// </summary>
        public virtual bool TryCreateProviderForConnection(string connectionString, out EntityProvider provider)
        {
            provider = default!;
            return false;
        }

        /// <summary>
        /// Creates an <see cref="EntityProvider"/> for the file path, 
        /// if the file path is compatible.
        /// </summary>
        public virtual bool TryCreateProviderForFilePath(string filePath, out EntityProvider provider)
        {
            provider = default!;
            return false;
        }

        /// <summary>
        /// Creates an <see cref="EntityProvider"/> for the connection string, 
        /// if the connection string is compatible.
        /// </summary>
        public EntityProvider CreateProviderForConnection(string connectionString)
        {
            if (TryCreateProviderForConnection(connectionString, out var provider))
                return provider;
            throw new InvalidOperationException($"Cannot create provider for the connection string.");
        }

        public EntityProvider CreateProviderForFilePath(string filePath)
        {
            if (TryCreateProviderForFilePath(filePath, out var provider))
                return provider;
            throw new InvalidOperationException($"Cannot create provider for the file path.");
        }
    }
}