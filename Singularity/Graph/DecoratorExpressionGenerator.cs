﻿using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Singularity.Bindings;
using Singularity.Expressions;

namespace Singularity.Graph
{
    internal sealed class DecoratorExpressionGenerator : IDependencyExpressionGenerator
    {
        public void Generate(UnresolvedDependency binding, ParameterExpression instanceParameter, List<ParameterExpression> parameters, List<Expression> body)
        {
            if (binding.Decorators.Count > 0)
            {
                Expression previousDecorator = instanceParameter;
                foreach (DecoratorBinding decorator in binding.Decorators)
                {
                    var visitor = new ReplaceExpressionVisitor(decorator.Expression.GetParameterExpressions().First(x => x.Type == instanceParameter.Type), previousDecorator);
                    Expression decoratorExpression = visitor.Visit(decorator.Expression);
                    parameters.AddRange(decoratorExpression.GetParameterExpressions().Where(parameterExpression => parameterExpression.Type != instanceParameter.Type));
                    previousDecorator = decoratorExpression;
                }
                body.Add(previousDecorator);
            }
        }
    }
}