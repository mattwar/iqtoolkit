// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Linq;

namespace IQToolkit
{
    /// <summary>
    /// Sessions track changes to entity instances 
    /// and enable submiting changes to multiple entities to the database in one atomic action. 
    /// </summary>
    public interface IEntitySession
    {
        /// <summary>
        /// The underlying <see cref="IEntityProvider"/> for this session.
        /// </summary>
        IEntityProvider Provider { get; }

        /// <summary>
        /// Gets the <see cref="ISessionTable{TEntity}"/> for the corresponding logical database table.
        /// </summary>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        ISessionTable<TEntity> GetTable<TEntity>(string entityId = null);

        /// <summary>
        /// Gets the <see cref="ISessionTable"/> for the corresponding logical database table.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <param name="entityId">An id used to associate the entity type with its mapping.
        /// If not specified the name of the entity type is used.</param>
        ISessionTable GetTable(Type entityType, string entityId = null);

        /// <summary>
        /// Submit all changes to the database as a single transaction.
        /// </summary>
        void SubmitChanges();
    }

    /// <summary>
    /// A table associated with an <see cref="IEntitySession"/>
    /// </summary>
    public interface ISessionTable : IQueryable
    {
        /// <summary>
        /// The <see cref="IEntitySession"/> associated with this <see cref="ISessionTable"/>
        /// </summary>
        IEntitySession Session { get; }

        /// <summary>
        /// The underlying provider's <see cref="IEntityTable"/> corresponding to this <see cref="ISessionTable"/>.
        /// </summary>
        IEntityTable Table { get; }

        /// <summary>
        /// Gets an entity instance given its id (primary key value)
        /// </summary>
        object GetById(object id);

        /// <summary>
        /// Set the <see cref="SubmitAction"/> for this entity instance.
        /// </summary>
        void SetSubmitAction(object instance, SubmitAction action);

        /// <summary>
        /// Gets the current <see cref="SubmitAction"/> for the entity instance.
        /// </summary>
        SubmitAction GetSubmitAction(object instance);
    }

    /// <summary>
    /// A table associated with an <see cref="IEntitySession"/>
    /// </summary>
    public interface ISessionTable<T> : IQueryable<T>, ISessionTable
    {
        /// <summary>
        /// The <see cref="IEntityTable{T}"/> associated with this <see cref="ISessionTable{T}"/>
        /// </summary>
        new IEntityTable<T> Table { get; }

        /// <summary>
        /// Gets an entity instance given its id (primary key value)
        /// </summary>
        new T GetById(object id);

        /// <summary>
        /// Set the <see cref="SubmitAction"/> for this entity instance.
        /// </summary>
        void SetSubmitAction(T instance, SubmitAction action);

        /// <summary>
        /// Gets the current <see cref="SubmitAction"/> for the entity instance.
        /// </summary>
        SubmitAction GetSubmitAction(T instance);
    }

    /// <summary>
    /// The action to be undertaken for an individual entity instance when <see cref="IEntitySession.SubmitChanges"/> is called.
    /// </summary>
    public enum SubmitAction
    {
        /// <summary>
        /// No action is taken.
        /// </summary>
        None,

        /// <summary>
        /// The entity is updated in the database with the new values of the instance.
        /// </summary>
        Update,

        /// <summary>
        /// The entity is updated if it has changed.
        /// </summary>
        PossibleUpdate,

        /// <summary>
        /// The new entity is inserted
        /// </summary>
        Insert,

        /// <summary>
        /// The entity is either inserted if new or updated if it already exists.
        /// </summary>
        InsertOrUpdate,

        /// <summary>
        /// The entity is deleted.
        /// </summary>
        Delete
    }

    /// <summary>
    /// Additional API's for all <see cref="ISessionTable"/> instances. 
    /// </summary>
    public static class SessionTableExtensions
    {
        /// <summary>
        /// Insert the entity into the database when <see cref="IEntitySession.SubmitChanges"/> is called.
        /// </summary>
        public static void InsertOnSubmit<T>(this ISessionTable<T> table, T instance)
        {
            table.SetSubmitAction(instance, SubmitAction.Insert);
        }

        /// <summary>
        /// Insert the entity into the database or update it if it already exists when <see cref="IEntitySession.SubmitChanges"/> is called.
        /// </summary>
        public static void InsertOnSubmit(this ISessionTable table, object instance)
        {
            table.SetSubmitAction(instance, SubmitAction.Insert);
        }

        /// <summary>
        /// Insert the entity into the database or update it if it already exists when <see cref="IEntitySession.SubmitChanges"/> is called.
        /// </summary>
        public static void InsertOrUpdateOnSubmit<T>(this ISessionTable<T> table, T instance)
        {
            table.SetSubmitAction(instance, SubmitAction.InsertOrUpdate);
        }

        /// <summary>
        /// Insert the entity instance if new or update with new values when <see cref="IEntitySession.SubmitChanges"/> is called.
        /// </summary>
        public static void InsertOrUpdateOnSubmit(this ISessionTable table, object instance)
        {
            table.SetSubmitAction(instance, SubmitAction.InsertOrUpdate);
        }

        /// <summary>
        /// Update the entity in the database when <see cref="IEntitySession.SubmitChanges"/> is called.
        /// </summary>
        public static void UpdateOnSubmit<T>(this ISessionTable<T> table, T instance)
        {
            table.SetSubmitAction(instance, SubmitAction.Update);
        }

        /// <summary>
        /// Update the entity in the database when <see cref="IEntitySession.SubmitChanges"/> is called.
        /// </summary>
        public static void UpdateOnSubmit(this ISessionTable table, object instance)
        {
            table.SetSubmitAction(instance, SubmitAction.Update);
        }

        /// <summary>
        /// Delete the entity from the database when <see cref="IEntitySession.SubmitChanges"/> is called.
        /// </summary>
        public static void DeleteOnSubmit<T>(this ISessionTable<T> table, T instance)
        {
            table.SetSubmitAction(instance, SubmitAction.Delete);
        }

        /// <summary>
        /// Delete the entity from the database when <see cref="IEntitySession.SubmitChanges"/> is called.
        /// </summary>
        public static void DeleteOnSubmit(this ISessionTable table, object instance)
        {
            table.SetSubmitAction(instance, SubmitAction.Delete);
        }
    }
}