// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Entities.Translation
{
    using Expressions;
    using Expressions.Sql;
    using Mapping;
    using Utils;

    /// <summary>
    /// A <see cref="QueryMapper"/> that can apply an <see cref="AdvancedEntityMapping"/> to a query.
    /// </summary>
    public class AdvancedMapper : BasicMapper
    {
        private readonly AdvancedEntityMapping _mapping;

        public AdvancedMapper(AdvancedEntityMapping mapping)
            : base(mapping)
        {
            _mapping = mapping;
        }

        /// <summary>
        /// Gets a set of related <see cref="MappingTable"/>'s in dependency order.
        /// </summary>
        public virtual IEnumerable<MappingTable> GetDependencyOrderedTables(MappingEntity entity)
        {
            var lookup = _mapping.GetTables(entity).ToLookup(t => _mapping.GetTableId(t));
            return _mapping.GetTables(entity)
                .Sort(t => _mapping.IsExtensionTable(t) 
                    ? lookup[_mapping.GetExtensionRelatedTableId(t)] 
                    : null);
        }

        public override EntityExpression GetEntityExpression(
            Expression root, 
            MappingEntity entity,
            QueryLinguist linguist,
            QueryPolice police)
        {
            // must be some complex type constructed from multiple columns
            var assignments = new List<EntityAssignment>();
            foreach (MemberInfo mi in _mapping.GetMappedMembers(entity))
            {
                if (!_mapping.IsAssociationRelationship(entity, mi))
                {
                    Expression me;
                    if (_mapping.IsNestedEntity(entity, mi))
                    {
                        me = this.GetEntityExpression(root, _mapping.GetRelatedEntity(entity, mi), linguist, police);
                    }
                    else
                    {
                        me = this.GetMemberExpression(root, entity, mi, linguist, police);
                    }
                    if (me != null)
                    {
                        assignments.Add(new EntityAssignment(mi, me));
                    }
                }
            }

            return new EntityExpression(entity, this.BuildEntityExpression(entity, assignments));
        }

        public override Expression GetMemberExpression(
            Expression root, 
            MappingEntity entity, 
            MemberInfo member,
            QueryLinguist linguist,
            QueryPolice police)
        {
            if (_mapping.IsNestedEntity(entity, member))
            {
                MappingEntity subEntity = _mapping.GetRelatedEntity(entity, member);
                return this.GetEntityExpression(root, subEntity, linguist, police);
            }
            else
            {
                return base.GetMemberExpression(root, entity, member, linguist, police);
            }
        }

        public override ClientProjectionExpression GetQueryExpression(
            MappingEntity entity,
            QueryLinguist linguist,
            QueryPolice police)
        {
            var tables = _mapping.GetTables(entity);
            if (tables.Count <= 1)
            {
                return base.GetQueryExpression(entity, linguist, police);
            }

            var aliases = new Dictionary<string, TableAlias>();
            var rootTable = tables.Single(ta => !_mapping.IsExtensionTable(ta));
            var tex = new TableExpression(new TableAlias(), entity, _mapping.GetTableName(rootTable));
            aliases.Add(_mapping.GetTableId(rootTable), tex.Alias);
            Expression source = tex;

            foreach (MappingTable table in tables.Where(t => _mapping.IsExtensionTable(t)))
            {
                var joinedTableAlias = new TableAlias();
                var extensionAlias = _mapping.GetTableId(table);
                aliases.Add(extensionAlias, joinedTableAlias);

                var keyColumns = _mapping.GetExtensionKeyColumnNames(table).ToList();
                var relatedMembers = _mapping.GetExtensionRelatedMembers(table).ToList();
                var relatedAlias = _mapping.GetExtensionRelatedTableId(table);

                TableAlias relatedTableAlias;
                aliases.TryGetValue(relatedAlias, out relatedTableAlias);

                var joinedTex = new TableExpression(joinedTableAlias, entity, _mapping.GetTableName(table));

                Expression? cond = null;
                for (int i = 0, n = keyColumns.Count; i < n; i++)
                {
                    var memberType = TypeHelper.GetMemberType(relatedMembers[i]);
                    var colType = this.GetColumnType(entity, relatedMembers[i], linguist.Language);
                    var relatedColumn = new ColumnExpression(memberType, colType, relatedTableAlias, _mapping.GetColumnName(entity, relatedMembers[i]));
                    var joinedColumn = new ColumnExpression(memberType, colType, joinedTableAlias, keyColumns[i]);
                    var eq = joinedColumn.Equal(relatedColumn);
                    cond = (cond != null) ? cond.And(eq) : eq;
                }

                source = new JoinExpression(JoinType.SingletonLeftOuterJoin, source, joinedTex, cond);
            }

            var columns = new List<ColumnDeclaration>();
            this.GetColumns(entity, aliases, columns, linguist, police);
            var root = new SelectExpression(new TableAlias(), columns, source, null);
            var existingAliases = aliases.Values.ToArray();

            var projector = this.GetEntityExpression(root, entity, linguist, police);
            var selectAlias = new TableAlias();
            var pc = ColumnProjector.ProjectColumns(linguist, projector, null, selectAlias, root.Alias);
            var proj = new ClientProjectionExpression(
                new SelectExpression(selectAlias, pc.Columns, root, null),
                pc.Projector
                );

            return (ClientProjectionExpression)police.ApplyPolicy(proj, entity.StaticType, linguist, this);
        }

        private void GetColumns(
            MappingEntity entity,
            Dictionary<string, TableAlias> aliases,
            List<ColumnDeclaration> columns,
            QueryLinguist linguist,
            QueryPolice police)
        {
            foreach (MemberInfo mi in _mapping.GetMappedMembers(entity))
            {
                if (!_mapping.IsAssociationRelationship(entity, mi))
                {
                    if (_mapping.IsNestedEntity(entity, mi))
                    {
                        this.GetColumns(_mapping.GetRelatedEntity(entity, mi), aliases, columns, linguist, police);
                    }
                    else if (_mapping.IsColumn(entity, mi))
                    {
                        string name = _mapping.GetColumnName(entity, mi);
                        string aliasName = _mapping.GetTableId(entity, mi);
                        TableAlias alias;
                        aliases.TryGetValue(aliasName, out alias);
                        var colType = this.GetColumnType(entity, mi, linguist.Language);
                        ColumnExpression ce = new ColumnExpression(TypeHelper.GetMemberType(mi), colType, alias, name);
                        ColumnDeclaration cd = new ColumnDeclaration(name, ce, colType);
                        columns.Add(cd);
                    }
                }
            }
        }

        public override Expression GetInsertExpression(
            MappingEntity entity, 
            Expression instance, 
            LambdaExpression? selector,
            QueryLinguist linguist,
            QueryPolice police)
        {
            var tables = _mapping.GetTables(entity);
            if (tables.Count < 2)
            {
                return base.GetInsertExpression(entity, instance, selector, linguist, police);
            }

            var commands = new List<Expression>();

            var map = this.GetDependentGeneratedColumns(entity);
            var vexMap = new Dictionary<MemberInfo, Expression>();

            foreach (var table in this.GetDependencyOrderedTables(entity))
            {
                var tableAlias = new TableAlias();
                var tex = new TableExpression(tableAlias, entity, _mapping.GetTableName(table));
                var assignments = this.GetColumnAssignments(
                    tex, 
                    instance, 
                    entity,
                    (e, m) => _mapping.GetTableId(e, m) == _mapping.GetTableId(table) && !_mapping.IsGenerated(e, m),
                    vexMap,
                    linguist,
                    police
                    );
                var totalAssignments = assignments.Concat(
                    this.GetRelatedColumnAssignments(tex, entity, table, vexMap, linguist, police)
                    );
                commands.Add(new InsertCommand(tex, totalAssignments));

                List<MemberInfo> members;
                if (map.TryGetValue(_mapping.GetTableId(table), out members))
                {
                    var d = this.GetDependentGeneratedVariableDeclaration(entity, table, members, instance, vexMap, linguist, police);
                    commands.Add(d);
                }
            }

            if (selector != null)
            {
                commands.Add(this.GetInsertResult(entity, instance, selector, vexMap, linguist, police));
            }

            return new BlockCommand(commands);
        }

        private Dictionary<string, List<MemberInfo>> GetDependentGeneratedColumns(MappingEntity entity)
        {
            return
                (from xt in _mapping.GetTables(entity).Where(t => _mapping.IsExtensionTable(t))
                 group xt by _mapping.GetExtensionRelatedTableId(xt))
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(xt => _mapping.GetExtensionRelatedMembers(xt)).Distinct().ToList()
                );
        }

        // make a variable declaration / initialization for dependent generated values
        private CommandExpression GetDependentGeneratedVariableDeclaration(
            MappingEntity entity, 
            MappingTable table, 
            List<MemberInfo> members, 
            Expression instance, 
            Dictionary<MemberInfo, Expression> map,
            QueryLinguist linguist,
            QueryPolice police)
        {
            // first make command that retrieves the generated ids if any
            DeclarationCommand? genIdCommand = null;
            
            var generatedIds = _mapping.GetMappedMembers(entity)
                .Where(m => _mapping.IsPrimaryKey(entity, m) && _mapping.IsGenerated(entity, m))
                .ToList();

            if (generatedIds.Count > 0)
            {
                genIdCommand = this.GetGeneratedIdCommand(entity, members, map, linguist);

                // if that's all there is then just return the generated ids
                if (members.Count == generatedIds.Count)
                {
                    return genIdCommand;
                }
            }

            // next make command that retrieves the generated members
            // only consider members that were not generated ids
            members = members.Except(generatedIds).ToList();

            var tableAlias = new TableAlias();
            var tex = new TableExpression(tableAlias, entity, _mapping.GetTableName(table));

            Expression? where = null;
            if (generatedIds.Count > 0)
            {
                where = generatedIds.Select((m, i) =>
                    this.GetMemberExpression(tex, entity, m, linguist, police)
                    .Equal(map[m]))
                    .Aggregate((x, y) => x.And(y));
            }
            else
            {
                where = this.GetIdentityCheck(tex, entity, instance, linguist, police);
            }

            var selectAlias = new TableAlias();
            var columns = new List<ColumnDeclaration>();
            var variables = new List<VariableDeclaration>();

            foreach (var mi in members)
            {
                var col = (ColumnExpression)this.GetMemberExpression(tex, entity, mi, linguist, police);
                columns.Add(new ColumnDeclaration(_mapping.GetColumnName(entity, mi), col, col.QueryType));
                ColumnExpression vcol = new ColumnExpression(col.Type, col.QueryType, selectAlias, col.Name);
                variables.Add(new VariableDeclaration(mi.Name, col.QueryType, vcol));
                map.Add(mi, new VariableExpression(mi.Name, col.Type, col.QueryType));
            }

            var genMembersCommand = new DeclarationCommand(variables, new SelectExpression(selectAlias, columns, tex, where));

            if (genIdCommand != null)
            {
                return new BlockCommand(genIdCommand, genMembersCommand);
            }

            return genMembersCommand;
        }

        private IEnumerable<ColumnAssignment> GetColumnAssignments(
            Expression table,
            Expression instance, 
            MappingEntity entity,
            Func<MappingEntity, MemberInfo, bool> fnIncludeColumn,
            Dictionary<MemberInfo, Expression>? map,
            QueryLinguist linguist,
            QueryPolice police)
        {
            foreach (var m in _mapping.GetMappedMembers(entity))
            {
                if (_mapping.IsColumn(entity, m) && fnIncludeColumn(entity, m))
                {
                    yield return new ColumnAssignment(
                        (ColumnExpression)this.GetMemberExpression(table, entity, m, linguist, police),
                        this.GetMemberAccess(instance, m, map)
                        );
                }
                else if (_mapping.IsNestedEntity(entity, m))
                {
                    var assignments = this.GetColumnAssignments(
                        table,
                        Expression.MakeMemberAccess(instance, m),
                        _mapping.GetRelatedEntity(entity, m),
                        fnIncludeColumn,
                        map,
                        linguist,
                        police
                        );

                    foreach (var ca in assignments)
                    {
                        yield return ca;
                    }
                }
            }
        }

        private IEnumerable<ColumnAssignment> GetRelatedColumnAssignments(
            Expression expr,
            MappingEntity entity,
            MappingTable table,
            Dictionary<MemberInfo, Expression> map,
            QueryLinguist linguist,
            QueryPolice police)
        {
            if (_mapping.IsExtensionTable(table))
            {
                var keyColumns = _mapping.GetExtensionKeyColumnNames(table).ToArray();
                var relatedMembers = _mapping.GetExtensionRelatedMembers(table).ToArray();
                for (int i = 0, n = keyColumns.Length; i < n; i++)
                {
                    MemberInfo member = relatedMembers[i];
                    Expression exp = map[member];
                    yield return new ColumnAssignment(
                        (ColumnExpression)this.GetMemberExpression(expr, entity, member, linguist, police), 
                        exp);
                }
            }
        }

        private Expression GetMemberAccess(
            Expression instance, 
            MemberInfo member, 
            Dictionary<MemberInfo, Expression>? map)
        {
            Expression exp;
            if (map == null || !map.TryGetValue(member, out exp))
            {
                exp = Expression.MakeMemberAccess(instance, member);
            }
            return exp;
        }

        public override Expression GetUpdateExpression(
            MappingEntity entity, 
            Expression instance, 
            LambdaExpression? updateCheck, 
            LambdaExpression? selector, 
            Expression? @else,
            QueryLinguist linguist,
            QueryPolice police)
        {
            var tables = _mapping.GetTables(entity);
            if (tables.Count < 2)
            {
                return base.GetUpdateExpression(entity, instance, updateCheck, selector, @else, linguist, police);
            }

            var commands = new List<Expression>();
            foreach (var table in this.GetDependencyOrderedTables(entity))
            {
                TableExpression tex = new TableExpression(new TableAlias(), entity, _mapping.GetTableName(table));
                var assignments = this.GetColumnAssignments(
                    tex, 
                    instance, 
                    entity, 
                    (e, m) => _mapping.GetTableId(e, m) == _mapping.GetTableId(table) && _mapping.IsUpdatable(e, m), 
                    null, 
                    linguist, 
                    police
                    );

                var where = this.GetIdentityCheck(tex, entity, instance, linguist, police);
                commands.Add(new UpdateCommand(tex, where, assignments));
            }

            if (selector != null)
            {
                commands.Add(
                    new IfCommand(
                        linguist.GetRowsAffectedExpression(commands[commands.Count - 1]).GreaterThan(Expression.Constant(0)),
                        this.GetUpdateResult(entity, instance, selector, linguist, police),
                        @else
                        )
                    );
            }
            else if (@else != null)
            {
                commands.Add(
                    new IfCommand(
                        linguist.GetRowsAffectedExpression(commands[commands.Count - 1]).LessThanOrEqual(Expression.Constant(0)),
                        @else,
                        null
                        )
                    );
            }

            Expression block = new BlockCommand(commands);

            if (updateCheck != null)
            {
                var test = this.GetEntityStateTest(entity, instance, updateCheck, linguist, police);
                return new IfCommand(test, block, null);
            }

            return block;
        }

        private Expression? GetIdentityCheck(
            TableExpression root, 
            MappingEntity entity, 
            Expression instance, 
            MappingTable table,
            QueryLinguist linguist,
            QueryPolice police)
        {
            if (_mapping.IsExtensionTable(table))
            {
                var keyColNames = _mapping.GetExtensionKeyColumnNames(table).ToArray();
                var relatedMembers = _mapping.GetExtensionRelatedMembers(table).ToArray();

                Expression? where = null;
                for (int i = 0, n = keyColNames.Length; i < n; i++)
                {
                    var relatedMember = relatedMembers[i];
                    var cex = new ColumnExpression(
                        TypeHelper.GetMemberType(relatedMember), 
                        this.GetColumnType(entity, relatedMember, linguist.Language), 
                        root.Alias, 
                        keyColNames[n]
                        );
                    var nex = this.GetMemberExpression(instance, entity, relatedMember, linguist, police);
                    var eq = cex.Equal(nex);
                    where = (where != null) ? where.And(eq) : where;
                }

                return where;
            }
            else
            {
                return base.GetIdentityCheck(root, entity, instance, linguist, police);
            }
        }

        public override Expression GetDeleteExpression(
            MappingEntity entity, 
            Expression? instance, 
            LambdaExpression? deleteCheck,
            QueryLinguist linguist,
            QueryPolice police)
        {
            var tables = _mapping.GetTables(entity);
            if (tables.Count < 2)
            {
                return base.GetDeleteExpression(entity, instance, deleteCheck, linguist, police);
            }

            if (instance != null)
            {
                var commands = new List<Expression>();

                foreach (var table in this.GetDependencyOrderedTables(entity).Reverse())
                {
                    TableExpression tex = new TableExpression(new TableAlias(), entity, _mapping.GetTableName(table));
                    var where = this.GetIdentityCheck(tex, entity, instance, linguist, police);
                    commands.Add(new DeleteCommand(tex, where));
                }

                Expression block = new BlockCommand(commands);

                if (deleteCheck != null)
                {
                    var test = this.GetEntityStateTest(entity, instance, deleteCheck, linguist, police);
                    return new IfCommand(test, block, null);
                }

                return block;
            }
            else
            {
                var commands = new List<Expression>();

                foreach (var table in this.GetDependencyOrderedTables(entity).Reverse())
                {
                    var tex = new TableExpression(new TableAlias(), entity, _mapping.GetTableName(table));
                    var where = deleteCheck != null 
                        ? this.GetEntityStateTest(entity, null, deleteCheck, linguist, police) 
                        : null;
                    commands.Add(new DeleteCommand(tex, where));
                }

                return new BlockCommand(commands);
            }
        }
    }
}