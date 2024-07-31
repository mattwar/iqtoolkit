// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using IQToolkit.Expressions;

namespace IQToolkit.Entities
{
    using Expressions;
    using Expressions.Sql;
    using Mapping;
    using Translation;
    using Utils;

    /// <summary>
    /// Transforms a translated query expression into an executable expression that 
    /// will query the database and convert the tabular results into objects.
    /// </summary>
    public class QueryPlanBuilder
    {
        public static QueryPlan Build(
            QueryLanguageRewriter linguist,
            FormattingOptions formattingOptions,
            QueryPolicy policy,
            Expression query,
            Expression provider)
        {
            // remove possible lambda and add back later
            var lambda = query as LambdaExpression;
            if (lambda != null)
                query = lambda.Body;

            // add executor parameter
            var executorParam = Expression.Parameter(typeof(QueryExecutor), "executor");
            var initializer = Expression.Property(Expression.Convert(provider, typeof(IHaveExecutor)), "Executor");

            var diagnostics = new List<Diagnostic>();
            var builder = new Builder(linguist, formattingOptions, policy, executorParam, diagnostics);

            // add parameters & values for top level lambda
            builder.AddExecutorParameter(executorParam, initializer);
            
            var executor = builder.Build(query);

            // add back the lambda
            if (lambda != null)
                executor = lambda.Update(lambda.Type, executor, lambda.Parameters);

            return new QueryPlan(executor, diagnostics);
        }

        private class Builder : SqlExpressionVisitor
        {
            private readonly QueryPolicy _policy;
            private readonly QueryLanguageRewriter _linguist;
            private readonly FormattingOptions _formattingOptions;
            private readonly ParameterExpression _executor;
            private readonly List<Diagnostic> _diagnostics;
            private Scope? _scope;
            private bool _isTop = true;
            private MemberInfo? _receivingMember;
            private int _nReaders = 0;
            private readonly List<ParameterExpression> _variables = new List<ParameterExpression>();
            private readonly List<Expression> _initializers = new List<Expression>();
            private Dictionary<string, Expression> _variableMap = new Dictionary<string, Expression>();

            public Builder(
                QueryLanguageRewriter linguist,
                FormattingOptions formattingOptions,
                QueryPolicy policy,
                ParameterExpression executor,
                List<Diagnostic> diagnostics
                )
            {
                _linguist = linguist;
                _formattingOptions = formattingOptions;
                _policy = policy;
                _executor = executor;
                _variables = new List<ParameterExpression>();
                _initializers = new List<Expression>();
                _diagnostics = diagnostics;
            }

            public void AddExecutorParameter(
                ParameterExpression executorParameter,
                Expression initializer)
            {
                _variables.Add(executorParameter);
                _initializers.Add(initializer);
            }

            public Expression Build(Expression expression)
            {
                expression = this.Visit(expression)!;
                expression = this.AddVariables(expression);
                return expression;
            }

            private Expression AddVariables(Expression expression)
            {
                // add variable assignments up front
                if (_variables.Count > 0)
                {
                    var exprs = new List<Expression>();

                    for (int i = 0, n = _variables.Count; i < n; i++)
                    {
                        exprs.Add(Expression.Assign(_variables[i], _initializers[i]));
                    }

                    exprs.Add(expression);

                    return Expression.Block(_variables, exprs);
                }

                return expression;
            }

            public static IEnumerable<R> Batch<T, R>(IEnumerable<T> items, Func<T, R> selector, bool stream)
            {
                var result = items.Select(selector);
                if (!stream)
                {
                    return result.ToList();
                }
                else
                {
                    return new EnumerateOnce<R>(result);
                }
            }

            private Expression BuildInner(Expression expression)
            {
                var eb = new Builder(_linguist, _formattingOptions, _policy, _executor, _diagnostics);
                eb._scope = _scope;
                eb._receivingMember = _receivingMember;
                eb._nReaders = _nReaders;
                eb.nLookup = this.nLookup;
                eb._variableMap = _variableMap;
                return eb.Build(expression);
            }

            protected override MemberBinding VisitMemberBinding(MemberBinding binding)
            {
                var save = _receivingMember;
                _receivingMember = binding.Member;
                var result = base.VisitMemberBinding(binding);
                _receivingMember = save;
                return result;
            }

            int nLookup = 0;

