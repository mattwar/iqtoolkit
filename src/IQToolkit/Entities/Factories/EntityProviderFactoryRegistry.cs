// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace IQToolkit.Entities.Factories
{
    using Utils;

    public class EntityProviderFactoryRegistry : EntityProviderFactory
    {
        private ImmutableList<EntityProviderFactory> _factories =
            ImmutableList<EntityProviderFactory>.Empty;

        private ImmutableDictionary<string, EntityProviderFactory> _nameToFactoryMap =
            ImmutableDictionary<string, EntityProviderFactory>.Empty;

        private ImmutableDictionary<Type, EntityProviderFactory> _typeToFactoryMap =
            ImmutableDictionary<Type, EntityProviderFactory>.Empty;

        private readonly Lazy<bool> _factoriesRegistered;

        private EntityProviderFactoryRegistry()
        {
            _factoriesRegistered = new Lazy<bool>(() =>
            {
                RegisterFactories();
                return true;
            });
        }

        public static readonly EntityProviderFactoryRegistry Singleton =
            new EntityProviderFactoryRegistry();

        private void RegisterFactories()
        {
            var thisAssemblyName =
                Assembly.GetExecutingAssembly().FullName;

            // find all currently loaded assemblies that immediately reference this assembly
            var toolkitReferencingAssemblies =
                AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => ReferencesAssembly(a, thisAssemblyName))
                    .ToList();

            // broaden to include directly referenced assemblies.
            var broadenedAssemblyNames = toolkitReferencingAssemblies
                .SelectMany(a => a.GetReferencedAssemblies())
                .Distinct()
                .ToList();

            // force load broadened assemblies
            foreach (var assembly in broadenedAssemblyNames)
            {
                AppDomain.CurrentDomain.Load(assembly);
            }

            // find all entity provider factory types and register them
            var factoryTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(EntityProviderFactory).IsAssignableFrom(t))
                .ToList();

            foreach (var factoryType in factoryTypes)
            {
                Register(factoryType);
            }
        }

        private static bool ReferencesAssembly(Assembly assembly, string assemblyName)
        {
            return assembly.GetReferencedAssemblies().Any(an => an.FullName == assemblyName);
        }

        public override string Name => "Registry";

        public override bool TryCreateProviderForConnection(string connectionString, out IEntityProvider provider)
        {
            CheckFactoriesLoaded();

            var factories = _factories;
            foreach (var factory in factories)
            {
                if (factory.TryCreateProviderForConnection(connectionString, out provider))
                    return true;
            }

            provider = default!;
            return false;
        }

        public override bool TryCreateProviderForFilePath(string filePath, out IEntityProvider provider)
        {
            CheckFactoriesLoaded();

            var factories = _factories;
            foreach (var factory in factories)
            {
                if (factory.TryCreateProviderForFilePath(filePath, out provider))
                    return true;
            }

            provider = default!;
            return false;
        }

        private void CheckFactoriesLoaded()
        {
            var _ = _factoriesRegistered.Value;
        }

        /// <summary>
        /// Register a <see cref="EntityProviderFactory"/> instance.
        /// </summary>
        public void Register(EntityProviderFactory factory)
        {
            var factoryType = factory.GetType();

            if (ImmutableInterlocked.TryAdd(ref _nameToFactoryMap, factory.Name, factory))
            {
                ImmutableInterlocked.TryAdd(ref _typeToFactoryMap, factoryType, factory);
                ImmutableInterlocked.Update(ref _factories, facts => facts.Add(factory));
            }
        }

        /// <summary>
        /// Register a <see cref="EntityProviderFactory"/> by type.
        /// </summary>
        public void Register(Type factoryType)
        {
            if (!_typeToFactoryMap.ContainsKey(factoryType)
                && factoryType != this.GetType())
            {
                try
                {
                    var factory = (EntityProviderFactory)Activator.CreateInstance(factoryType);
                    Register(factory);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Gets the factory for the name, if available.
        /// </summary>
        public bool TryGetFactory(string factoryName, out EntityProviderFactory factory)
        {
            CheckFactoriesLoaded();
            return _nameToFactoryMap.TryGetValue(factoryName, out factory!);
        }
    }
}