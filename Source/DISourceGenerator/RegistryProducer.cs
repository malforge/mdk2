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
        using System.Linq;

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
            
            readonly HashSet<Type> _instances = new HashSet<Type>
            {{
                {1}
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
            
            Stack<Type> _resolvingStack = new();
            HashSet<Type> _resolvingSet = new();
            
            /// <inheritdoc/>
            public bool TryResolve(Type serviceType, [MaybeNullWhen(false)] out object instance)
            {{
                // Check singleton cache first (unless it's instance-per-resolve)
                if (!_instances.Contains(serviceType) && _singletons.TryGetValue(serviceType, out instance))
                    return true;
                
                if (!_registrations.TryGetValue(serviceType, out var factory))
                {{
                    instance = null;
                    return false;
                }}
                
                if (!_resolvingSet.Add(serviceType))
                {{
                    // Build dependency chain for error message
                    var chain = string.Join(" -> ", _resolvingStack.Reverse().Select(t => t.Name)) + " -> " + serviceType.Name;
                    throw new InvalidOperationException($"Circular dependency detected: {{chain}}");
                }}
                
                _resolvingStack.Push(serviceType);
                try
                {{
                    instance = factory(this);
                }}
                finally
                {{
                    _resolvingStack.Pop();
                    _resolvingSet.Remove(serviceType);
                }}
                
                // Only cache if not instance-per-resolve
                if (!_instances.Contains(serviceType))
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
    const string InstanceItemTemplate = "typeof({0})";

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
        var instances = new List<string>();
        
        // Group items by implementation type to handle multiple registrations
        var grouped = _items.GroupBy(item => item.Implementation, SymbolEqualityComparer.Default);
        
        foreach (var group in grouped)
        {
            var itemList = group.ToList();
            var firstItem = itemList[0];
            
            // Get constructor for the implementation
            var ctor = firstItem.Implementation.Constructors.Length > 0
                ? firstItem.Implementation.Constructors.OrderByDescending(c => c.Parameters.Length).First()
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
            
            if (firstItem.IsInstance)
            {
                // For instances, each registration gets its own factory (no sharing)
                foreach (var item in itemList)
                {
                    items.Add(string.Format(ItemTemplate, item.Service.ToDisplayString(), item.Implementation.ToDisplayString(), string.Join(", ", parameters)));
                    instances.Add(string.Format(InstanceItemTemplate, item.Service.ToDisplayString()));
                }
            }
            else
            {
                // For singletons with multiple registrations:
                // - First one becomes the "master" with the actual factory
                // - Others become "aliases" that resolve the master type
                
                // Add the master registration (first item or the one matching the implementation type)
                var masterItem = itemList.FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.Service, i.Implementation));
                if (masterItem.Equals(default(DependencyRegistryGenerator.Item)))
                    masterItem = firstItem;
                    
                items.Add(string.Format(ItemTemplate, masterItem.Service.ToDisplayString(), masterItem.Implementation.ToDisplayString(), string.Join(", ", parameters)));
                
                // Add alias registrations for the rest
                foreach (var item in itemList)
                {
                    if (SymbolEqualityComparer.Default.Equals(item.Service, masterItem.Service))
                        continue; // Skip the master
                    
                    // Alias: just resolve the master type
                    items.Add(string.Format("[typeof({0})] = dr => dr.Resolve<{1}>()", item.Service.ToDisplayString(), masterItem.Service.ToDisplayString()));
                }
            }
        }
        
        return string.Format(RegistryTemplate, string.Join(ItemSeparator, items), string.Join(ItemSeparator, instances));
    }
}