            private Expression MakeJoinKey(IReadOnlyList<Expression> key)
            {
                if (key.Count == 1)
                {
                    return key[0];
                }
                else
                {
                    var constructor = TypeHelper.FindConstructor(typeof(CompoundKey), new[] { typeof(object[]) });

                    return Expression.New(
                        constructor,
                        Expression.NewArrayInit(typeof(object), key.Select(k => (Expression)Expression.Convert(k, typeof(object))))
                        );
                }
            }

            protected internal override Expression VisitClientJoin(ClientJoinExpression join)
            {
                // convert client join into a up-front lookup table builder & replace client-join in tree with lookup accessor

                // 1) lookup = query.Select(e => new KVP(key: inner, value: e)).ToLookup(kvp => kvp.Key, kvp => kvp.Value)
                Expression innerKey = MakeJoinKey(join.InnerKey);
                Expression outerKey = MakeJoinKey(join.OuterKey);

                var kvpConstructor = TypeHelper.FindConstructor(typeof(KeyValuePair<,>).MakeGenericType(innerKey.Type, join.Projection.Projector.Type), new Type[] { innerKey.Type, join.Projection.Projector.Type });
                Expression constructKVPair = Expression.New(kvpConstructor, innerKey, join.Projection.Projector);
                ClientProjectionExpression newProjection = new ClientProjectionExpression(join.Projection.Select, constructKVPair);

                int iLookup = ++nLookup;
                var execution = this.GetProjectionExecutor(newProjection, false);

                ParameterExpression kvp = Expression.Parameter(constructKVPair.Type, "kvp");

                // filter out nulls
                if (join.Projection.Projector is OuterJoinedExpression)
                {
                    LambdaExpression pred = Expression.Lambda(
                        Expression.PropertyOrField(kvp, "Value").NotEqual(TypeHelper.GetNullConstant(join.Projection.Projector.Type)),
                        kvp
                        );
                    execution = Expression.Call(typeof(Enumerable), "Where", new Type[] { kvp.Type }, execution, pred);
                }

                // make lookup
                var keySelector = Expression.Lambda(Expression.PropertyOrField(kvp, "Key"), kvp);
                var elementSelector = Expression.Lambda(Expression.PropertyOrField(kvp, "Value"), kvp);
                Expression toLookup = Expression.Call(typeof(Enumerable), "ToLookup", new Type[] { kvp.Type, outerKey.Type, join.Projection.Projector.Type }, execution, keySelector, elementSelector);

                // 2) agg(lookup[outer])
                var lookup = Expression.Parameter(toLookup.Type, "lookup" + iLookup);
                var property = lookup.Type.GetTypeInfo().GetDeclaredProperty("Item");
                Expression access = Expression.Call(lookup, property.GetMethod, this.Visit(outerKey));

                if (join.Projection.Aggregator != null)
                {
                    // apply aggregator
                    access = join.Projection.Aggregator.Body.Replace(
                        join.Projection.Aggregator.Parameters[0],
                        access
                        );
                }

                _variables.Add(lookup);
                _initializers.Add(toLookup);

                return access;
            }

            protected internal override Expression VisitClientProjection(ClientProjectionExpression projection)
            {
                if (_isTop)
                {
                    _isTop = false;
                    return this.GetProjectionExecutor(projection, _scope != null);
                }
                else
                {
                    return this.BuildInner(projection);
                }
            }

            protected virtual Expression Parameterize(Expression expression)
            {
                if (_variableMap.Count > 0)
                {
                    expression = expression.Replace(exp =>
                        exp is VariableExpression vex
                            && _variableMap.TryGetValue(vex.Name, out var sub) 
                            ? sub 
                            : exp
                        );
                }

                return _linguist.Parameterize(expression);
            }

            private Expression GetProjectionExecutor(ClientProjectionExpression projection, bool okayToDefer)
            {
                // parameterize query
                projection = (ClientProjectionExpression)this.Parameterize(projection);

                if (_scope != null)
                {
                    // also convert references to outer alias to client parameters! these become SQL parameters too
                    projection = (ClientProjectionExpression)OuterParameterizer.Parameterize(_scope.Alias, projection);
                }

                GetQueryCommandAndValues(projection.Select, out var command, out var values);

                return this.GetProjectionExecutor(projection, okayToDefer, command, values);
            }

