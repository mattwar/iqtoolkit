// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq;

namespace IQToolkit.Data.Sessions
{
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
        object? GetById(object id);

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
    public interface ISessionTable<TEntity> : IQueryable<TEntity>, ISessionTable
        where TEntity : class
    {
        /// <summary>
        /// The <see cref="IEntityTable{T}"/> associated with this <see cref="ISessionTable{T}"/>
        /// </summary>
        new IEntityTable<TEntity> Table { get; }

        /// <summary>
        /// Gets an entity instance given its id (primary key value)
        /// </summary>
        new TEntity? GetById(object id);

        /// <summary>
        /// Set the <see cref="SubmitAction"/> for this entity instance.
        /// </summary>
        void SetSubmitAction(TEntity instance, SubmitAction action);

        /// <summary>
        /// Gets the current <see cref="SubmitAction"/> for the entity instance.
        /// </summary>
        SubmitAction GetSubmitAction(TEntity instance);
    }
}