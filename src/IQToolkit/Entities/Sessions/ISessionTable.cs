// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System.Linq;

namespace IQToolkit.Entities.Sessions
{
    /// <summary>
    /// A table associated with an <see cref="IEntitySession"/>
    /// </summary>
    public interface ISessionTable : IEntityTable
    {
        /// <summary>
        /// The <see cref="IEntitySession"/> associated with this <see cref="ISessionTable"/>
        /// </summary>
        IEntitySession Session { get; }

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
    public interface ISessionTable<TEntity> : IEntityTable<TEntity>, ISessionTable
        where TEntity : class
    {
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