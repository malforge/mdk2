namespace DISourceGenerator
{
    public class FrameworkProducer
    {
        public static readonly FrameworkProducer Instance = new();
        
        const string AttributeTemplate = 
            """
            #nullable enable
            
            using System;
            using System.Diagnostics.CodeAnalysis;
            
            namespace Mal.DependencyInjection;
            
            /// <summary>
            /// Marks a class as a singleton - one shared instance will be created and cached.
            /// </summary>
            /// <typeparam name="T">The service type to register the class as. If not specified, the class type itself is used.</typeparam>
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
            sealed class SingletonAttribute<T> : Attribute where T: class;

            /// <summary>
            /// Marks a class as a singleton - one shared instance will be created and cached.
            /// The class will be registered as its own type.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
            sealed class SingletonAttribute : Attribute;
            
            /// <summary>
            /// Marks a class to be created as a new instance on each resolve.
            /// </summary>
            /// <typeparam name="T">The service type to register the class as. If not specified, the class type itself is used.</typeparam>
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
            sealed class InstanceAttribute<T> : Attribute where T: class;

            /// <summary>
            /// Marks a class to be created as a new instance on each resolve.
            /// The class will be registered as its own type.
            /// </summary>
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
            sealed class InstanceAttribute : Attribute;
            
            /// <summary>
            /// Represents a simple dependency injection container that can resolve registered services.
            /// </summary>
            public interface IDependencyContainer
            {
                /// <summary>
                /// Resolves an instance of the specified service type.
                /// </summary>
                /// <typeparam name="T">The type of the service to resolve.</typeparam>
                /// <returns>The resolved service instance.</returns>
                /// <exception cref="InvalidOperationException">Thrown if the service type is not registered.</exception>
                T Resolve<T>() where T: class;
                
                /// <summary>
                /// Tries to resolve an instance of the specified service type.
                /// </summary>
                /// <param name="instance">The resolved instance, or null if the service is not registered.</param>
                /// <returns>True if the service was resolved; otherwise, false.</returns>
                bool TryResolve<T>([MaybeNullWhen(false)] out T instance) where T: class;
                
                /// <summary>
                /// Resolves an instance of the specified service type.
                /// </summary>
                /// <param name="serviceType">The type of the service to resolve.</param>
                /// <returns>The resolved service instance.</returns>
                /// <exception cref="InvalidOperationException">Thrown if the service type is not registered.</exception>
                object Resolve(Type serviceType);
                
                /// <summary>
                /// Tries to resolve an instance of the specified service type.
                /// </summary>
                /// <param name="serviceType">The type of the service to resolve.</param>
                /// <param name="instance">The resolved instance, or null if the service is not registered.</param>
                /// <returns>True if the service was resolved; otherwise, false.</returns>
                bool TryResolve(Type serviceType, [MaybeNullWhen(false)] out object instance);
            }
            """;
        
        public string Produce()
        {
            return AttributeTemplate;
        }
    }
}