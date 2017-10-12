// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data
{
    using Common;

    /// <summary>
    /// Implements the <see cref="IEntitySession"/> contract over an <see cref="EntityProvider"/>.
    /// </summary>
    public class EntitySession : IEntitySession
    {
        private readonly EntityProvider provider;
        private readonly SessionProvider sessionProvider;
        private readonly Dictionary<MappingEntity, ISessionTable> tables;

        /// <summary>
        /// Construct a <see cref="EntitySession"/>
        /// </summary>
        public EntitySession(EntityProvider provider)
        {
            this.provider = provider;
            this.sessionProvider = new SessionProvider(this, provider);
            this.tables = new Dictionary<MappingEntity, ISessionTable>();
        }

        /// <summary>
        /// The underlying <see cref="IEntityProvider"/>
        /// </summary>
        public IEntityProvider Provider
        {
            get { return this.sessionProvider; }
        }

        IEntityProvider IEntitySession.Provider
        {
            get { return this.Provider; }
        }

        protected IEnumerable<ISessionTable> GetTables()
        {
            return this.tables.Values;
        }

        /// <summary>
        /// Gets the <see cref="ISessionTable"/> for the corresponding database table.
        /// </summary>
        /// <param name="entityType">The type of the entities held by this table.</param>
        /// <param name="entityId">The id of the entity in the mapping.
        /// If unspecified, the provider will infer the id from the element type.</param>
        public ISessionTable GetTable(Type entityType, string entityId = null)
        {
            return this.GetTable(this.sessionProvider.Provider.Mapping.GetEntity(entityType, entityId));
        }

        /// <summary>
        /// Gets the <see cref="ISessionTable"/> for the corresponding database table.
        /// </summary>
        /// <param name="entityId">The id of the entity in the mapping.
        /// If unspecified, the provider will infer the id from the element type.</param>
        public ISessionTable<TEntity> GetTable<TEntity>(string entityId = null)
        {
            return (ISessionTable<TEntity>)this.GetTable(typeof(TEntity), entityId);
        }

        protected ISessionTable GetTable(MappingEntity entity)
        {
            ISessionTable table;
            if (!this.tables.TryGetValue(entity, out table))
            {
                table = this.CreateTable(entity);
                this.tables.Add(entity, table);
            }
            return table;
        }

        private object OnEntityMaterialized(MappingEntity entity, object instance)
        {
            IEntitySessionTable table = (IEntitySessionTable)this.GetTable(entity);
            return table.OnEntityMaterialized(instance);
        }

        interface IEntitySessionTable : ISessionTable
        {
            object OnEntityMaterialized(object instance);
            MappingEntity Entity { get; }
        }

        private abstract class SessionTable<T> : Query<T>, ISessionTable<T>, ISessionTable, IEntitySessionTable
        {
            private readonly EntitySession session;
            private readonly MappingEntity entity;
            private readonly IEntityTable<T> underlyingTable;

            public SessionTable(EntitySession session, MappingEntity entity)
                : base(session.sessionProvider, typeof(ISessionTable<T>))
            {
                this.session = session;
                this.entity = entity;
                this.underlyingTable = this.session.Provider.GetTable<T>(entity.EntityId);
            }

            public IEntitySession Session
            {
                get { return this.session; }
            }

            public MappingEntity Entity 
            {
                get { return this.entity; }
            }

            public IEntityTable<T> Table
            {
                get { return this.underlyingTable; }
            }

            IEntityTable ISessionTable.Table
            {
                get { return this.underlyingTable; }
            }

            public T GetById(object id)
            {
                return this.underlyingTable.GetById(id);
            }

            object ISessionTable.GetById(object id)
            {
                return this.GetById(id);
            }

            public virtual object OnEntityMaterialized(object instance)
            {
                return instance;
            }

            public virtual void SetSubmitAction(T instance, SubmitAction action)
            {
                throw new NotImplementedException();
            }

            void ISessionTable.SetSubmitAction(object instance, SubmitAction action)
            {
                this.SetSubmitAction((T)instance, action);
            }

            public virtual SubmitAction GetSubmitAction(T instance)
            {
                throw new NotImplementedException();
            }

            SubmitAction ISessionTable.GetSubmitAction(object instance)
            {
                return this.GetSubmitAction((T)instance);
            }
        }

        private class SessionProvider : QueryProvider, IEntityProvider, IQueryExecutorFactory
        {
            private readonly EntitySession session;
            private readonly EntityProvider provider;

            public SessionProvider(EntitySession session, EntityProvider provider)
            {
                this.session = session;
                this.provider = provider;
            }

            public EntityProvider Provider
            {
                get { return this.provider; }
            }

            public override object Execute(Expression expression)
            {
                return this.provider.Execute(expression);
            }

            public override string GetQueryText(Expression expression)
            {
                return this.provider.GetQueryText(expression);
            }

            public IEntityTable<T> GetTable<T>(string tableId)
            {
                return this.provider.GetTable<T>(tableId);
            }

            public IEntityTable GetTable(Type type, string tableId)
            {
                return this.provider.GetTable(type, tableId);
            }

            public bool CanBeEvaluatedLocally(Expression expression)
            {
                return this.provider.Mapping.CanBeEvaluatedLocally(expression);
            }

            public bool CanBeParameter(Expression expression)
            {
                return this.provider.CanBeParameter(expression);
            }

            QueryExecutor IQueryExecutorFactory.CreateExecutor()
            {
                return new SessionExecutor(this.session, ((IQueryExecutorFactory)this.provider).CreateExecutor());
            }
        }

        private class SessionExecutor : QueryExecutor
        {
            private readonly EntitySession session;
            private readonly QueryExecutor executor;

            public SessionExecutor(EntitySession session, QueryExecutor executor)
            {
                this.session = session;
                this.executor = executor;
            }

            public override int RowsAffected
            {
                get { return this.executor.RowsAffected; }
            }

            public override object Convert(object value, Type type)
            {
                return this.executor.Convert(value, type);
            }

            public override IEnumerable<T> Execute<T>(QueryCommand command, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues)
            {
                return this.executor.Execute<T>(command, Wrap(fnProjector, entity), entity, paramValues);
            }

            public override IEnumerable<int> ExecuteBatch(QueryCommand query, IEnumerable<object[]> paramSets, int batchSize, bool stream)
            {
                return this.executor.ExecuteBatch(query, paramSets, batchSize, stream);
            }

            public override IEnumerable<T> ExecuteBatch<T>(QueryCommand query, IEnumerable<object[]> paramSets, Func<FieldReader, T> fnProjector, MappingEntity entity, int batchSize, bool stream)
            {
                return this.executor.ExecuteBatch<T>(query, paramSets, Wrap(fnProjector, entity), entity, batchSize, stream);
            }

            public override IEnumerable<T> ExecuteDeferred<T>(QueryCommand query, Func<FieldReader, T> fnProjector, MappingEntity entity, object[] paramValues)
            {
                return this.executor.ExecuteDeferred<T>(query, Wrap(fnProjector, entity), entity, paramValues);
            }

            public override int ExecuteCommand(QueryCommand query, object[] paramValues)
            {
                return this.executor.ExecuteCommand(query, paramValues);
            }

            private Func<FieldReader, T> Wrap<T>(Func<FieldReader, T> fnProjector, MappingEntity entity)
            {
                Func<FieldReader, T> fnWrapped = (fr) => (T)this.session.OnEntityMaterialized(entity, fnProjector(fr));
                return fnWrapped;
            }
        }

        /// <summary>
        /// Submit changes made to entity instances back to the database, as a single transaction.
        /// </summary>
        public virtual void SubmitChanges()
        {
            this.provider.DoTransacted(
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

        protected virtual ISessionTable CreateTable(MappingEntity entity)
        {
            return (ISessionTable)Activator.CreateInstance(typeof(TrackedTable<>).MakeGenericType(entity.StaticType), new object[] { this, entity });
        }

        private interface ITrackedTable : IEntitySessionTable
        {
            object GetFromCacheById(object key);
            IEnumerable<TrackedItem> TrackedItems { get; }
            TrackedItem GetTrackedItem(object instance);
            bool SubmitChanges(TrackedItem item);
            void AcceptChanges(TrackedItem item);
        }

        private class TrackedTable<T> : SessionTable<T>, ITrackedTable
        {
            private readonly Dictionary<T, TrackedItem> tracked;
            private readonly Dictionary<object, T> identityCache;

            public TrackedTable(EntitySession session, MappingEntity entity)
                : base(session, entity)
            {
                this.tracked = new Dictionary<T, TrackedItem>();
                this.identityCache = new Dictionary<object, T>();
            }

            IEnumerable<TrackedItem> ITrackedTable.TrackedItems
            {
                get { return this.tracked.Values; }
            }

            TrackedItem ITrackedTable.GetTrackedItem(object instance)
            {
                TrackedItem ti;
                if (this.tracked.TryGetValue((T)instance, out ti))
                    return ti;
                return null;
            }

            object ITrackedTable.GetFromCacheById(object key)
            {
                T cached;
                this.identityCache.TryGetValue(key, out cached);
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
                            var keyValues = this.Table.Insert((T)item.Instance, generatedKeySelector);
                            ApplyGeneratedKeyValues(item, keyValues);
                        }
                        else
                        {
                            this.Table.Insert((T)item.Instance);
                        }

                        return true;

                    case SubmitAction.InsertOrUpdate:
                        this.Table.InsertOrUpdate(item.Instance);
                        return true;

                    case SubmitAction.PossibleUpdate:
                        if (item.Original != null &&
                            this.Mapping.IsModified(item.Entity, item.Instance, item.Original))
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
                this.tracked.Remove((T) item.Instance);
 
                var generatedKeys = GetGeneratedKeys();
                for (int i = 0; i < generatedKeys.Length; i++)
                {
                    generatedKeys[i].SetValue((T) item.Instance, keyValues[i]);
                }
 
                // re-add tracked item to update its key
                this.tracked[(T)item.Instance] = item;
            }
 
            private MemberInfo[] generatedKeys;
            private MemberInfo[] GetGeneratedKeys()
            {
                if (generatedKeys == null)
                {
                    generatedKeys = this.Mapping.GetPrimaryKeyMembers(this.Entity)
                                        .Where(k => this.Mapping.IsGenerated(this.Entity, k)).ToArray();
                }
 
                return generatedKeys;
            }
 
            private Expression<Func<T, object[]>> generatedKeySelector;
            private Expression<Func<T, object[]>> GetGeneratedKeySelector()
            {
                var generatedKeys = GetGeneratedKeys();
                if (generatedKeys.Length > 0)
                {
                    var e = Expression.Parameter(typeof(T), "entity");
                    var arrayConstruction = Expression.NewArrayInit(typeof(object), generatedKeys.Select(gk => Expression.Convert(GetMemberAccess(e, gk), typeof(object))).ToArray());
                    generatedKeySelector = Expression.Lambda<Func<T, object[]>>(arrayConstruction, e);
                }
 
                return generatedKeySelector;
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
                        this.RemoveFromCache((T)item.Instance);
                        this.AssignAction((T)item.Instance, SubmitAction.None);
                        break;
                    case SubmitAction.Insert:
                        this.AddToCache((T)item.Instance);
                        this.AssignAction((T)item.Instance, SubmitAction.PossibleUpdate);
                        break;
                    case SubmitAction.InsertOrUpdate:
                        this.AddToCache((T)item.Instance);
                        this.AssignAction((T)item.Instance, SubmitAction.PossibleUpdate);
                        break;
                    case SubmitAction.PossibleUpdate:
                    case SubmitAction.Update:
                        this.AssignAction((T)item.Instance, SubmitAction.PossibleUpdate);
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
                T typedInstance = (T)instance;
                var cached = this.AddToCache(typedInstance);
                if ((object)cached == (object)typedInstance)
                {
                    this.AssignAction(typedInstance, SubmitAction.PossibleUpdate);
                }

                return cached;
            }

            /// <summary>
            /// Gets the current <see cref="SubmitAction"/> for the entity instance.
            /// </summary>
            public override SubmitAction GetSubmitAction(T instance)
            {
                TrackedItem ti;
                if (this.tracked.TryGetValue(instance, out ti))
                {
                    if (ti.State == SubmitAction.PossibleUpdate)
                    {
                        if (this.Mapping.IsModified(ti.Entity, ti.Instance, ti.Original))
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

            public override void SetSubmitAction(T instance, SubmitAction action)
            {
                switch (action)
                {
                    case SubmitAction.None:
                    case SubmitAction.PossibleUpdate:
                    case SubmitAction.Update:
                    case SubmitAction.Delete:
                        var cached = this.AddToCache(instance);
                        if ((object)cached != (object)instance)
                        {
                            throw new InvalidOperationException("An different instance with the same key is already in the cache.");
                        }
                        break;
                }

                this.AssignAction(instance, action);
            }

            private QueryMapping Mapping
            {
                get { return ((EntitySession)this.Session).provider.Mapping; }
            }

            private T AddToCache(T instance)
            {
                object key = this.Mapping.GetPrimaryKey(this.Entity, instance);
                T cached;
                if (!this.identityCache.TryGetValue(key, out cached))
                {
                    cached = instance;
                    this.identityCache.Add(key, cached);
                }
                return cached;
            }

            private void RemoveFromCache(T instance)
            {
                object key = this.Mapping.GetPrimaryKey(this.Entity, instance);
                this.identityCache.Remove(key);
            }

            private void AssignAction(T instance, SubmitAction action)
            {
                TrackedItem ti;
                this.tracked.TryGetValue(instance, out ti);

                switch (action)
                {
                    case SubmitAction.Insert:
                    case SubmitAction.InsertOrUpdate:
                    case SubmitAction.Update:
                    case SubmitAction.Delete:
                    case SubmitAction.None:
                        this.tracked[instance] = new TrackedItem(this, instance, ti != null ? ti.Original : null, action, ti != null ? ti.HookedEvent : false);
                        break;
                    case SubmitAction.PossibleUpdate:
                        INotifyPropertyChanging notify = instance as INotifyPropertyChanging;
                        if (notify != null)
                        {
                            if (!ti.HookedEvent)
                            {
                                notify.PropertyChanging += new PropertyChangingEventHandler(this.OnPropertyChanging);
                            }
                            this.tracked[instance] = new TrackedItem(this, instance, null, SubmitAction.PossibleUpdate, true);
                        }
                        else
                        {
                            var original = this.Mapping.CloneEntity(this.Entity, instance);
                            this.tracked[instance] = new TrackedItem(this, instance, original, SubmitAction.PossibleUpdate, false);
                        }
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Unknown SubmitAction: {0}", action));
                }
            }

            protected virtual void OnPropertyChanging(object sender, PropertyChangingEventArgs args)
            {
                TrackedItem ti;
                if (this.tracked.TryGetValue((T)sender, out ti) && ti.State == SubmitAction.PossibleUpdate)
                {
                    object clone = this.Mapping.CloneEntity(ti.Entity, ti.Instance);
                    this.tracked[(T)sender] = new TrackedItem(this, ti.Instance, clone, SubmitAction.Update, true);
                }
            }
        }

        private class TrackedItem
        {
            public ITrackedTable Table { get; }
            public object Instance { get; }
            public object Original { get; }
            public SubmitAction State { get; }
            public bool HookedEvent { get; }

            internal TrackedItem(ITrackedTable table, object instance, object original, SubmitAction state, bool hookedEvent)
            {
                this.Table = table;
                this.Instance = instance;
                this.Original = original;
                this.State = state;
                this.HookedEvent = hookedEvent;
            }

            public MappingEntity Entity => this.Table.Entity;

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
                        object cached = item.Table.GetFromCacheById(this.provider.Mapping.GetPrimaryKey(item.Entity, item.Instance));
                        if (cached != null && cached != item.Instance)
                        {
                            var ti = item.Table.GetTrackedItem(cached);
                            if (ti != null && ti.State == SubmitAction.Delete)
                            {
                                beforeMe = beforeMe.Concat(new[] { ti });
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

        private TrackedItem GetTrackedItem(EntityInfo entity)
        {
            ITrackedTable table = (ITrackedTable)this.GetTable(entity.Mapping);
            return table.GetTrackedItem(entity.Instance);
        }

        private IEnumerable<Edge> GetEdges(IEnumerable<TrackedItem> items)
        {
            foreach (var c in items)
            {
                foreach (var d in this.provider.Mapping.GetDependingEntities(c.Entity, c.Instance))
                {
                    var dc = this.GetTrackedItem(d);
                    if (dc != null)
                    {
                        yield return new Edge(dc, c);
                    }
                }

                foreach (var d in this.provider.Mapping.GetDependentEntities(c.Entity, c.Instance))
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
                return this.Equals(obj as Edge);
            }

            public override int GetHashCode()
            {
                return this.hash;
            }
        }
    }
}