            private void GetQueryCommandAndValues(
                Expression expression, 
                out Expression command,
                out IReadOnlyList<Expression> values)
            {

                var formatted = _linguist.Language.Formatter.Format(expression, _formattingOptions);
                if (formatted.Diagnostics.Count > 0)
                    _diagnostics.AddRange(formatted.Diagnostics);

                var commandText = formatted.Text;

                var clientParameters = formatted.ParameterReferences.OfType<ClientParameterExpression>().ToList();

                // ODBC needs each reference to be a separate parameter.
                if (!_formattingOptions.IsOdbc)
                    clientParameters = clientParameters.DistinctBy(cp => cp.Name).ToList();
                
                var parameters = clientParameters
                    .Select(cp => new QueryParameter(cp.Name, cp.Type, cp.QueryType))
                    .ToList();

                command =
                    Expression.Constant(
                        new QueryCommand(commandText, parameters));

                values = clientParameters
                    .Select(v => Expression.Convert(this.Visit(v.Value), typeof(object)))
                    .ToList();
            }

            private Expression GetProjectionExecutor(
                ClientProjectionExpression projection, 
                bool okayToDefer, 
                Expression command, 
                IReadOnlyList<Expression> values)
            {
                okayToDefer &= (_receivingMember != null && _policy.IsDeferLoaded(_receivingMember));

                var saveScope = _scope;
                var reader = Expression.Parameter(typeof(FieldReader), "reader" + _nReaders++);
                _scope = new Scope(_scope, reader, projection.Select.Alias, projection.Select.Columns);
                var projector = Expression.Lambda(this.Visit(projection.Projector), reader);
                _scope = saveScope;

                var entity = projection.Projector.FindFirstDownOrDefault<EntityExpression>()?.Entity;

                string methExecute = okayToDefer
                    ? "ExecuteDeferred"
                    : "Execute";

                // call low-level execute directly on supplied DbQueryProvider
                Expression result = Expression.Call(_executor, methExecute, new Type[] { projector.Body.Type },
                    command,
                    projector,
                    Expression.Constant(entity, typeof(MappingEntity)),
                    Expression.NewArrayInit(typeof(object), values)
                    );

                if (projection.Aggregator != null)
                {
                    // apply aggregator
                    result = projection.Aggregator.Body.Replace(projection.Aggregator.Parameters[0], result);
                }

                return result;
            }

            protected internal override Expression VisitBatch(BatchExpression batch)
            {
                if (_linguist.Language.AllowsMultipleCommands 
                    || !IsMultipleCommands(batch.Operation.Body as CommandExpression))
                {
                    return this.BuildExecuteBatch(batch);
                }
                else
                {
                    var source = this.Visit(batch.Input)!;
                    var op = this.Visit(batch.Operation.Body);
                    var fn = Expression.Lambda(op, batch.Operation.Parameters[1]);
                    return Expression.Call(this.GetType(), "Batch", new Type[] { TypeHelper.GetSequenceElementType(source.Type), batch.Operation.Body.Type }, source, fn, batch.Stream);
                }
            }

            protected virtual Expression BuildExecuteBatch(BatchExpression batch)
            {
                // parameterize query
                var operation = this.Parameterize(batch.Operation.Body);

                GetQueryCommandAndValues(operation, out var command, out var values);
                
                Expression paramSets = Expression.Call(
                    typeof(Enumerable), 
                    "Select", 
                    new Type[] { batch.Operation.Parameters[1].Type, typeof(object[]) },
                    new Expression[]
                    {
                        batch.Input,
                        Expression.Lambda(
                            Expression.NewArrayInit(typeof(object), values), new[] { batch.Operation.Parameters[1] })
                    });

                Expression? plan = null;

                var projection = operation.FindFirstDownOrDefault<ClientProjectionExpression>();
                if (projection != null)
                {
                    var saveScope = _scope;
                    var reader = Expression.Parameter(typeof(FieldReader), "reader" + _nReaders++);
                    _scope = new Scope(_scope, reader, projection.Select.Alias, projection.Select.Columns);
                    LambdaExpression projector = Expression.Lambda(this.Visit(projection.Projector), reader);
                    _scope = saveScope;

                    var entity = projection.Projector.FindFirstDownOrDefault<EntityExpression>()?.Entity;

                    plan = Expression.Call(_executor, "ExecuteBatch", new Type[] { projector.Body.Type },
                        command,
                        paramSets,
                        projector,
                        Expression.Constant(entity, typeof(MappingEntity)),
                        batch.BatchSize,
                        batch.Stream
                        );
                }
                else
                {
                    plan = Expression.Call(_executor, "ExecuteBatch", null,
                        command,
                        paramSets,
                        batch.BatchSize,
                        batch.Stream
                        );
                }

                return plan;
            }

