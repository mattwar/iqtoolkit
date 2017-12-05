﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// Result from calling ColumnProjector.ProjectColumns
    /// </summary>
    public sealed class ProjectedColumns
    {
        readonly Expression projector;
        readonly ReadOnlyCollection<ColumnDeclaration> columns;

        public ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns)
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
        public ReadOnlyCollection<ColumnDeclaration> Columns
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
    public class ColumnProjector : DbExpressionVisitor
    {
        readonly QueryLanguage language;
        readonly Dictionary<ColumnExpression, ColumnExpression> map;
        readonly List<ColumnDeclaration> columns;
        readonly HashSet<string> columnNames;
        readonly HashSet<Expression> candidates;
        readonly HashSet<TableAlias> existingAliases;
        readonly TableAlias newAlias;
        int iColumn;

        private ColumnProjector(QueryLanguage language, ProjectionAffinity affinity, Expression expression, IEnumerable<ColumnDeclaration> existingColumns, TableAlias newAlias, IEnumerable<TableAlias> existingAliases)
        {
            this.language = language;
            this.newAlias = newAlias;
            this.existingAliases = new HashSet<TableAlias>(existingAliases);
            this.map = new Dictionary<ColumnExpression, ColumnExpression>();
            if (existingColumns != null)
            {
                this.columns = new List<ColumnDeclaration>(existingColumns);
                this.columnNames = new HashSet<string>(existingColumns.Select(c => c.Name));
            }
            else
            {
                this.columns = new List<ColumnDeclaration>();
                this.columnNames = new HashSet<string>();
            }
            this.candidates = Nominator.Nominate(language, affinity, expression);
        }

        public static ProjectedColumns ProjectColumns(QueryLanguage language, ProjectionAffinity affinity, Expression expression, IEnumerable<ColumnDeclaration> existingColumns, TableAlias newAlias, IEnumerable<TableAlias> existingAliases)
        {
            ColumnProjector projector = new ColumnProjector(language, affinity, expression, existingColumns, newAlias, existingAliases);
            Expression expr = projector.Visit(expression);
            return new ProjectedColumns(expr, projector.columns.AsReadOnly());
        }

        public static ProjectedColumns ProjectColumns(QueryLanguage language, Expression expression, IEnumerable<ColumnDeclaration> existingColumns, TableAlias newAlias, IEnumerable<TableAlias> existingAliases)
        {
            return ProjectColumns(language, ProjectionAffinity.Client, expression, existingColumns, newAlias, existingAliases);
        }

        public static ProjectedColumns ProjectColumns(QueryLanguage language, ProjectionAffinity affinity, Expression expression, IEnumerable<ColumnDeclaration> existingColumns, TableAlias newAlias, params TableAlias[] existingAliases)
        {
            return ProjectColumns(language, affinity, expression, existingColumns, newAlias, (IEnumerable<TableAlias>)existingAliases);
        }

        public static ProjectedColumns ProjectColumns(QueryLanguage language, Expression expression, IEnumerable<ColumnDeclaration> existingColumns, TableAlias newAlias, params TableAlias[] existingAliases)
        {
            return ProjectColumns(language, expression, existingColumns, newAlias, (IEnumerable<TableAlias>)existingAliases);
        }

        protected override Expression Visit(Expression expression)
        {
            if (this.candidates.Contains(expression))
            {
                if (expression.NodeType == (ExpressionType)DbExpressionType.Column)
                {
                    ColumnExpression column = (ColumnExpression)expression;
                    ColumnExpression mapped;
                    if (this.map.TryGetValue(column, out mapped))
                    {
                        return mapped;
                    }
                    // check for column that already refers to this column
                    foreach (ColumnDeclaration existingColumn in this.columns)
                    {
                        ColumnExpression cex = existingColumn.Expression as ColumnExpression;
                        if (cex != null && cex.Alias == column.Alias && cex.Name == column.Name)
                        {
                            // refer to the column already in the column list
                            return new ColumnExpression(column.Type, column.QueryType, this.newAlias, existingColumn.Name);
                        }
                    }
                    if (this.existingAliases.Contains(column.Alias)) 
                    {
                        int ordinal = this.columns.Count;
                        string columnName = this.GetUniqueColumnName(column.Name);
                        this.columns.Add(new ColumnDeclaration(columnName, column, column.QueryType));
                        mapped = new ColumnExpression(column.Type, column.QueryType, this.newAlias, columnName);
                        this.map.Add(column, mapped);
                        this.columnNames.Add(columnName);
                        return mapped;
                    }
                    // must be referring to outer scope
                    return column;
                }
                else
                {
                    string columnName = this.GetNextColumnName();
                    var colType = this.language.TypeSystem.GetColumnType(expression.Type);
                    this.columns.Add(new ColumnDeclaration(columnName, expression, colType));
                    return new ColumnExpression(expression.Type, colType, this.newAlias, columnName);
                }
            }
            else
            {
                return base.Visit(expression);
            }
        }

        private bool IsColumnNameInUse(string name)
        {
            return this.columnNames.Contains(name);
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
            return this.GetUniqueColumnName("c" + (iColumn++));
        }

        /// <summary>
        /// Nominator is a class that walks an expression tree bottom up, determining the set of 
        /// candidate expressions that are possible columns of a select expression
        /// </summary>
        class Nominator : DbExpressionVisitor
        {
            private readonly QueryLanguage language;
            private readonly HashSet<Expression> candidates;
            private readonly ProjectionAffinity affinity;
            private bool isBlocked;

            private Nominator(QueryLanguage language, ProjectionAffinity affinity)
            {
                this.language = language;
                this.affinity = affinity;
                this.candidates = new HashSet<Expression>();
                this.isBlocked = false;
            }

            internal static HashSet<Expression> Nominate(QueryLanguage language, ProjectionAffinity affinity, Expression expression)
            {
                Nominator nominator = new Nominator(language, affinity);
                nominator.Visit(expression);
                return nominator.candidates;
            }

            protected override Expression Visit(Expression expression)
            {
                if (expression != null)
                {
                    bool saveIsBlocked = this.isBlocked;
                    this.isBlocked = false;
                    if (this.language.MustBeColumn(expression))
                    {
                        this.candidates.Add(expression);
                        // don't merge saveIsBlocked
                    }
                    else
                    {
                        base.Visit(expression);
                        if (!this.isBlocked)
                        {
                            if (this.language.MustBeColumn(expression) 
                                || (this.affinity == ProjectionAffinity.Server && this.language.CanBeColumn(expression)))
                            {
                                this.candidates.Add(expression);
                            }
                            else 
                            {
                                this.isBlocked = true;
                            }
                        }
                        this.isBlocked |= saveIsBlocked;
                    }
                }
                return expression;
            }

            protected override Expression VisitProjection(ProjectionExpression proj)
            {
                this.Visit(proj.Projector);
                return proj;
            }
        }
    }
}
