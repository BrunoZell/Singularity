﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using Singularity.Bindings;
using Singularity.Exceptions;
using Singularity.Extensions;

namespace Singularity.Graph
{
    public class DependencyGraph
    {
        public ReadOnlyDictionary<Type, DependencyNode> Dependencies { get; }
        public IBindingConfig BindingConfig { get; }

        public DependencyGraph(IBindingConfig bindingConfig, IEnumerable<IDependencyExpressionGenerator> dependencyExpressionGenerators, DependencyGraph parentDependencyGraph = null)
        {
            if (parentDependencyGraph != null)
                bindingConfig = new BindingConfig(bindingConfig, parentDependencyGraph);
            BindingConfig = bindingConfig;
            BindingConfig.ValidateBindings();

            var dependencies = GenerateDependencyNodes(bindingConfig, dependencyExpressionGenerators);

            Dependencies = new ReadOnlyDictionary<Type, DependencyNode>(dependencies);

            var graph = new Graph<DependencyNode>(Dependencies.Values);
            var updateOrder = graph.GetUpdateOrder(GetDependencies);

            foreach (var dependencyNodes in updateOrder)
            {
                foreach (var dependencyNode in dependencyNodes)
                {
                    dependencyNode.ResolvedDependency = ResolveDependency(dependencyNode.UnresolvedDependency);
                }
            }
        }

        private Dictionary<Type, DependencyNode> GenerateDependencyNodes(IBindingConfig bindingConfig, IEnumerable<IDependencyExpressionGenerator> dependencyExpressionGenerators)
        {
            var dependencies = new Dictionary<Type, DependencyNode>();
            foreach (var binding in bindingConfig.Bindings.Values)
            {
                if (binding.ConfiguredBinding == null && binding.Decorators.Count > 0) continue;
                var expression = GenerateDependencyExpression(binding, dependencyExpressionGenerators);
                var node = new DependencyNode(new UnresolvedDependency(expression, binding.ConfiguredBinding.Lifetime, binding.ConfiguredBinding.OnDeath));
                dependencies.Add(binding.DependencyType, node);
            }

            return dependencies;
        }

        private Expression GenerateDependencyExpression(IBinding binding, IEnumerable<IDependencyExpressionGenerator> dependencyExpressionGenerators)
        {
            var expression = binding.ConfiguredBinding.Expression;
            var dependencyType = binding.DependencyType;
            var body = new List<Expression>();
            var parameters = new List<ParameterExpression>();
            parameters.AddRange(expression.GetParameterExpressions());

            var instanceParameter = Expression.Variable(binding.DependencyType, $"{expression.Type} instance");
            body.Add(Expression.Assign(instanceParameter, Expression.Convert(expression, dependencyType)));
            foreach (var dependencyExpressionGenerator in dependencyExpressionGenerators)
            {
                dependencyExpressionGenerator.Generate(binding, instanceParameter, parameters, body);
            }

            if (body.Last().Type == typeof(void)) body.Add(instanceParameter);
            return body.Count > 1 ? Expression.Lambda(Expression.Block(parameters.Concat(new[] { instanceParameter }), body), parameters) : expression;
        }

        private IEnumerable<DependencyNode> GetDependencies(DependencyNode dependencyNode)
        {
            var parameters = dependencyNode.UnresolvedDependency.Expression.GetParameterExpressions();
            var resolvedDependencies = new List<DependencyNode>();
            if (parameters.TryExecute(parameterExpression => { resolvedDependencies.Add(GetDependency(parameterExpression.Type)); }, out var exceptions))
            {
                throw new SingularityAggregateException($"Could not find all dependencies for {dependencyNode.UnresolvedDependency.Expression.Type}" , exceptions);
            }

            return resolvedDependencies;
        }

        private DependencyNode GetDependency(Type type)
        {
            if (Dependencies.TryGetValue(type, out var parent))
            {
                return parent;
            }
            throw new DependencyNotFoundException(type);
        }

        private ResolvedDependency ResolveDependency(UnresolvedDependency unresolvedDependency)
        {
            var expression = ResolveMethodCallExpression(unresolvedDependency.Expression);

            switch (unresolvedDependency.Lifetime)
            {
                case Lifetime.PerCall:
                    break;
                case Lifetime.PerContainer:
                    var action = Expression.Lambda(expression).Compile();
                    var value = action.DynamicInvoke();
                    expression = Expression.Constant(value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new ResolvedDependency(expression);
        }

        private Expression ResolveMethodCallExpression(Expression expression)
        {
            switch (expression)
            {
                case LambdaExpression lambdaExpression:
                    {
                        var body = ResolveMethodCallParameters(lambdaExpression.Parameters);
	                    var innerVariables = lambdaExpression.Body.GetParameterExpressions();
	                    var expressions = lambdaExpression.Body.FlattenExpression();
                        expression = Expression.Block(innerVariables, body.Concat(expressions));
					}
                    break;
                case NewExpression newExpression:
                    {
                        if (newExpression.Arguments.Count == 0) break;
                        var body = ResolveMethodCallParameters(newExpression.Arguments);
                        body.Add(newExpression);
                        expression = Expression.Block(newExpression.Arguments.Cast<ParameterExpression>(), body);
                    }
                    break;
                case BlockExpression _:
                case ConstantExpression _:
                    break;
                default:
                    throw new NotSupportedException($"The expression of type {expression.GetType()} is not supported");
            }
            return expression;
        }

        private List<Expression> ResolveMethodCallParameters(IEnumerable<Expression> parameterExpressions)
        {
            var body = new List<Expression>();
            foreach (var unresolvedParameter in parameterExpressions)
            {
                var dependency = GetDependency(unresolvedParameter.Type);
                body.Add(Expression.Assign(unresolvedParameter, dependency.ResolvedDependency.Expression));
            }
            return body;
        }
    }
}