            protected virtual bool IsMultipleCommands(CommandExpression? command)
            {
                if (command == null)
                    return false;

                switch (command)
                {
                    case InsertCommand _:
                    case DeleteCommand _:
                    case UpdateCommand _:
                        return false;
                    default:
                        return true;
                }
            }

            protected internal override Expression VisitInsertCommand(InsertCommand insert)
            {
                return this.BuildExecuteCommand(insert);
            }

            protected internal override Expression VisitUpdateCommand(UpdateCommand update)
            {
                return this.BuildExecuteCommand(update);
            }

            protected internal override Expression VisitDeleteCommand(DeleteCommand delete)
            {
                return this.BuildExecuteCommand(delete);
            }

            protected internal override Expression VisitBlockCommand(BlockCommand block)
            {
                return Expression.Block(
                    block.Commands.Rewrite(this)
                    );
            }

            protected internal override Expression VisitIfCommand(IfCommand ifx)
            {
                var test =
                    Expression.Condition(
                        ifx.Test,
                        ifx.IfTrue,
                        ifx.IfFalse != null
                            ? ifx.IfFalse
                            : ifx.IfTrue.Type == typeof(int)
                                ? (Expression)Expression.Property(_executor, "RowsAffected")
                                : (Expression)Expression.Constant(TypeHelper.GetDefault(ifx.IfTrue.Type), ifx.IfTrue.Type)
                                );

                return this.Visit(test)!;
            }

            protected internal override Expression VisitScalarFunctionCall(ScalarFunctionCallExpression func)
            {
                if (_linguist.Language.IsRowsAffectedExpressions(func))
                {
                    return Expression.Property(_executor, "RowsAffected");
                }

                return base.VisitScalarFunctionCall(func);
            }

            protected internal override Expression VisitExistsSubquery(ExistsSubqueryExpression exists)
            {
                // how did we get here? Translate exists into count query
                var colType = _linguist.Language.TypeSystem.GetQueryType(typeof(int));
                var newSelect = exists.Select.WithColumns(
                    new[] { new ColumnDeclaration("value", new AggregateExpression(typeof(int), "Count", null, false), colType) }
                    );

                var projection =
                    new ClientProjectionExpression(
                        newSelect,
                        new ColumnExpression(typeof(int), colType, newSelect.Alias, "value"),
                        Aggregator.GetAggregator(typeof(int), typeof(IEnumerable<int>))
                        );

                var expression = projection.GreaterThan(Expression.Constant(0));

                return this.Visit(expression)!;
            }

            protected internal override Expression VisitDeclarationCommand(DeclarationCommand decl)
            {
                if (decl.Source != null)
                {
                    // make query that returns all these declared values as an object[]
                    var projection = new ClientProjectionExpression(
                        decl.Source,
                        Expression.NewArrayInit(
                            typeof(object),
                            decl.Variables.Select(v => v.Expression.Type.IsValueType
                                ? Expression.Convert(v.Expression, typeof(object))
                                : v.Expression).ToArray()
                            ),
                        Aggregator.GetAggregator(typeof(object[]), typeof(IEnumerable<object[]>))
                        );

                    // create execution variable to hold the array of declared variables
                    var vars = Expression.Parameter(typeof(object[]), "vars");
                    _variables.Add(vars);
                    _initializers.Add(Expression.Constant(null, typeof(object[])));

                    // create subsitution for each variable (so it will find the variable value in the new vars array)
                    for (int i = 0, n = decl.Variables.Count; i < n; i++)
                    {
                        var v = decl.Variables[i];
                        var cp = new ClientParameterExpression(
                            v.Name, v.QueryType,
                            Expression.Convert(Expression.ArrayIndex(vars, Expression.Constant(i)), v.Expression.Type)
                            );
                        _variableMap.Add(v.Name, cp);
                    }

                    // make sure the execution of the select stuffs the results into the new vars array
                    return Expression.Assign(vars, this.Visit(projection)!);
                }

                // probably bad if we get here since we must not allow mulitple commands
                throw new InvalidOperationException("Declaration query not allowed for this langauge");
            }

