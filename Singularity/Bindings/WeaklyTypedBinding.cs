﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Singularity.Exceptions;
using Singularity.Graph;

namespace Singularity.Bindings
{
    /// <summary>
    /// Represents a weakly typed registration
    /// </summary>
    public sealed class WeaklyTypedBinding
    {
        /// <summary>
        /// The metadata of this binding.
        /// </summary>
        public BindingMetadata BindingMetadata { get; }

        /// <summary>
        /// A expression that is used to create the instance
        /// </summary>
        public Expression? Expression => WeaklyTypedConfiguredBinding?.Expression;

        /// <summary>
        /// When should new instance be created. See <see cref="Lifetime"/> for more detailed information.
        /// </summary>
        public Lifetime Lifetime => WeaklyTypedConfiguredBinding?.Lifetime ?? Lifetime.Transient;

        /// <summary>
        /// A action that is executed when the <see cref="Scoped"/> is disposed. This usually happens when the <see cref="Container"/> is disposed.
        /// </summary>
        public Action<object>? Finalizer => WeaklyTypedConfiguredBinding?.Finalizer;

        public DisposeBehavior NeedsDispose => WeaklyTypedConfiguredBinding?.DisposeBehavior ?? DisposeBehavior.Default;

        internal WeaklyTypedConfiguredBinding? WeaklyTypedConfiguredBinding { get; set; }

        internal WeaklyTypedBinding(Type[] dependencyTypes, string callerFilePath, int callerLineNumber, IModule? module)
        {
            foreach (Type dependencyType in dependencyTypes)
            {
                if (dependencyType.IsGenericType && dependencyType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    Type enumerableType = dependencyType.GenericTypeArguments[0];
                    throw new EnumerableRegistrationException($"don't register {enumerableType} as IEnumerable directly. Instead register them as you would normally.");
                }
            }

            BindingMetadata = new BindingMetadata(dependencyTypes, callerFilePath, callerLineNumber, module);
        }

        /// <summary>
        /// Sets the expression that is used to create the instance(s)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public WeaklyTypedConfiguredBinding Inject(Expression expression)
        {
            BindingMetadata.DependencyTypes.CheckInstanceTypeIsAssignable(expression.GetReturnType());
            WeaklyTypedConfiguredBinding = new WeaklyTypedConfiguredBinding(this, expression);
            return WeaklyTypedConfiguredBinding;
        }
    }
}
