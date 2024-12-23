using System;
using System.Collections.Immutable;
using Mdk.CommandLine.Mod.Pack.Api;
using Mdk.CommandLine.Mod.Pack.DefaultProcessors;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.Mod.Pack;

/// <summary>
///     A manager for all the processing steps required to pack a script.
/// </summary>
public class ModProcessingManager
{
    /// <summary>
    ///     A list of preprocessors to run before the script is combined, in workflow order.
    /// </summary>
    public required ProcessorSet<IModScriptPreprocessor> Preprocessors { get; init; }

    /// <summary>
    ///     A list of postprocessors to run after the script is combined, in workflow order.
    /// </summary>
    public required ProcessorSet<IModScriptProcessor> Postprocessors { get; init; }

    /// <summary>
    ///     A producer to produce the files for the script, in the desired output folder.
    /// </summary>
    public required IModProducer Producer { get; init; }

    /// <summary>
    ///     Begin construction of a new <see cref="ModProcessingManager" />. Contains the defaults, and as such
    ///     can either be used as-is, or modified to suit the needs of the user.
    /// </summary>
    /// <returns>A new <see cref="ProcessingManagerBuilder" />.</returns>
    public static ProcessingManagerBuilder Create() =>
        new()
        {
            Preprocessors = [..DefaultProcessorTypes.Preprocessors],
            Postprocessors = [..DefaultProcessorTypes.Postprocessors],
            Producer = DefaultProcessorTypes.Producer
        };

    /// <summary>
    ///     A builder for a <see cref="ModProcessingManager" />.
    /// </summary>
    public readonly struct ProcessingManagerBuilder
    {
        /// <summary>
        ///     A list of types to use as preprocessors (see <see cref="ModProcessingManager.Preprocessors" />).
        /// </summary>
        public ImmutableArray<Type> Preprocessors { get; init; }

        /// <summary>
        ///     A list of types to use as postprocessors (see <see cref="ModProcessingManager.Postprocessors" />).
        /// </summary>
        public ImmutableArray<Type> Postprocessors { get; init; }

        /// <summary>
        ///     A type to use as a producer (see <see cref="ModProcessingManager.Producer" />).
        /// </summary>
        public Type? Producer { get; init; }

        /// <summary>
        ///     Add additional preprocessors to the builder.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ProcessingManagerBuilder WithAdditionalPreprocessors(params Type[]? types)
        {
            if (types is null || types.Length == 0) return this;
            foreach (var type in types)
            {
                if (!typeof(IModScriptPreprocessor).IsAssignableFrom(type))
                    throw new InvalidOperationException($"Type {type.FullName} does not implement {typeof(IModScriptPreprocessor).FullName}.");
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    throw new InvalidOperationException($"Type {type.FullName} does not have a parameterless constructor.");
            }
            return new ProcessingManagerBuilder
            {
                Preprocessors = Preprocessors.AddRange(types)
            };
        }
        
        /// <summary>
        ///     Add additional postprocessors to the builder.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ProcessingManagerBuilder WithAdditionalPostprocessors(params Type[]? types)
        {
            if (types is null || types.Length == 0) return this;
            foreach (var type in types)
            {
                if (!typeof(IModScriptProcessor).IsAssignableFrom(type))
                    throw new InvalidOperationException($"Type {type.FullName} does not implement {typeof(IModScriptProcessor).FullName}.");
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    throw new InvalidOperationException($"Type {type.FullName} does not have a parameterless constructor.");
            }
            return new ProcessingManagerBuilder
            {
                Postprocessors = Postprocessors.AddRange(types)
            };
        }
        
        /// <summary>
        ///     Replace the producer in the builder.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ProcessingManagerBuilder WithProducer(Type? type)
        {
            if (type is null) return this;
            if (!typeof(IModProducer).IsAssignableFrom(type))
                throw new InvalidOperationException($"Type {type.FullName} does not implement {typeof(IModProducer).FullName}.");
            if (type.GetConstructor(Type.EmptyTypes) == null)
                throw new InvalidOperationException($"Type {type.FullName} does not have a parameterless constructor.");
            return new ProcessingManagerBuilder
            {
                Producer = type
            };
        }

        /// <summary>
        ///     Build the <see cref="ModProcessingManager" />.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ModProcessingManager Build()
        {
            if (Producer == null)
                throw new InvalidOperationException("Producer must be specified.");

            return new ModProcessingManager
            {
                Preprocessors = new ProcessorSet<IModScriptPreprocessor>(Preprocessors),
                Postprocessors = new ProcessorSet<IModScriptProcessor>(Postprocessors),
                Producer = (IModProducer?)Activator.CreateInstance(Producer) ?? throw new InvalidOperationException("Producer could not be created.")
            };
        }
    }

    static class DefaultProcessorTypes
    {
        public static readonly Type[] Preprocessors = [typeof(PreprocessorConditionals)];
        public static readonly Type[] Postprocessors = [typeof(RegionAnnotator)];
        public static readonly Type Producer = typeof(Producer);
    }
}