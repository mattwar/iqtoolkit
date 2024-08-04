// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Sessions
{
    using Mapping;
    using System.Collections;
    using Utils;

    /// <summary>
    /// Implements the <see cref="IEntitySession"/> contract over an <see cref="EntityProvider"/>.
    /// </summary>
    public class EntitySession : IEntitySession
    {
        private readonly IEntityProvider _provider;
        private readonly SessionProvider _sessionProvider;
        private readonly Dictionary<MappedEntity, ISessionTable> _tables;

        /// <summary>
        /// Construct a <see cref="EntitySession"/>
        /// </summary>
        public EntitySession(IEntityProvider provider)
        {
            _provider = provider;
            _sessionProvider = new SessionProvider(this, provider);
            _tables = new Dictionary<MappedEntity, ISessionTable>();
        }

        public IEntityProvider Provider => _sessionProvider;

        protected IEnumerable<ISessionTable> GetTables()
        {
            return _tables.Values;
        }

        /// <summary>
        /// Gets the <see cref="ISessionTable"/> for the corresponding database table.
        /// </summary>
        /// <param name="entityType">The type of the entities held by this table.</param>
        /// <param name="entityId">The id of the entity in the mapping.
        /// If unspecified, the provider will infer the id from the element type.</param>
        public ISessionTable GetTable(Type entityType, string? entityId = null)
        {
            return this.GetTable(
                _sessionProvider.Mapping.GetEntity(entityType, entityId)
                );
        }

        /// <summary>
        /// Gets the <see cref="ISessionTable"/> for the corresponding database table.
        /// </summary>
        /// <param name="entityId">The id of the entity in the mapping.
        /// If unspecified, the provider will infer the id from the element type.</param>
        public ISessionTable<TEntity> GetTable<TEntity>(string? entityId = null)
            where TEntity : class
        {
            return (ISessionTable<TEntity>)this.GetTable(typeof(TEntity), entityId);
        }

        protected ISessionTable GetTable(MappedEntity entity)
        {
            ISessionTable table;
            if (!_tables.TryGetValue(entity, out table))
            {
                table = this.CreateTable(entity);
                _tables.Add(entity, table);
            }
            return table;
        }

        private object OnEntityMaterialized(MappedEntity entity, object instance)
        {
            IEntitySessionTable table = (IEntitySessionTable)this.GetTable(entity);
            return table.OnEntityMaterialized(instance);
        }

        interface IEntitySessionTable : ISessionTable
        {
            object OnEntityMaterialized(object instance);
            MappedEntity Entity { get; }
        }

        private abstract class SessionTable<TEntity> 
            : Query<TEntity>, ISessionTable<TEntity>, ISessionTable, IEntitySessionTable
            where TEntity : class
        {
            public IEntitySession Session { get; }
            public MappedEntity Entity { get; }
            public IUpdatableEntityTable<TEntity> Table { get; }

            public SessionTable(EntitySession session, MappedEntity entity)
                : base(session._sessionProvider, typeof(ISessionTable<TEntity>))
            {
                this.Session = session;
                this.Entity = entity;
                this.Table = this.Session.Provider.GetTable<TEntity>(entity.EntityId);
            }

            public new IEntityProvider Provider => this.Session.Provider;
            public Type EntityType => typeof(TEntity);
            public string EntityId => this.Entity.EntityId;
            public TEntity? GetById(object id) => this.Table.GetById(id);
            object? IEntityTable.GetById(object id) => this.GetById(id);

            public virtual object OnEntityMaterialized(object instance)
            {
                return instance;
            }

            public virtual void SetSubmitAction(TEntity instance, SubmitAction action)
            {
                throw new NotImplementedException();
            }

            void ISessionTable.SetSubmitAction(object instance, SubmitAction action)
            {
                this.SetSubmitAction((TEntity)instance, action);
            }

            public virtual SubmitAction GetSubmitAction(TEntity instance)
            {
                throw new NotImplementedException();
            }

            SubmitAction ISessionTable.GetSubmitAction(object instance)
            {
                return this.GetSubmitAction((TEntity)instance);
            }
        }

        /// <summary>
        /// A provider that wraps an underlying provider to intercept entity creation.
        /// </summary>
        private class SessionProvider 
            : QueryProvider, IEntityProvider
        {
            private readonly EntitySession _session;
            private readonly SessionExecutor _executor;

            private readonly IEntityProvider _provider;

            public SessionProvider(
                EntitySession session, 
                IEntityProvider provider)
            {
                _session = session;
                _executor = new SessionExecutor(session, provider.Executor);
                _provider = provider;
            }

            public QueryExecutor Executor => _executor;
            public QueryLanguage Language => _provider.Language;
            public EntityMapping Mapping => _provider.Mapping;
            public QueryPolicy Policy => _provider.Policy;
            public TextWriter? Log => _provider.Log;
            public QueryCache? Cache => _provider.Cache;
            public QueryOptions Options => _provider.Options;

            #region IEntityProvider
            IEntityProvider IEntityProvider.WithLanguage(QueryLanguage language) =>
                new SessionProvider(_session, _provider.WithLanguage(language));

            IEntityProvider IEntityProvider.WithMapping(EntityMapping mapping) =>
                new SessionProvider(_session, _provider.WithMapping(mapping));

            IEntityProvider IEntityProvider.WithPolicy(QueryPolicy policy) =>
                new SessionProvider(_session, _provider.WithPolicy(policy));

            IEntityProvider IEntityProvider.WithLog(TextWriter? log) =>
                new SessionProvider(_session, _provider.WithLog(log));

            IEntityProvider IEntityProvider.WithCache(QueryCache? cache) =>
                new SessionProvider(_session, _provider.WithCache(cache));

            IEntityProvider IEntityProvider.WithOptions(QueryOptions options) =>
                new SessionProvider(_session, _provider.WithOptions(options: options));
            #endregion

            public override object? Execute(Expression expression)
            {
                return _provider.Execute(expression);
            }

            public IUpdatableEntityTable<TEntity> GetTable<TEntity>(string? tableId)
                where TEntity : class
            {
                return _provider.GetTable<TEntity>(tableId);
            }

            public IUpdatableEntityTable GetTable(Type type, string? tableId)
            {
                return _provider.GetTable(type, tableId);
            }

            public bool CanBeEvaluatedLocally(Expression expression)
            {
                return _provider.CanBeEvaluatedLocally(expression);
            }

            public bool CanBeParameter(Expression expression)
            {
                return _provider.CanBeParameter(expression);
            }

            public object? ExecutePlan(QueryPlan plan)
            {
                return _provider.ExecutePlan(plan);
            }

            public QueryPlan GetQueryPlan(Expression expression)
            {
                return _provider.GetQueryPlan(expression);
            }
        }

        private class SessionExecutor : QueryExecutor
        {
            private readonly EntitySession _session;
            private readonly QueryExecutor _executor;

            public SessionExecutor(EntitySession session, QueryExecutor executor)
            {
                _session = session;
                _executor = executor;
            }

            public override TypeConverter Converter => _executor.Converter;
            public override QueryTypeSystem TypeSystem => _executor.TypeSystem;
            public override TextWriter? Log => _executor.Log;

            protected override QueryExecutor Construct(TypeConverter converter, QueryTypeSystem typeSystem, TextWriter? log)
            {
                return new SessionExecutor(
                    _session, 
                    _executor
                        .WithConverter(converter)
                        .WithTypeSystem(typeSystem)
                        .WithLog(log)
                    );
            }

            public override IEnumerable<T> Execute<T>(QueryCommand command, Func<FieldReader, T> fnProjector, MappedEntity entity, object[] paramValues)
            {
                return _executor.Execute<T>(command, Wrap(fnProjector, entity), entity, paramValues);
            }

            public override IEnumerable<int> ExecuteBatch(QueryCommand query, IEnumerable<object[]> paramSets, int batchSize, bool stream)
            {
                return _executor.ExecuteBatch(query, paramSets, batchSize, stream);
            }

            public override IEnumerable<T> ExecuteBatch<T>(QueryCommand query, IEnumerable<object[]> paramSets, Func<FieldReader, T> fnProjector, MappedEntity entity, int batchSize, bool stream)
            {
                return _executor.ExecuteBatch<T>(query, paramSets, Wrap(fnProjector, entity), entity, batchSize, stream);
            }

            public override IEnumerable<T> ExecuteDeferred<T>(QueryCommand query, Func<FieldReader, T> fnProjector, MappedEntity entity, object[] paramValues)
            {
                return _executor.ExecuteDeferred<T>(query, Wrap(fnProjector, entity), entity, paramValues);
            }

            public override int ExecuteCommand(QueryCommand query, object?[]? paramValues)
            {
                return _executor.ExecuteCommand(query, paramValues);
            }

            private Func<FieldReader, T> Wrap<T>(Func<FieldReader, T> fnProjector, MappedEntity entity)
            {
                Func<FieldReader, T> fnWrapped = 
                    (fr) => (T)_session.OnEntityMaterialized(entity, fnProjector(fr)!);
                return fnWrapped;
            }
        }

        /// <summary>
        /// Submit changes made to entity instances back to the database, as a single transaction.
        /// </summary>
        public virtual void SubmitChanges()
        {
            _provider.Executor.DoTransacted(
                delegate
                {
                    var submitted = new List<TrackedItem>();

                    // do all submit actions
                    foreach (var item in this.GetOrderedItems())
                    {
                        if (item.Table.SubmitChanges(item))
                        {
                            submitted.Add(item);
                        }
                    }

                    // on completion, accept changes
                    foreach (var item in submitted)
                    {
                        item.Table.AcceptChanges(item);
                    }
                }
            );
        }

        protected virtual ISessionTable CreateTable(MappedEntity entity)
        {
            return (ISessionTable)Activator.CreateInstance(typeof(TrackedTable<>).MakeGenericType(entity.Type), new object[] { this, entity });
        }

        private interface ITrackedTable : IEntitySessionTable
        {
            object? GetFromCacheById(object key);
            IEnumerable<TrackedItem> TrackedItems { get; }
            TrackedItem? GetTrackedItem(object instance);
            bool SubmitChanges(TrackedItem item);
            void AcceptChanges(TrackedItem item);
        }

        private class TrackedTable<TEntity> : SessionTable<TEntity>, ITrackedTable
            where TEntity : class
        {
            private readonly Dictionary<TEntity, TrackedItem> _tracked;
            private readonly Dictionary<object, TEntity> _identityCache;

            public TrackedTable(EntitySession session, MappedEntity entity)
                : base(session, entity)
            {
                _tracked = new Dictionary<TEntity, TrackedItem>();
                _identityCache = new Dictionary<object, TEntity>();
            }

            IEnumerable<TrackedItem> ITrackedTable.TrackedItems
            {
                get { return _tracked.Values; }
            }

            TrackedItem? ITrackedTable.GetTrackedItem(object instance)
            {
                TrackedItem ti;
                if (_tracked.TryGetValue((TEntity)instance, out ti))
                    return ti;
                return null;
            }

            object? ITrackedTable.GetFromCacheById(object key)
            {
                TEntity cached;
                _identityCache.TryGetValue(key, out cached);
                return cached;
            }

            private bool SubmitChanges(TrackedItem item)
            {
                switch (item.State)
                {
                    case SubmitAction.Delete:
                        this.Table.Delete(item.Instance);
                        return true;

                    case SubmitAction.Insert:
                        var generatedKeySelector = this.GetGeneratedKeySelector();

                        if (generatedKeySelector != null)
                        {
                            var keyValues = this.Table.Insert((TEntity)item.Instance, generatedKeySelector);
                            ApplyGeneratedKeyValues(item, keyValues);
                        }
                        else
                        {
                            this.Table.Insert((TEntity)item.Instance);
                        }

                        return true;

                    case SubmitAction.InsertOrUpdate:
                        this.Table.InsertOrUpdate(item.Instance);
                        return true;

                    case SubmitAction.ConditionalUpdate:
                        if (item.Original != null &&
                            IsModified(item.Entity, item.Instance, item.Original))
                        {
                            this.Table.Update(item.Instance);
                            return true;
                        }
                        break;

                    case SubmitAction.Update:
                        this.Table.Update(item.Instance);
                        return true;

                    default:
                        break; // do nothing
                }

                return false;
            }

            bool ITrackedTable.SubmitChanges(TrackedItem item)
            {
                return this.SubmitChanges(item);
            }

            private void ApplyGeneratedKeyValues(TrackedItem item, object[] keyValues)
            {
                // remove from tracked item
                _tracked.Remove((TEntity) item.Instance);
 
                var generatedKeys = GetGeneratedKeys();
                for (int i = 0; i < generatedKeys.Length; i++)
                {
                    generatedKeys[i].SetValue((TEntity) item.Instance, keyValues[i]);
                }
 
                // re-add tracked item to update its key
                _tracked[(TEntity)item.Instance] = item;
            }
 
            private MemberInfo[]? _generatedKeys;
            private MemberInfo[] GetGeneratedKeys()
            {
                if (_generatedKeys == null)
                {
                    _generatedKeys = this.Entity.PrimaryKeyMembers
                        .Where(k => k.IsGenerated)
                        .Select(k => k.Member)
                        .ToArray();
                }
 
                return _generatedKeys;
            }
 
            private Expression<Func<TEntity, object[]>>? _generatedKeySelector;
            private Expression<Func<TEntity, object[]>>? GetGeneratedKeySelector()
            {
                var generatedKeys = GetGeneratedKeys();
                if (generatedKeys.Length > 0)
                {
                    var e = Expression.Parameter(typeof(TEntity), "entity");
                    var arrayConstruction = Expression.NewArrayInit(typeof(object), generatedKeys.Select(gk => Expression.Convert(GetMemberAccess(e, gk), typeof(object))).ToArray());
                    _generatedKeySelector = Expression.Lambda<Func<TEntity, object[]>>(arrayConstruction, e);
                }
 
                return _generatedKeySelector;
            }
 
            private Expression GetMemberAccess(Expression expression, MemberInfo member)
            {
                var fi = member as FieldInfo;
                if (fi != null)
                {
                    return Expression.Field(expression, fi);
                }

                var pi = member as PropertyInfo;
                if (pi != null)
                {
                    return Expression.Property(expression, pi);
                }

                throw new InvalidOperationException();
            }

            private void AcceptChanges(TrackedItem item)
            {
                switch (item.State)
                {
                    case SubmitAction.Delete:
                        this.RemoveFromCache((TEntity)item.Instance);
                        this.AssignAction((TEntity)item.Instance, SubmitAction.None);
                        break;
                    case SubmitAction.Insert:
                        this.AddToCache((TEntity)item.Instance);
                        this.AssignAction((TEntity)item.Instance, SubmitAction.ConditionalUpdate);
                        break;
                    case SubmitAction.InsertOrUpdate:
                        this.AddToCache((TEntity)item.Instance);
                        this.AssignAction((TEntity)item.Instance, SubmitAction.ConditionalUpdate);
                        break;
                    case SubmitAction.ConditionalUpdate:
                    case SubmitAction.Update:
                        this.AssignAction((TEntity)item.Instance, SubmitAction.ConditionalUpdate);
                        break;
                    default:
                        break; // do nothing
                }
            }

            void ITrackedTable.AcceptChanges(TrackedItem item)
            {
                this.AcceptChanges(item);
            }

            public override object OnEntityMaterialized(object instance)
            {
                TEntity typedInstance = (TEntity)instance;
                var cached = this.AddToCache(typedInstance)!;
                if ((object)cached == (object)typedInstance)
                {
                    this.AssignAction(typedInstance, SubmitAction.ConditionalUpdate);
                }

                return cached;
            }

            /// <summary>
            /// Gets the current <see cref="SubmitAction"/> for the entity instance.
            /// </summary>
            public override SubmitAction GetSubmitAction(TEntity instance)
            {
                TrackedItem ti;
                if (_tracked.TryGetValue(instance, out ti))
                {
                    if (ti.State == SubmitAction.ConditionalUpdate)
                    {
                        if (ti.Original != null 
                            && IsModified(ti.Entity, ti.Instance, ti.Original))
                        {
                            return SubmitAction.Update;
                        }
                        else
                        {
                            return SubmitAction.None;
                        }
                    }

                    return ti.State;
                }

                return SubmitAction.None;
            }

            public override void SetSubmitAction(TEntity instance, SubmitAction action)
            {
                switch (action)
                {
                    case SubmitAction.None:
                    case SubmitAction.ConditionalUpdate:
                    case SubmitAction.Update:
                    case SubmitAction.Delete:
                        var cached = this.AddToCache(instance)!;
                        if ((object)cached != (object)instance!)
                        {
                            throw new InvalidOperationException("An different instance with the same key is already in the cache.");
                        }
                        break;
                }

                this.AssignAction(instance, action);
            }

            private EntityMapping Mapping
            {
                get { return this.Session.Provider.Mapping; }
            }

            private TEntity AddToCache(TEntity instance)
            {
                var key = GetPrimaryKey(this.Entity, instance);
                if (key != null)
                {
                    if (!_identityCache.TryGetValue(key, out var cached))
                    {
                        cached = instance;
                        _identityCache.Add(key, cached);
                    }

                    return cached;
                }

                return instance;
            }

            private void RemoveFromCache(TEntity instance)
            {
                var key = GetPrimaryKey(this.Entity, instance);
                if (key != null)
                {
                    _identityCache.Remove(key);
                }
            }

            private void AssignAction(TEntity instance, SubmitAction action)
            {
                TrackedItem ti;
                _tracked.TryGetValue(instance, out ti);

                switch (action)
                {
                    case SubmitAction.Insert:
                    case SubmitAction.InsertOrUpdate:
                    case SubmitAction.Update:
                    case SubmitAction.Delete:
                    case SubmitAction.None:
                        _tracked[instance] = new TrackedItem(this, instance, ti?.Original, action, ti?.HookedEvent ?? false);
                        break;

                    case SubmitAction.ConditionalUpdate:
                        if (instance is INotifyPropertyChanging notify)
                        {
                            if (!ti.HookedEvent)
                            {
                                notify.PropertyChanging += new PropertyChangingEventHandler(this.OnPropertyChanging);
                            }
                            _tracked[instance] = new TrackedItem(this, instance, null, SubmitAction.ConditionalUpdate, true);
                        }
                        else
                        {
                            var original = CloneEntity(this.Entity, instance);
                            _tracked[instance] = new TrackedItem(this, instance, original, SubmitAction.ConditionalUpdate, false);
                        }
                        break;

                    default:
                        throw new InvalidOperationException(string.Format("Unknown SubmitAction: {0}", action));
                }
            }

            protected virtual void OnPropertyChanging(object sender, PropertyChangingEventArgs args)
            {
                TrackedItem ti;
                if (_tracked.TryGetValue((TEntity)sender, out ti) && ti.State == SubmitAction.ConditionalUpdate)
                {
                    object clone = CloneEntity(this.Entity, ti.Instance);
                    _tracked[(TEntity)sender] = new TrackedItem(this, ti.Instance, clone, SubmitAction.Update, true);
                }
            }
        }

        private class TrackedItem
        {
            public ITrackedTable Table { get; }
            public object Instance { get; }
            public object? Original { get; }
            public SubmitAction State { get; }
            public bool HookedEvent { get; }

            internal TrackedItem(
                ITrackedTable table, 
                object instance, 
                object? original, 
                SubmitAction state, 
                bool hookedEvent)
            {
                this.Table = table;
                this.Instance = instance;
                this.Original = original;
                this.State = state;
                this.HookedEvent = hookedEvent;
            }

            public MappedEntity Entity => this.Table.Entity;

            public static readonly IEnumerable<TrackedItem> EmptyList = new TrackedItem[] { };
        }

        private IEnumerable<TrackedItem> GetOrderedItems()
        {
            var items = (from tab in this.GetTables()
                         from ti in ((ITrackedTable)tab).TrackedItems
                         where ti.State != SubmitAction.None
                         select ti).ToList();

            // build edge map to represent all references between entities
            var edges = this.GetEdges(items).Distinct().ToList();
            var stLookup = edges.ToLookup(e => e.Source, e => e.Target);
            var tsLookup = edges.ToLookup(e => e.Target, e => e.Source);

            return TopologicalSorter.Sort(items, item =>
            {
                switch (item.State)
                {
                    case SubmitAction.Insert:
                    case SubmitAction.InsertOrUpdate:
                        // all things this instance depends on must come first
                        var beforeMe = stLookup[item];

                        // if another object exists with same key that is being deleted, then the delete must come before the insert
                        var primaryKey = GetPrimaryKey(item.Entity, item.Instance);
                        if (primaryKey != null)
                        {
                            var cached = item.Table.GetFromCacheById(primaryKey);
                            if (cached != null && cached != item.Instance)
                            {
                                var ti = item.Table.GetTrackedItem(cached);
                                if (ti != null && ti.State == SubmitAction.Delete)
                                {
                                    beforeMe = beforeMe.Concat(new[] { ti });
                                }
                            }
                        }
                        return beforeMe;

                    case SubmitAction.Delete:
                        // all things that depend on this instance must come first
                        return tsLookup[item];

                    default:
                        return TrackedItem.EmptyList;
                }
            });
        }

        private TrackedItem? GetTrackedItem(EntityInfo entity)
        {
            ITrackedTable table = (ITrackedTable)this.GetTable(entity.Mapping);
            return table.GetTrackedItem(entity.Instance);
        }

        private IEnumerable<Edge> GetEdges(IEnumerable<TrackedItem> items)
        {
            foreach (var c in items)
            {
                foreach (var d in this.GetDependingEntities(c.Entity, c.Instance))
                {
                    var dc = this.GetTrackedItem(d);
                    if (dc != null)
                    {
                        yield return new Edge(dc, c);
                    }
                }

                foreach (var d in this.GetDependentEntities(c.Entity, c.Instance))
                {
                    var dc = this.GetTrackedItem(d);
                    if (dc != null)
                    {
                        yield return new Edge(c, dc);
                    }
                }
            }
        }

        private class Edge : IEquatable<Edge>
        {
            public TrackedItem Source { get; }
            public TrackedItem Target { get; }
            private readonly int hash;

            internal Edge(TrackedItem source, TrackedItem target)
            {
                this.Source = source;
                this.Target = target;
                this.hash = this.Source.GetHashCode() + this.Target.GetHashCode();
            }

            public bool Equals(Edge edge)
            {
                return edge != null && this.Source == edge.Source && this.Target == edge.Target;
            }

            public override bool Equals(object obj)
            {
                return obj is Edge e && this.Equals(e);
            }

            public override int GetHashCode()
            {
                return this.hash;
            }
        }

        private IEnumerable<EntityInfo> GetDependentEntities(
            MappedEntity entity, object instance)
        {
            var mapping = _provider.Mapping;

            foreach (var rm in entity.RelationshipMembers)
            {
                if (rm.IsSource)
                {
                    var relatedEntity = rm.RelatedEntity;
                    var value = TypeHelper.GetFieldOrPropertyValue(rm.Member, instance);
                    if (value != null)
                    {
                        var list = value as IList;
                        if (list != null)
                        {
                            foreach (var item in list)
                            {
                                if (item != null)
                                {
                                    yield return new EntityInfo(item, relatedEntity);
                                }
                            }
                        }
                        else
                        {
                            yield return new EntityInfo(value, relatedEntity);
                        }
                    }
                }
            }
        }

        private IEnumerable<EntityInfo> GetDependingEntities(
            MappedEntity entity, object instance)
        {
            var mapping = _provider.Mapping;

            foreach (var rm in entity.RelationshipMembers)
            {
                if (rm.IsTarget)
                {
                    var relatedEntity = rm.RelatedEntity;

                    var value = TypeHelper.GetFieldOrPropertyValue(rm.Member, instance);
                    if (value != null)
                    {
                        var list = value as IList;
                        if (list != null)
                        {
                            foreach (var item in list)
                            {
                                if (item != null)
                                {
                                    yield return new EntityInfo(item, relatedEntity);
                                }
                            }
                        }
                        else
                        {
                            yield return new EntityInfo(value, relatedEntity);
                        }
                    }
                }
            }
        }

        private static object? GetPrimaryKey(MappedEntity entity, object instance)
        {
            object? firstKey = null;
            List<object>? keys = null;

            foreach (var pkm in entity.PrimaryKeyMembers)
            {
                var value = TypeHelper.GetFieldOrPropertyValue(pkm.Member, instance);
                
                if (firstKey == null)
                {
                    firstKey = value;
                }
                else
                {
                    if (keys == null)
                    {
                        keys = new List<object>();
                        keys.Add(firstKey);
                    }

                    keys.Add(value!);
                }
            }

            if (keys != null)
            {
                return new CompoundKey(keys.ToArray());
            }

            return firstKey;
        }

        private static object CloneEntity(MappedEntity entity, object instance)
        {
            var clone = System.Runtime.Serialization.FormatterServices.GetSafeUninitializedObject(entity.ConstructedType);

            foreach (var mm in entity.MappedMembers)
            {
                if (mm is MappedColumnMember cm)
                {
                    TypeHelper.SetFieldOrPropertyValue(
                        cm.Member,
                        clone,
                        TypeHelper.GetFieldOrPropertyValue(cm.Member, instance)
                        );
                }
                else if (mm is MappedNestedEntityMember nm)
                {
                    var nestedValue = TypeHelper.GetFieldOrPropertyValue(nm.Member, instance);
                    if (nestedValue != null)
                    {
                        var nestedClone = CloneEntity(nm.RelatedEntity, nestedValue);
                        TypeHelper.SetFieldOrPropertyValue(nm.Member, nestedClone, clone);
                    }
                }
            }

            return clone;
        }

        private static bool IsModified(MappedEntity entity, object instance, object original)
        {
            foreach (var mm in entity.MappedMembers)
            {
                if (mm is MappedColumnMember cm)
                {
                    var current_value = TypeHelper.GetFieldOrPropertyValue(cm.Member, instance);
                    var original_value = TypeHelper.GetFieldOrPropertyValue(cm.Member, original);

                    if (!object.Equals(current_value, original_value))
                        return true;
                }
                else if (mm is MappedNestedEntityMember nm)
                {
                    var current_value = TypeHelper.GetFieldOrPropertyValue(nm.Member, instance);
                    var original_value = TypeHelper.GetFieldOrPropertyValue(nm.Member, original);
                    if (IsModified(nm.RelatedEntity, current_value!, original_value!))
                        return true;
                }
            }

            return false;
        }
    }
}