            protected virtual Expression BuildExecuteCommand(CommandExpression command)
            {
                // parameterize query
                var expression = this.Parameterize(command);

                GetQueryCommandAndValues(expression, out var queryCommand, out var values);

                var projection = expression.FindFirstDownOrDefault<ClientProjectionExpression>();
                if (projection != null)
                {
                    return this.GetProjectionExecutor(projection, false, queryCommand, values);
                }

                Expression plan = Expression.Call(_executor, "ExecuteCommand", null,
                    queryCommand,
                    Expression.NewArrayInit(typeof(object), values)
                    );

                return plan;
            }

            protected internal override Expression VisitEntity(EntityExpression entity)
            {
                return this.Visit(entity.Expression)!;
            }

            protected internal override Expression VisitOuterJoined(OuterJoinedExpression outer)
            {
                var expr = this.Visit(outer.Expression)!;
                ColumnExpression column = (ColumnExpression)outer.Test;
                int iOrdinal;
                if (_scope != null && _scope.TryGetValue(column, out var reader, out iOrdinal))
                {
                    return Expression.Condition(
                        Expression.Call(reader, "IsDbNull", null, Expression.Constant(iOrdinal)),
                        Expression.Constant(TypeHelper.GetDefault(outer.Type), outer.Type),
                        expr
                        );
                }
                return expr;
            }

            protected internal override Expression VisitColumn(ColumnExpression column)
            {
                int iOrdinal;
                if (_scope != null && _scope.TryGetValue(column, out var fieldReader, out iOrdinal))
                {
                    MethodInfo method = FieldReader.GetReaderMethod(column.Type);
                    return Expression.Call(fieldReader, method, Expression.Constant(iOrdinal));
                }
                else
                {
                    System.Diagnostics.Debug.Fail(string.Format("column not in scope: {0}", column));
                }
                return column;
            }

            private sealed class Scope
            {
                private readonly Scope? _outer;
                private readonly ParameterExpression? _fieldReader;
                private readonly ImmutableDictionary<string, int> _nameMap;

                internal TableAlias Alias { get; private set; }

                internal Scope(Scope? outer, ParameterExpression fieldReader, TableAlias alias, IEnumerable<ColumnDeclaration> columns)
                {
                    _outer = outer;
                    _fieldReader = fieldReader;
                    this.Alias = alias;
                    _nameMap = columns
                        .Select((c, i) => new { c, i })
                        .ToImmutableDictionary(x => x.c.Name, x => x.i);
                }

                internal bool TryGetValue(ColumnExpression column, out ParameterExpression? fieldReader, out int ordinal)
                {
                    for (Scope? s = this; s != null; s = s._outer)
                    {
                        if (column.Alias == s.Alias 
                            && _nameMap.TryGetValue(column.Name, out ordinal))
                        {
                            fieldReader = _fieldReader;
                            return true;
                        }
                    }

                    fieldReader = null;
                    ordinal = 0;
                    return false;
                }
            }

            /// <summary>
            /// columns referencing the outer alias are turned into special client parameters
            /// </summary>
            private sealed class OuterParameterizer : SqlExpressionVisitor
            {
                private readonly TableAlias _outerAlias;
                private readonly Dictionary<ColumnExpression, ClientParameterExpression> _map =
                    new Dictionary<ColumnExpression, ClientParameterExpression>();
                private int _iParam;

                private OuterParameterizer(TableAlias outerAlias)
                {
                    _outerAlias = outerAlias;
                }

                internal static Expression Parameterize(TableAlias outerAlias, Expression expr)
                {
                    var op = new OuterParameterizer(outerAlias);
                    return op.Visit(expr)!;
                }

                protected internal override Expression VisitClientProjection(ClientProjectionExpression proj)
                {
                    SelectExpression select = (SelectExpression)this.Visit(proj.Select)!;
                    return proj.Update(select, proj.Projector, proj.Aggregator);
                }

                protected internal override Expression VisitColumn(ColumnExpression column)
                {
                    if (column.Alias == _outerAlias)
                    {
                        if (!_map.TryGetValue(column, out var cp))
                        {
                            cp = new ClientParameterExpression("n" + (_iParam++), column.QueryType, column);
                            _map.Add(column, cp);
                        }

                        return cp;
                    }

                    return column;
                }
            }
        }
    }
}