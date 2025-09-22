using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DISourceGenerator;

public class RegistryProducer
{
    const string RegistryTemplate =
        """
        #nullable enable
        
        using System;
        using System.Diagnostics.CodeAnalysis;
        using System.Collections.Generic;

        namespace Mal.DependencyInjection;
        
        /// <summary>
        /// An auto-generated dependency container that registers services marked with the <c>DependencyAttribute</c>.
        /// </summary>
        partial class DependencyContainer: IServiceProvider, IDependencyContainer
        {{
            readonly Dictionary<Type, Func<DependencyContainer, object>> _registrations = new Dictionary<Type, Func<DependencyContainer, object>>
            {{
                {0}
            }};
            
            readonly Dictionary<Type, object> _singletons = new();
            
            /// <summary>
            /// Initializes a new instance of the <see cref="DependencyContainer"/> class.
            /// </summary>
            public DependencyContainer()
            {{
                _singletons[typeof(DependencyContainer)] = this;
                _singletons[typeof(IDependencyContainer)] = this;
            }}
            
            object? IServiceProvider.GetService(Type serviceType) => Resolve(serviceType);
            
            /// <inheritdoc/>
            public T Resolve<T>() where T: class => (T)Resolve(typeof(T))!;
            
            /// <inheritdoc/>
            public bool TryResolve<T>([MaybeNullWhen(false)] out T instance) where T: class
            {{
                if (!TryResolve(typeof(T), out var obj))
                {{
                    instance = null;
                    return false;
                }}
                instance = (T)obj!;
                return true;
            }}
            
            /// <inheritdoc/>
            public object Resolve(Type serviceType)
            {{
                if (!TryResolve(serviceType, out var instance))
                    throw new InvalidOperationException($"Service of type {{serviceType}} is not registered.");
                return instance;
            }}
            
            /// <inheritdoc/>
            public bool TryResolve(Type serviceType, [MaybeNullWhen(false)] out object instance)
            {{
                if (_singletons.TryGetValue(serviceType, out instance))
                    return true;
                
                if (!_registrations.TryGetValue(serviceType, out var factory))
                {{
                    instance = null;
                    return false;
                }}
                instance = factory(this);
                _singletons[serviceType] = instance;
                return true;
            }}

            /// <summary>
            /// Registers a service with the specified factory method.
            /// </summary>
            /// <typeparam name="TService">The type of the service to register.</typeparam>
            /// <param name="factory">The factory method to create instances of the service.</param>
            /// <exception cref="InvalidOperationException">Thrown if the service type is already registered.</exception>
            public void Register<TService>(Func<DependencyContainer, TService> factory) where TService: class
            {{
                if (!_registrations.TryAdd(typeof(TService), dr => factory(dr)))
                    throw new InvalidOperationException($"Service of type {{typeof(TService)}} is already registered.");
            }}
            
            /// <summary>
            /// Registers a service with the specified factory method.
            /// </summary>
            /// <param name="serviceType">The type of the service to register.</param>
            /// <param name="factory">The factory method to create instances of the service.</param>
            /// <exception cref="InvalidOperationException">Thrown if the service type is already registered.</exception>
            public void Register(Type serviceType, Func<DependencyContainer, object> factory)
            {{
                if (!_registrations.TryAdd(serviceType, factory))
                    throw new InvalidOperationException($"Service of type {{serviceType}} is already registered.");
            }}
        }}
        """;

    const string ItemTemplate = "[typeof({0})] = dr => new {1}({2})";
    const string ItemSeparator= ",\n        ";

    const string ParameterTemplate = "dr.Resolve<{0}>()";
    const string LazyParameterTemplate = "new Lazy<{0}>(() => dr.Resolve<{0}>())";
    readonly ImmutableArray<DependencyRegistryGenerator.Item> _items;

    public RegistryProducer(ImmutableArray<DependencyRegistryGenerator.Item> items)
    {
        _items = items;
    }

    public string Produce()
    {
        var items = new List<string>();
        foreach (var item in _items)
        {
            var ctor = item.Implementation.Constructors.Length > 0
                ? item.Implementation.Constructors.OrderByDescending(c => c.Parameters.Length).First()
                : null;
            var parameters = new List<string>();
            if (ctor is not null)
            {
                foreach (var p in ctor.Parameters)
                {
                    // If this is a Lazy, we need to resolve the inner type, and make the argument a new Lazy<>
                    if (p.Type is INamedTypeSymbol { IsGenericType: true, Name: "Lazy", TypeArguments.Length: 1 } lazyType)
                    {
                        parameters.Add(string.Format(LazyParameterTemplate, lazyType.TypeArguments[0].ToDisplayString()));
                        continue;
                    }
                    parameters.Add(string.Format(ParameterTemplate, p.Type.ToDisplayString()));
                }
            }
            items.Add(string.Format(ItemTemplate, item.Service.ToDisplayString(), item.Implementation.ToDisplayString(), string.Join(", ", parameters)));
        }
        return string.Format(RegistryTemplate, string.Join(ItemSeparator, items));
    }
}