﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
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
    /// A <see cref="QueryMapper"/> that can apply a <see cref="BasicEntityMapping"/> to a query expression.
    /// </summary>
    public class BasicMapper : QueryMapper
    {
        private readonly BasicEntityMapping _mapping;
        public override EntityMapping Mapping => _mapping;

        public BasicMapper(BasicEntityMapping mapping)
        {
            _mapping = mapping;
        }

        /// <summary>
        /// The query language specific type for the column
        /// </summary>
        public virtual QueryType GetColumnType(
            MappingEntity entity, 
            MemberInfo member,
            QueryLanguage language)
        {
            var dbType = _mapping.GetColumnDbType(entity, member);

            if (dbType != null)
            {
                return language.TypeSystem.Parse(dbType) ?? QueryType.Unknown;
            }

            return language.TypeSystem.GetQueryType(TypeHelper.GetMemberType(member))!;
        }

        public override ClientProjectionExpression GetQueryExpression(
            MappingEntity entity, 
            QueryLinguist linguist,
            QueryPolice police)
        {
            var tableAlias = new TableAlias();
            var selectAlias = new TableAlias();
            var table = new TableExpression(tableAlias, entity, _mapping.GetTableName(entity));

            Expression projector = this.GetEntityExpression(table, entity, linguist, police);
            var pc = ColumnProjector.ProjectColumns(linguist, projector, null, selectAlias, tableAlias);

            var proj = new ClientProjectionExpression(
                new SelectExpression(selectAlias, pc.Columns, table, null),
                pc.Projector
                );

            return (ClientProjectionExpression)police.ApplyPolicy(proj, entity.StaticType, linguist, this);
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
                    Expression me = this.GetMemberExpression(root, entity, mi, linguist, police);
                    if (me != null)
                    {
                        assignments.Add(new EntityAssignment(mi, me));
                    }
                }
            }

            return new EntityExpression(entity, BuildEntityExpression(entity, assignments));
        }

        public class EntityAssignment
        {
            public MemberInfo Member { get; }
            public Expression Expression { get; }

            public EntityAssignment(MemberInfo member, Expression expression)
            {
                if (member == null)
                    throw new ArgumentNullException(nameof(member));
                if (expression == null)
                    throw new ArgumentNullException(nameof(expression));

                this.Member = member;
                this.Expression = expression;
            }
        }

        protected virtual Expression BuildEntityExpression(MappingEntity entity, IReadOnlyList<EntityAssignment> assignments)
        {
            NewExpression newExpression;

            // handle cases where members are not directly assignable
            var readonlyMembers = assignments.Where(b => TypeHelper.IsReadOnly(b.Member)).ToArray();
            var cons = entity.RuntimeType.GetDeclaredConstructors();
            var hasNoArgConstructor = cons.Any(c => c.GetParameters().Length == 0);

            if (readonlyMembers.Length > 0 || !hasNoArgConstructor)
            {
                // find all the constructors that bind all the read-only members
                var consThatApply = cons
                    .Select(c => this.BindConstructor(c, readonlyMembers)!)
                    .Where(cbr => cbr != null && cbr.Remaining.Count == 0)
                    .ToList();

                if (consThatApply.Count == 0)
                {
                    throw new InvalidOperationException(string.Format("Cannot construct type '{0}' with all mapped and included members.", entity.RuntimeType));
                }

                // just use the first one... (Note: need better algorithm?)
                if (readonlyMembers.Length == assignments.Count)
                {
                    return consThatApply[0].Expression;
                }

                var r = this.BindConstructor(consThatApply[0].Expression.Constructor, assignments)!;
                newExpression = r.Expression; 
                assignments = r.Remaining;
            }
            else
            {
                newExpression = Expression.New(entity.RuntimeType);
            }

            Expression result;
            if (assignments.Count > 0)
            {
                if (entity.StaticType.IsInterface)
                {
                    assignments = this.RemapAssignments(assignments, entity.RuntimeType).ToList();
                }

                result = Expression.MemberInit(newExpression, (MemberBinding[])assignments.Select(a => Expression.Bind(a.Member, a.Expression)).ToArray());
            }
            else
            {
                result = newExpression;
            }

            if (entity.StaticType != entity.RuntimeType)
            {
                result = Expression.Convert(result, entity.StaticType);
            }

            return result;
        }

        private IEnumerable<EntityAssignment> RemapAssignments(IEnumerable<EntityAssignment> assignments, Type entityType)
        {
            foreach (var assign in assignments)
            {
                var member = TypeHelper.FindDeclaredFieldOrProperty(entityType, assign.Member.Name);
                if (member != null)
                {
                    yield return new EntityAssignment(member, assign.Expression);
                }
                else
                {
                    yield return assign;
                }
            }
        }

        /// <summary>
        /// Attempts to match up entity assignments with constructor parameters.
        /// Returns the <see cref="NewExpression"/> that constructs the entity with the matching assignment values
        /// and the remaining unused assignments.
        /// </summary>
        protected virtual ConstructorBindResult? BindConstructor(
            ConstructorInfo cons, 
            IReadOnlyList<EntityAssignment> assignments)
        {
            var ps = cons.GetParameters();
            var args = new Expression[ps.Length];
            var mis = new MemberInfo[ps.Length];
            var members = new HashSet<EntityAssignment>(assignments);
            var used = new HashSet<EntityAssignment>();

            for (int i = 0, n = ps.Length; i < n; i++)
            {
                ParameterInfo p = ps[i];
                
                var assignment = members.FirstOrDefault(a =>
                    p.Name == a.Member.Name
                    && p.ParameterType.IsAssignableFrom(a.Expression.Type));

                if (assignment == null)
                {
                    assignment = members.FirstOrDefault(a =>
                        string.Compare(p.Name, a.Member.Name, StringComparison.OrdinalIgnoreCase) == 0
                        && p.ParameterType.IsAssignableFrom(a.Expression.Type));
                }

                if (assignment != null)
                {
                    args[i] = assignment.Expression;
                    mis[i] = assignment.Member;
                    used.Add(assignment);
                }
                else
                {
                    // find member with same name as parameter and associate it in object initializer
                    MemberInfo mem = TypeHelper.GetDeclaredFieldsAndProperties(cons.DeclaringType).Where(m => string.Compare(m.Name, p.Name, StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                    if (mem != null)
                    {
                        args[i] = Expression.Constant(TypeHelper.GetDefault(p.ParameterType), p.ParameterType);
                        mis[i] = mem;
                    }
                    else
                    {
                        // unknown parameter, does not match any member
                        return null;
                    }
                }
            }

            members.ExceptWith(used);

            return new ConstructorBindResult(Expression.New(cons, args, mis), members);
        }

        protected class ConstructorBindResult
        {
            public NewExpression Expression { get; }
            public IReadOnlyList<EntityAssignment> Remaining { get; }

            public ConstructorBindResult(NewExpression expression, IEnumerable<EntityAssignment> remaining)
            {
                this.Expression = expression;
                this.Remaining = remaining.ToReadOnly();
            }
        }

        public override bool HasIncludedMembers(EntityExpression entity, QueryPolicy policy)
        {
            foreach (var mi in _mapping.GetMappedMembers(entity.Entity))
            {
                if (policy.IsIncluded(mi))
                    return true;
            }

            return false;
        }

        public override EntityExpression IncludeMembers(
            EntityExpression entity, 
            Func<MemberInfo, bool> fnIsIncluded,
            QueryLinguist linguist,
            QueryPolice police)
        {
            var assignments = this.GetAssignments(entity.Expression).ToDictionary(ma => ma.Member.Name);
            bool anyAdded = false;
            
            foreach (var mi in _mapping.GetMappedMembers(entity.Entity))
            {
                EntityAssignment ea;
                bool okayToInclude = !assignments.TryGetValue(mi.Name, out ea) || IsNullRelationshipAssignment(entity.Entity, ea);
                if (okayToInclude && fnIsIncluded(mi))
                {
                    ea = new EntityAssignment(mi, this.GetMemberExpression(entity.Expression, entity.Entity, mi, linguist, police));
                    assignments[mi.Name] = ea;
                    anyAdded = true;
                }
            }

            if (anyAdded)
            {
                return new EntityExpression(entity.Entity, this.BuildEntityExpression(entity.Entity, assignments.Values.ToList()));
            }

            return entity;
        }

        private bool IsNullRelationshipAssignment(MappingEntity entity, EntityAssignment assignment)
        {
            if (_mapping.IsRelationship(entity, assignment.Member))
            {
                var cex = assignment.Expression as ConstantExpression;
                if (cex != null && cex.Value == null)
                    return true;
            }
            return false;
        }


        private IEnumerable<EntityAssignment> GetAssignments(Expression newOrMemberInit)
        {
            var assignments = new List<EntityAssignment>();
            var minit = newOrMemberInit as MemberInitExpression;
            if (minit != null)
            {
                assignments.AddRange(minit.Bindings.OfType<MemberAssignment>().Select(a => new EntityAssignment(a.Member, a.Expression)));
                newOrMemberInit = minit.NewExpression;
            }
            var nex = newOrMemberInit as NewExpression;
            if (nex != null && nex.Members != null)
            {
                assignments.AddRange(
                    Enumerable.Range(0, nex.Arguments.Count)
                              .Where(i => nex.Members[i] != null)
                              .Select(i => new EntityAssignment(nex.Members[i], nex.Arguments[i]))
                              );
            }
            return assignments;
        }

        public override Expression GetMemberExpression(
            Expression root, 
            MappingEntity entity, 
            MemberInfo member,
            QueryLinguist linguist,
            QueryPolice police)
        {
            if (_mapping.IsAssociationRelationship(entity, member))
            {
                var relatedEntity = _mapping.GetRelatedEntity(entity, member);
                var projection = this.GetQueryExpression(relatedEntity, linguist, police);

                // make where clause for joining back to 'root'
                var declaredTypeMembers = _mapping.GetAssociationKeyMembers(entity, member).ToList();
                var associatedMembers = _mapping.GetAssociationRelatedKeyMembers(entity, member).ToList();

                Expression? where = null;

                for (int i = 0, n = associatedMembers.Count; i < n; i++)
                {
                    Expression equal =
                        this.GetMemberExpression(projection.Projector, relatedEntity, associatedMembers[i], linguist, police)
                            .Equal(this.GetMemberExpression(root, entity, declaredTypeMembers[i], linguist, police));
                    where = (where != null) ? where.And(equal) : equal;
                }

                TableAlias newAlias = new TableAlias();
                var pc = ColumnProjector.ProjectColumns(linguist, projection.Projector, null, newAlias, projection.Select.Alias);

                var aggregator = Aggregator.GetAggregator(TypeHelper.GetMemberType(member), typeof(IEnumerable<>).MakeGenericType(pc.Projector.Type));
                var result = new ClientProjectionExpression(
                    new SelectExpression(newAlias, pc.Columns, projection.Select, where),
                    pc.Projector, aggregator
                    );

                return police.ApplyPolicy(result, member, linguist, this);
            }
            else
            {
                if (root is AliasedExpression aliasedRoot 
                    && _mapping.IsColumn(entity, member)
                    && this.GetColumnType(entity, member, linguist.Language) is { } columnType)
                {
                    return new ColumnExpression(
                        TypeHelper.GetMemberType(member), 
                        columnType, 
                        aliasedRoot.Alias, 
                        _mapping.GetColumnName(entity, member)
                        );
                }

                return root.ResolveMemberAccess(member);
            }
        }

        public override Expression GetInsertExpression(
            MappingEntity entity, 
            Expression instance, 
            LambdaExpression? selector,
            QueryLinguist linguist,
            QueryPolice police)
        {
            var tableAlias = new TableAlias();
            var table = new TableExpression(tableAlias, entity, _mapping.GetTableName(entity));
            var assignments = this.GetColumnAssignments(
                table, 
                instance, 
                entity, 
                (e, m) => !(_mapping.IsGenerated(e, m) || _mapping.IsReadOnly(e, m)),
                linguist,
                police
                );

            if (selector != null)
            {
                return new BlockCommand(
                    new InsertCommand(table, assignments),
                    this.GetInsertResult(entity, instance, selector, null, linguist, police)
                    );
            }

            return new InsertCommand(table, assignments);
        }

        private IEnumerable<ColumnAssignment> GetColumnAssignments(
            Expression table, 
            Expression instance, 
            MappingEntity entity, 
            Func<MappingEntity, MemberInfo, bool> fnIncludeColumn,
            QueryLinguist linguist,
            QueryPolice police)
        {
            foreach (var m in _mapping.GetMappedMembers(entity))
            {
                if (_mapping.IsColumn(entity, m) && fnIncludeColumn(entity, m))
                {
                    yield return new ColumnAssignment(
                        (ColumnExpression)this.GetMemberExpression(table, entity, m, linguist, police),
                        Expression.MakeMemberAccess(instance, m)
                        );
                }
            }
        }

        protected virtual Expression GetInsertResult(
            MappingEntity entity, 
            Expression instance, 
            LambdaExpression selector, 
            Dictionary<MemberInfo, Expression>? map,
            QueryLinguist linguist,
            QueryPolice police)
        {
            var tableAlias = new TableAlias();
            var tex = new TableExpression(tableAlias, entity, _mapping.GetTableName(entity));
            var aggregator = Aggregator.GetAggregator(selector.Body.Type, typeof(IEnumerable<>).MakeGenericType(selector.Body.Type));

            Expression where;
            DeclarationCommand? genIdCommand = null;
            var generatedIds = _mapping.GetMappedMembers(entity).Where(m => _mapping.IsPrimaryKey(entity, m) && _mapping.IsGenerated(entity, m)).ToList();
            if (generatedIds.Count > 0)
            {
                if (map == null || !generatedIds.Any(m => map.ContainsKey(m)))
                {
                    var localMap = new Dictionary<MemberInfo, Expression>();
                    genIdCommand = this.GetGeneratedIdCommand(entity, generatedIds.ToList(), localMap, linguist);
                    map = localMap;
                }

                // is this just a retrieval of one generated id member?
                var mex = selector.Body as MemberExpression;
                if (mex != null && _mapping.IsPrimaryKey(entity, mex.Member) && _mapping.IsGenerated(entity, mex.Member))
                {
                    if (genIdCommand != null && genIdCommand.Source != null)
                    {
                        // just use the select from the genIdCommand
                        return new ClientProjectionExpression(
                            genIdCommand.Source,
                            new ColumnExpression(mex.Type, genIdCommand.Variables[0].QueryType, genIdCommand.Source.Alias, genIdCommand.Source.Columns[0].Name),
                            aggregator
                            );
                    }
                    else
                    {
                        TableAlias alias = new TableAlias();
                        var colType = this.GetColumnType(entity, mex.Member, linguist.Language);
                        return new ClientProjectionExpression(
                            new SelectExpression(alias, new[] { new ColumnDeclaration("", map[mex.Member], colType) }, null, null),
                            new ColumnExpression(TypeHelper.GetMemberType(mex.Member), colType, alias, ""),
                            aggregator
                            );
                    }
                }

                where = generatedIds.Select((m, i) =>
                    this.GetMemberExpression(tex, entity, m, linguist, police).Equal(map[m])
                    ).Aggregate((x, y) => x.And(y));
            }
            else
            {
                where = this.GetIdentityCheck(tex, entity, instance, linguist, police);
            }

            var typeProjector = this.GetEntityExpression(tex, entity, linguist, police);
            var selection = selector.Body.Replace(selector.Parameters[0], typeProjector);
            var newAlias = new TableAlias();
            var pc = ColumnProjector.ProjectColumns(linguist, selection, null, newAlias, tableAlias);
            var pe = new ClientProjectionExpression(
                new SelectExpression(newAlias, pc.Columns, tex, where),
                pc.Projector,
                aggregator
                );

            if (genIdCommand != null)
            {
                return new BlockCommand(genIdCommand, pe);
            }

            return pe;
        }

        protected virtual DeclarationCommand GetGeneratedIdCommand(
            MappingEntity entity, 
            List<MemberInfo> members, 
            Dictionary<MemberInfo, Expression> map,
            QueryLinguist linguist)
        {
            var columns = new List<ColumnDeclaration>();
            var decls = new List<VariableDeclaration>();
            var alias = new TableAlias();

            foreach (var member in members)
            {
                var genId = linguist.GetGeneratedIdExpression(member);
                var name = member.Name;
                var colType = this.GetColumnType(entity, member, linguist.Language);
                
                columns.Add(new ColumnDeclaration(member.Name, genId, colType));
                decls.Add(new VariableDeclaration(member.Name, colType, new ColumnExpression(genId.Type, colType, alias, member.Name)));

                if (map != null)
                {
                    var vex = new VariableExpression(member.Name, TypeHelper.GetMemberType(member), colType);
                    map.Add(member, vex);
                }
            }

            var select = new SelectExpression(alias, columns, null, null);

            return new DeclarationCommand(decls, select);
        }

        protected virtual Expression GetIdentityCheck(
            Expression root, 
            MappingEntity entity, 
            Expression instance,
            QueryLinguist linguist,
            QueryPolice police)
        {
            return _mapping.GetMappedMembers(entity)
                .Where(m => _mapping.IsPrimaryKey(entity, m))
                .Select(m => this.GetMemberExpression(root, entity, m, linguist, police)
                    .Equal(Expression.MakeMemberAccess(instance, m)))
                .Aggregate((x, y) => x.And(y));
        }

        protected virtual Expression GetEntityExistsTest(
            MappingEntity entity, 
            Expression instance,
            QueryLinguist linguist,
            QueryPolice police)
        {
            ClientProjectionExpression tq = this.GetQueryExpression(entity, linguist, police);
            Expression where = this.GetIdentityCheck(tq.Select, entity, instance, linguist, police);
            return new ExistsSubqueryExpression(new SelectExpression(new TableAlias(), null, tq.Select, where));
        }

        protected virtual Expression GetEntityStateTest(
            MappingEntity entity,
            Expression? instance,
            LambdaExpression updateCheck,
            QueryLinguist linguist,
            QueryPolice police)
        {
            var tq = this.GetQueryExpression(entity, linguist, police);
            var where = instance != null ? this.GetIdentityCheck(tq.Select, entity, instance, linguist, police) : null;
            var check = updateCheck.Body.Replace(updateCheck.Parameters[0], tq.Projector);
            where = where != null ? where.And(check) : check;
            return new ExistsSubqueryExpression(
                new SelectExpression(new TableAlias(), null, tq.Select, where));
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
            var tableAlias = new TableAlias();
            var table = new TableExpression(tableAlias, entity, _mapping.GetTableName(entity));

            var where = this.GetIdentityCheck(table, entity, instance, linguist, police);
            if (updateCheck != null)
            {
                var typeProjector = this.GetEntityExpression(table, entity, linguist, police);
                var pred = updateCheck.Body.Replace(updateCheck.Parameters[0], typeProjector);
                where = where.And(pred);
            }

            var assignments = this.GetColumnAssignments(
                table, 
                instance, 
                entity, 
                (e, m) => _mapping.IsUpdatable(e, m),
                linguist,
                police
                );

            Expression update = new UpdateCommand(table, where, assignments);

            if (selector != null)
            {
                return new BlockCommand(
                    update,
                    new IfCommand(
                        linguist.GetRowsAffectedExpression(update).GreaterThan(Expression.Constant(0)),
                        this.GetUpdateResult(entity, instance, selector, linguist, police),
                        @else
                        )
                    );
            }
            else if (@else != null)
            {
                return new BlockCommand(
                    update,
                    new IfCommand(
                        linguist.GetRowsAffectedExpression(update).LessThanOrEqual(Expression.Constant(0)),
                        @else,
                        null
                        )
                    );
            }
            else
            {
                return update;
            }
        }

        protected virtual Expression GetUpdateResult(
            MappingEntity entity, 
            Expression instance, 
            LambdaExpression selector,
            QueryLinguist linguist,
            QueryPolice police)
        {
            var tq = this.GetQueryExpression(entity, linguist, police);
            var where = this.GetIdentityCheck(tq.Select, entity, instance, linguist, police);
            var selection = selector.Body.Replace(selector.Parameters[0], tq.Projector);
            var newAlias = new TableAlias();
            var pc = ColumnProjector.ProjectColumns(linguist, selection, null, newAlias, tq.Select.Alias);

            return new ClientProjectionExpression(
                new SelectExpression(newAlias, pc.Columns, tq.Select, where),
                pc.Projector,
                Aggregator.GetAggregator(selector.Body.Type, typeof(IEnumerable<>).MakeGenericType(selector.Body.Type))
                );
        }

        public override Expression GetInsertOrUpdateExpression(
            MappingEntity entity, 
            Expression instance, 
            LambdaExpression? updateCheck, 
            LambdaExpression? resultSelector,
            QueryLinguist linguist,
            QueryPolice police)
        {
            if (updateCheck != null)
            {
                Expression insert = this.GetInsertExpression(entity, instance, resultSelector, linguist, police);
                Expression update = this.GetUpdateExpression(entity, instance, updateCheck, resultSelector, null, linguist, police);
                var check = this.GetEntityExistsTest(entity, instance, linguist, police);
                return new IfCommand(check, update, insert);
            }
            else
            {
                Expression insert = this.GetInsertExpression(entity, instance, resultSelector, linguist, police);
                Expression update = this.GetUpdateExpression(entity, instance, updateCheck, resultSelector, insert, linguist, police);
                return update;
            }
        }

        public override Expression GetDeleteExpression(
            MappingEntity entity, 
            Expression? instance, 
            LambdaExpression? deleteCheck,
            QueryLinguist linguist,
            QueryPolice police)
        {
            var table = new TableExpression(new TableAlias(), entity, _mapping.GetTableName(entity));
            Expression? where = null;

            if (instance != null)
            {
                where = this.GetIdentityCheck(table, entity, instance, linguist, police);
            }

            if (deleteCheck != null)
            {
                var row = this.GetEntityExpression(table, entity, linguist, police);
                var pred = deleteCheck.Body.Replace(deleteCheck.Parameters[0], row);
                where = (where != null) ? where.And(pred) : pred;
            }

            return new DeleteCommand(table, where);
        }
    }
}