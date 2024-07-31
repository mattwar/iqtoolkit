// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Entities.Sessions
{
    /// <summary>
    /// Additional API's for all <see cref="ISessionTable"/> instances. 
    /// </summary>
    public static class SessionTableExtensions
    {
        /// <summary>
        /// Insert the entity into the database when <see cref="IEntitySession.SubmitChanges"/> is called.
        /// </summary>
        public static void InsertOnSubmit<TEntity>(this ISessionTable<TEntity> table, TEntity instance)
            where TEntity : class
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
        public static void InsertOrUpdateOnSubmit<TEntity>(this ISessionTable<TEntity> table, TEntity instance)
            where TEntity : class
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
        public static void UpdateOnSubmit<TEntity>(this ISessionTable<TEntity> table, TEntity instance)
            where TEntity : class
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
        public static void DeleteOnSubmit<TEntity>(this ISessionTable<TEntity> table, TEntity instance)
            where TEntity : class
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