// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

namespace IQToolkit.Data.Sessions
{
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
        ConditionalUpdate,

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
}