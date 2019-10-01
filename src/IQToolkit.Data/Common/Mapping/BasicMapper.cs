// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// A <see cref="QueryMapper"/> that can apply a <see cref="BasicMapping"/> to a query expression.
    /// </summary>
    public class BasicMapper : QueryMapper
    {
        private readonly BasicMapping mapping;
        private readonly QueryTranslator translator;

        public BasicMapper(BasicMapping mapping, QueryTranslator translator)
        {
            this.mapping = mapping;
            this.translator = translator;
        }

        public override QueryMapping Mapping
        {
            get { return this.mapping; }
        }

        public override QueryTranslator Translator
        {
            get { return this.translator; }
        }

        /// <summary>
        /// The query language specific type for the column
        /// </summary>
        public virtual QueryType GetColumnType(MappingEntity entity, MemberInfo member)
        {
            string dbType = this.mapping.GetColumnDbType(entity, member);

            if (dbType != null)
            {
                return this.translator.Linguist.Language.TypeSystem.Parse(dbType);
            }

            return this.translator.Linguist.Language.TypeSystem.GetColumnType(TypeHelper.GetMemberType(member));
        }

        public override ProjectionExpression GetQueryExpression(MappingEntity entity)
        {
            var tableAlias = new TableAlias();
            var selectAlias = new TableAlias();
            var table = new TableExpression(tableAlias, entity, this.mapping.GetTableName(entity));

            Expression projector = this.GetEntityExpression(table, entity);
            var pc = ColumnProjector.ProjectColumns(this.translator.Linguist.Language, projector, null, selectAlias, tableAlias);

            var proj = new ProjectionExpression(
                new SelectExpression(selectAlias, pc.Columns, table, null),
                pc.Projector
                );

            return (ProjectionExpression)this.Translator.Police.ApplyPolicy(proj, entity.StaticType.GetTypeInfo());
        }

        public override EntityExpression GetEntityExpression(Expression root, MappingEntity entity)
        {
            // must be some complex type constructed from multiple columns
            var assignments = new List<EntityAssignment>();
            foreach (MemberInfo mi in this.mapping.GetMappedMembers(entity))
            {
                if (!this.mapping.IsAssociationRelationship(entity, mi))
                {
                    Expression me = this.GetMemberExpression(root, entity, mi);
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

        protected virtual Expression BuildEntityExpression(MappingEntity entity, IList<EntityAssignment> assignments)
        {
            NewExpression newExpression;

            // handle cases where members are not directly assignable
            EntityAssignment[] readonlyMembers = assignments.Where(b => TypeHelper.IsReadOnly(b.Member)).ToArray();
            ConstructorInfo[] cons = entity.RuntimeType.GetTypeInfo().DeclaredConstructors.Where(c => c.IsPublic && !c.IsStatic).ToArray();
            bool hasNoArgConstructor = cons.Any(c => c.GetParameters().Length == 0);

            if (readonlyMembers.Length > 0 || !hasNoArgConstructor)
            {
                // find all the constructors that bind all the read-only members
                var consThatApply = cons.Select(c => this.BindConstructor(c, readonlyMembers))
                                        .Where(cbr => cbr != null && cbr.Remaining.Count == 0).ToList();
                if (consThatApply.Count == 0)
                {
                    throw new InvalidOperationException(string.Format("Cannot construct type '{0}' with all mapped and included members.", entity.RuntimeType));
                }

                // just use the first one... (Note: need better algorithm?)
                if (readonlyMembers.Length == assignments.Count)
                {
                    return consThatApply[0].Expression;
                }

                var r = this.BindConstructor(consThatApply[0].Expression.Constructor, assignments);

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
                if (entity.StaticType.GetTypeInfo().IsInterface)
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
                var member = TypeHelper.GetDataMember(entityType, assign.Member.Name);
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

        protected virtual ConstructorBindResult BindConstructor(ConstructorInfo cons, IList<EntityAssignment> assignments)
        {
            var ps = cons.GetParameters();
            var args = new Expression[ps.Length];
            var mis = new MemberInfo[ps.Length];
            HashSet<EntityAssignment> members = new HashSet<EntityAssignment>(assignments);
            HashSet<EntityAssignment> used = new HashSet<EntityAssignment>();

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
                    if (mis != null)
                        mis[i] = assignment.Member;
                    used.Add(assignment);
                }
                else
                {
                    // find member with same name as parameter and associate it in object initializer
                    MemberInfo mem = TypeHelper.GetDataMembers(cons.DeclaringType).Where(m => string.Compare(m.Name, p.Name, StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
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
            public ReadOnlyCollection<EntityAssignment> Remaining { get; }

            public ConstructorBindResult(NewExpression expression, IEnumerable<EntityAssignment> remaining)
            {
                this.Expression = expression;
                this.Remaining = remaining.ToReadOnly();
            }
        }

        public override bool HasIncludedMembers(EntityExpression entity)
        {
            var policy = this.translator.Police.Policy;
            foreach (var mi in this.mapping.GetMappedMembers(entity.Entity))
            {
                if (policy.IsIncluded(mi))
                    return true;
            }
            return false;
        }

        public override EntityExpression IncludeMembers(EntityExpression entity, Func<MemberInfo, bool> fnIsIncluded)
        {
            var assignments = this.GetAssignments(entity.Expression).ToDictionary(ma => ma.Member.Name);
            bool anyAdded = false;
            foreach (var mi in this.mapping.GetMappedMembers(entity.Entity))
            {
                EntityAssignment ea;
                bool okayToInclude = !assignments.TryGetValue(mi.Name, out ea) || IsNullRelationshipAssignment(entity.Entity, ea);
                if (okayToInclude && fnIsIncluded(mi))
                {
                    ea = new EntityAssignment(mi, this.GetMemberExpression(entity.Expression, entity.Entity, mi));
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
            if (this.mapping.IsRelationship(entity, assignment.Member))
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


        public override Expression GetMemberExpression(Expression root, MappingEntity entity, MemberInfo member)
        {
            if (this.mapping.IsAssociationRelationship(entity, member))
            {
                MappingEntity relatedEntity = this.mapping.GetRelatedEntity(entity, member);
                ProjectionExpression projection = this.GetQueryExpression(relatedEntity);

                // make where clause for joining back to 'root'
                var declaredTypeMembers = this.mapping.GetAssociationKeyMembers(entity, member).ToList();
                var associatedMembers = this.mapping.GetAssociationRelatedKeyMembers(entity, member).ToList();

                Expression where = null;
                for (int i = 0, n = associatedMembers.Count; i < n; i++)
                {
                    Expression equal =
                        this.GetMemberExpression(projection.Projector, relatedEntity, associatedMembers[i]).Equal(
                            this.GetMemberExpression(root, entity, declaredTypeMembers[i])
                        );
                    where = (where != null) ? where.And(equal) : equal;
                }

                TableAlias newAlias = new TableAlias();
                var pc = ColumnProjector.ProjectColumns(this.translator.Linguist.Language, projection.Projector, null, newAlias, projection.Select.Alias);

                LambdaExpression aggregator = Aggregator.GetAggregator(TypeHelper.GetMemberType(member), typeof(IEnumerable<>).MakeGenericType(pc.Projector.Type));
                var result = new ProjectionExpression(
                    new SelectExpression(newAlias, pc.Columns, projection.Select, where),
                    pc.Projector, aggregator
                    );

                return this.translator.Police.ApplyPolicy(result, member);
            }
            else
            {
                AliasedExpression aliasedRoot = root as AliasedExpression;
                if (aliasedRoot != null && this.mapping.IsColumn(entity, member))
                {
                    return new ColumnExpression(TypeHelper.GetMemberType(member), this.GetColumnType(entity, member), aliasedRoot.Alias, this.mapping.GetColumnName(entity, member));
                }
                return QueryBinder.BindMember(root, member);
            }
        }

        public override Expression GetInsertExpression(MappingEntity entity, Expression instance, LambdaExpression selector)
        {
            var tableAlias = new TableAlias();
            var table = new TableExpression(tableAlias, entity, this.mapping.GetTableName(entity));
            var assignments = this.GetColumnAssignments(table, instance, entity, (e, m) => !(mapping.IsGenerated(e, m) || mapping.IsReadOnly(e, m)));   // #MLCHANGE

            if (selector != null)
            {
                return new BlockCommand(
                    new InsertCommand(table, assignments),
                    this.GetInsertResult(entity, instance, selector, null)
                    );
            }

            return new InsertCommand(table, assignments);
        }

        private IEnumerable<ColumnAssignment> GetColumnAssignments(Expression table, Expression instance, MappingEntity entity, Func<MappingEntity, MemberInfo, bool> fnIncludeColumn)
        {
            foreach (var m in this.mapping.GetMappedMembers(entity))
            {
                if (this.mapping.IsColumn(entity, m) && fnIncludeColumn(entity, m))
                {
                    yield return new ColumnAssignment(
                        (ColumnExpression)this.GetMemberExpression(table, entity, m),
                        Expression.MakeMemberAccess(instance, m)
                        );
                }
            }
        }

        protected virtual Expression GetInsertResult(MappingEntity entity, Expression instance, LambdaExpression selector, Dictionary<MemberInfo, Expression> map)
        {
            var tableAlias = new TableAlias();
            var tex = new TableExpression(tableAlias, entity, this.mapping.GetTableName(entity));
            var aggregator = Aggregator.GetAggregator(selector.Body.Type, typeof(IEnumerable<>).MakeGenericType(selector.Body.Type));

            Expression where;
            DeclarationCommand genIdCommand = null;
            var generatedIds = this.mapping.GetMappedMembers(entity).Where(m => this.mapping.IsPrimaryKey(entity, m) && this.mapping.IsGenerated(entity, m)).ToList();
            if (generatedIds.Count > 0)
            {
                if (map == null || !generatedIds.Any(m => map.ContainsKey(m)))
                {
                    var localMap = new Dictionary<MemberInfo, Expression>();
                    genIdCommand = this.GetGeneratedIdCommand(entity, generatedIds.ToList(), localMap);
                    map = localMap;
                }

                // is this just a retrieval of one generated id member?
                var mex = selector.Body as MemberExpression;
                if (mex != null && this.mapping.IsPrimaryKey(entity, mex.Member) && this.mapping.IsGenerated(entity, mex.Member))
                {
                    if (genIdCommand != null)
                    {
                        // just use the select from the genIdCommand
                        return new ProjectionExpression(
                            genIdCommand.Source,
                            new ColumnExpression(mex.Type, genIdCommand.Variables[0].QueryType, genIdCommand.Source.Alias, genIdCommand.Source.Columns[0].Name),
                            aggregator
                            );
                    }
                    else
                    {
                        TableAlias alias = new TableAlias();
                        var colType = this.GetColumnType(entity, mex.Member);
                        return new ProjectionExpression(
                            new SelectExpression(alias, new[] { new ColumnDeclaration("", map[mex.Member], colType) }, null, null),
                            new ColumnExpression(TypeHelper.GetMemberType(mex.Member), colType, alias, ""),
                            aggregator
                            );
                    }
                }

                where = generatedIds.Select((m, i) =>
                    this.GetMemberExpression(tex, entity, m).Equal(map[m])
                    ).Aggregate((x, y) => x.And(y));
            }
            else
            {
                where = this.GetIdentityCheck(tex, entity, instance);
            }

            Expression typeProjector = this.GetEntityExpression(tex, entity);
            Expression selection = DbExpressionReplacer.Replace(selector.Body, selector.Parameters[0], typeProjector);
            TableAlias newAlias = new TableAlias();
            var pc = ColumnProjector.ProjectColumns(this.translator.Linguist.Language, selection, null, newAlias, tableAlias);
            var pe = new ProjectionExpression(
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

        protected virtual DeclarationCommand GetGeneratedIdCommand(MappingEntity entity, List<MemberInfo> members, Dictionary<MemberInfo, Expression> map)
        {
            var columns = new List<ColumnDeclaration>();
            var decls = new List<VariableDeclaration>();
            var alias = new TableAlias();
            foreach (var member in members)
            {
                Expression genId = this.translator.Linguist.Language.GetGeneratedIdExpression(member);
                var name = member.Name;
                var colType = this.GetColumnType(entity, member);
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

        protected virtual Expression GetIdentityCheck(Expression root, MappingEntity entity, Expression instance)
        {
            return this.mapping.GetMappedMembers(entity)
            .Where(m => this.mapping.IsPrimaryKey(entity, m))
            .Select(m => this.GetMemberExpression(root, entity, m).Equal(Expression.MakeMemberAccess(instance, m)))
            .Aggregate((x, y) => x.And(y));
        }

        protected virtual Expression GetEntityExistsTest(MappingEntity entity, Expression instance)
        {
            ProjectionExpression tq = this.GetQueryExpression(entity);
            Expression where = this.GetIdentityCheck(tq.Select, entity, instance);
            return new ExistsExpression(new SelectExpression(new TableAlias(), null, tq.Select, where));
        }

        protected virtual Expression GetEntityStateTest(MappingEntity entity, Expression instance, LambdaExpression updateCheck)
        {
            ProjectionExpression tq = this.GetQueryExpression(entity);
            Expression where = this.GetIdentityCheck(tq.Select, entity, instance);
            Expression check = DbExpressionReplacer.Replace(updateCheck.Body, updateCheck.Parameters[0], tq.Projector);
            where = where.And(check);
            return new ExistsExpression(new SelectExpression(new TableAlias(), null, tq.Select, where));
        }

        public override Expression GetUpdateExpression(MappingEntity entity, Expression instance, LambdaExpression updateCheck, LambdaExpression selector, Expression @else)
        {
            var tableAlias = new TableAlias();
            var table = new TableExpression(tableAlias, entity, this.mapping.GetTableName(entity));

            var where = this.GetIdentityCheck(table, entity, instance);
            if (updateCheck != null)
            {
                Expression typeProjector = this.GetEntityExpression(table, entity);
                Expression pred = DbExpressionReplacer.Replace(updateCheck.Body, updateCheck.Parameters[0], typeProjector);
                where = where.And(pred);
            }

            var assignments = this.GetColumnAssignments(table, instance, entity, (e, m) => this.mapping.IsUpdatable(e, m));

            Expression update = new UpdateCommand(table, where, assignments);

            if (selector != null)
            {
                return new BlockCommand(
                    update,
                    new IFCommand(
                        this.translator.Linguist.Language.GetRowsAffectedExpression(update).GreaterThan(Expression.Constant(0)),
                        this.GetUpdateResult(entity, instance, selector),
                        @else
                        )
                    );
            }
            else if (@else != null)
            {
                return new BlockCommand(
                    update,
                    new IFCommand(
                        this.translator.Linguist.Language.GetRowsAffectedExpression(update).LessThanOrEqual(Expression.Constant(0)),
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

        protected virtual Expression GetUpdateResult(MappingEntity entity, Expression instance, LambdaExpression selector)
        {
            var tq = this.GetQueryExpression(entity);
            Expression where = this.GetIdentityCheck(tq.Select, entity, instance);
            Expression selection = DbExpressionReplacer.Replace(selector.Body, selector.Parameters[0], tq.Projector);
            TableAlias newAlias = new TableAlias();
            var pc = ColumnProjector.ProjectColumns(this.translator.Linguist.Language, selection, null, newAlias, tq.Select.Alias);
            return new ProjectionExpression(
                new SelectExpression(newAlias, pc.Columns, tq.Select, where),
                pc.Projector,
                Aggregator.GetAggregator(selector.Body.Type, typeof(IEnumerable<>).MakeGenericType(selector.Body.Type))
                );
        }

        public override Expression GetInsertOrUpdateExpression(MappingEntity entity, Expression instance, LambdaExpression updateCheck, LambdaExpression resultSelector)
        {
            if (updateCheck != null)
            {
                Expression insert = this.GetInsertExpression(entity, instance, resultSelector);
                Expression update = this.GetUpdateExpression(entity, instance, updateCheck, resultSelector, null);
                var check = this.GetEntityExistsTest(entity, instance);
                return new IFCommand(check, update, insert);
            }
            else
            {
                Expression insert = this.GetInsertExpression(entity, instance, resultSelector);
                Expression update = this.GetUpdateExpression(entity, instance, updateCheck, resultSelector, insert);
                return update;
            }
        }

        public override Expression GetDeleteExpression(MappingEntity entity, Expression instance, LambdaExpression deleteCheck)
        {
            TableExpression table = new TableExpression(new TableAlias(), entity, this.mapping.GetTableName(entity));
            Expression where = null;

            if (instance != null)
            {
                where = this.GetIdentityCheck(table, entity, instance);
            }

            if (deleteCheck != null)
            {
                Expression row = this.GetEntityExpression(table, entity);
                Expression pred = DbExpressionReplacer.Replace(deleteCheck.Body, deleteCheck.Parameters[0], row);
                where = (where != null) ? where.And(pred) : pred;
            }

            return new DeleteCommand(table, where);
        }
    }
}