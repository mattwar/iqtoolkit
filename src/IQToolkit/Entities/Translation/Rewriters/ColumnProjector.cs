// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using Expressions.Sql;

    /// <summary>
    /// Result from calling ColumnProjector.ProjectColumns
    /// </summary>
    public sealed class ProjectedColumns
    {
        private readonly Expression projector;
        private readonly IReadOnlyList<ColumnDeclaration> columns;

        public ProjectedColumns(Expression projector, IReadOnlyList<ColumnDeclaration> columns)
        {
            this.projector = projector;
            this.columns = columns;
        }

        /// <summary>
        /// The expression to computed on the client.
        /// </summary>
        public Expression Projector
        {
            get { return this.projector; }
        }

        /// <summary>
        /// The columns to be computed on the server.
        /// </summary>
        public IReadOnlyList<ColumnDeclaration> Columns
        {
            get { return this.columns; }
        }
    }

    public enum ProjectionAffinity
    {
        /// <summary>
        /// Prefer expression computation on the client
        /// </summary>
        Client,

        /// <summary>
        /// Prefer expression computation on the server
        /// </summary>
        Server
    }

    /// <summary>
    /// Splits an expression into two parts
    ///   1) a list of column declarations for sub-expressions that must be evaluated on the server
    ///   2) a expression that describes how to combine/project the columns back together into the correct result
    /// </summary>
    public class ColumnProjector : SqlExpressionVisitor
    {
        private readonly QueryLanguage _language;
        private readonly Dictionary<ColumnExpression, ColumnExpression> _map;
        private readonly List<ColumnDeclaration> _columns;
        private readonly HashSet<string> _columnNames;
        private readonly HashSet<Expression> _candidates;
        private readonly ImmutableHashSet<TableAlias> _existingAliases;
        private readonly TableAlias _newAlias;
        private int _iColumn;

        private ColumnProjector(
            QueryLanguage language, 
            ProjectionAffinity affinity, 
            Expression expression, 
            IEnumerable<ColumnDeclaration>? existingColumns, 
            TableAlias newAlias, 
            IEnumerable<TableAlias> existingAliases)
        {
            _language = language;
            _newAlias = newAlias;
            _existingAliases = existingAliases.ToImmutableHashSet();
            _map = new Dictionary<ColumnExpression, ColumnExpression>();
            
            if (existingColumns != null)
            {
                _columns = new List<ColumnDeclaration>(existingColumns);
                _columnNames = new HashSet<string>(existingColumns.Select(c => c.Name));
            }
            else
            {
                _columns = new List<ColumnDeclaration>();
                _columnNames = new HashSet<string>();
            }

            _candidates = Nominator.Nominate(language, affinity, _existingAliases, expression);
        }

        public static ProjectedColumns ProjectColumns(
            QueryLanguage language, 
            ProjectionAffinity affinity, 
            Expression expression, 
            IEnumerable<ColumnDeclaration>? existingColumns, 
            TableAlias newAlias, 
            IEnumerable<TableAlias> existingAliases)
        {
            var projector = new ColumnProjector(language, affinity, expression, existingColumns, newAlias, existingAliases);
            var expr = projector.Visit(expression);
            return new ProjectedColumns(expr, projector._columns.AsReadOnly());
        }

        public static ProjectedColumns ProjectColumns(
            QueryLanguage language, 
            Expression expression, 
            IEnumerable<ColumnDeclaration>? existingColumns, 
            TableAlias newAlias, 
            IEnumerable<TableAlias> existingAliases)
        {
            return ProjectColumns(language, ProjectionAffinity.Client, expression, existingColumns, newAlias, existingAliases);
        }

        public static ProjectedColumns ProjectColumns(
            QueryLanguage language, 
            ProjectionAffinity affinity, 
            Expression expression, 
            IEnumerable<ColumnDeclaration>? existingColumns, 
            TableAlias newAlias, 
            params TableAlias[] existingAliases)
        {
            return ProjectColumns(language, affinity, expression, existingColumns, newAlias, (IEnumerable<TableAlias>)existingAliases);
        }

        public static ProjectedColumns ProjectColumns(
            QueryLanguage language, 
            Expression expression, 
            IEnumerable<ColumnDeclaration>? existingColumns, 
            TableAlias newAlias, 
            params TableAlias[] existingAliases)
        {
            return ProjectColumns(language, expression, existingColumns, newAlias, (IEnumerable<TableAlias>)existingAliases);
        }

        public override Expression Visit(Expression expression)
        {
            if (expression == null)
                return null!;

            if (_candidates.Contains(expression))
            {
                if (expression is ColumnExpression column)
                {
                    if (_map.TryGetValue(column, out var mapped))
                    {
                        return mapped;
                    }

                    // check for column that already refers to this column
                    foreach (ColumnDeclaration existingColumn in _columns)
                    {
                        var cex = existingColumn.Expression as ColumnExpression;
                        if (cex != null && cex.Alias == column.Alias && cex.Name == column.Name)
                        {
                            // refer to the column already in the column list
                            return new ColumnExpression(column.Type, column.QueryType, _newAlias, existingColumn.Name);
                        }
                    }

                    if (_existingAliases.Contains(column.Alias)) 
                    {
                        var ordinal = _columns.Count;
                        var columnName = this.GetUniqueColumnName(column.Name);
                        _columns.Add(new ColumnDeclaration(columnName, column, column.QueryType));
                        mapped = new ColumnExpression(column.Type, column.QueryType, _newAlias, columnName);
                        _map.Add(column, mapped);
                        _columnNames.Add(columnName);
                        return mapped;
                    }

                    // must be referring to outer scope
                    return column;
                }
                else
                {
                    var columnName = this.GetNextColumnName();
                    var colType = _language.TypeSystem.GetQueryType(expression.Type);
                    _columns.Add(new ColumnDeclaration(columnName, expression, colType));
                    return new ColumnExpression(expression.Type, colType, _newAlias, columnName);
                }
            }
            else
            {
                return base.Visit(expression);
            }
        }

        private bool IsColumnNameInUse(string name)
        {
            return _columnNames.Contains(name);
        }

        private string GetUniqueColumnName(string name)
        {
            string baseName = name;
            int suffix = 1;
            
            while (this.IsColumnNameInUse(name))
            {
                name = baseName + (suffix++);
            }

            return name;
        }

        private string GetNextColumnName()
        {
            return this.GetUniqueColumnName("c" + (_iColumn++));
        }

        /// <summary>
        /// Nominator is a class that determining the set of 
        /// candidate expressions that are possible columns of a select expression
        /// </summary>
        private class Nominator : SqlExpressionVisitor
        {
            private readonly QueryLanguage _language;
            private readonly HashSet<Expression> _candidates;
            private readonly ProjectionAffinity _affinity;
            private readonly ImmutableHashSet<TableAlias> _validAliases;

            private State _state;
            private bool _validColumnsOnly;

            private enum State
            {
                MustBeColumn,
                CanBeColumn,
                CannotBeColumn
            }

            private Nominator(
                QueryLanguage language, 
                ProjectionAffinity affinity,
                ImmutableHashSet<TableAlias> validAliases)
            {
                _language = language;
                _affinity = affinity;
                _validAliases = validAliases;
                _candidates = new HashSet<Expression>();
                _state = State.CanBeColumn;
            }

            internal static HashSet<Expression> Nominate(
                QueryLanguage language, 
                ProjectionAffinity affinity, 
                ImmutableHashSet<TableAlias> validAliases,
                Expression expression)
            {
                Nominator nominator = new Nominator(language, affinity, validAliases);
                nominator.Visit(expression);
                return nominator._candidates;
            }

            private State GetState(Expression expression)
            {
                if (expression is ColumnExpression col)
                {
                    // we cannot nominate a column that is declared in the local projection
                    // any other alias is assumed to be external and is valid
                    return _validAliases.Contains(col.Alias)
                        ? State.MustBeColumn
                        : State.CannotBeColumn;
                }
                else if (_validColumnsOnly)
                {
                    // not a reference to valid column
                    return State.CannotBeColumn;
                }
                else if (_language.MustBeColumn(expression))
                {
                    return State.MustBeColumn;
                }
                else if (_language.CanBeColumn(expression))
                {
                    return State.CanBeColumn;
                }
                else
                {
                    return State.CannotBeColumn;
                }
            }

            public override Expression Visit(Expression expression)
            {
                if (expression == null)
                    return null!;

                // reset from incoming state from peers
                var oldState = _state;
                _state = State.CanBeColumn;

                base.Visit(expression);

                // get current expression's state
                var exprState = GetState(expression);

                if (exprState == State.MustBeColumn
                    && _state == State.CannotBeColumn)
                {
                    // if only refers to internally declared or valid columns, then okay
                    if (expression.IsSelfContained(_validAliases))
                    {
                        _candidates.Add(expression);
                        _state = oldState;
                    }
                }
                else if (_state != State.CannotBeColumn // child element cannot be in column
                    && exprState != State.CannotBeColumn) // this node cannot be in column
                {
                    if (exprState == State.MustBeColumn
                        || (exprState == State.CanBeColumn 
                            && _affinity == ProjectionAffinity.Server))
                    {
                        // don't have constants become columns, but also don't block
                        if (!(expression is ConstantExpression))
                            _candidates.Add(expression);
                    }
                }

                _state = (oldState == State.CannotBeColumn
                    || _state == State.CannotBeColumn
                    || exprState == State.CannotBeColumn)
                    ? State.CannotBeColumn
                    : State.CanBeColumn;

                return expression;
            }

            // if we start with a client project, don't nominate expressions that are not part of the
            // projection, except for direct referenced to external columns (probably in where clause)
            protected internal override Expression VisitClientProjection(ClientProjectionExpression proj)
            {
                if (_validColumnsOnly)
                {
                    // still only nominate references to valid columns
                    base.VisitClientProjection(proj);
                }
                else
                {
                    // only nominate references to valid columns
                    _validColumnsOnly = true;
                    this.Visit(proj.Select);

                    // normal rules apply on projection expressions
                    _validColumnsOnly = false;
                    this.Visit(proj.Projector);
                }

                return proj;
            }
        }
    }
}
