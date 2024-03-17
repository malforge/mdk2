using System;
using System.Collections.Immutable;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.IngameScript.Pack;

/// <summary>
///     A manager for all the processing steps required to pack a script.
/// </summary>
public class ScriptProcessingManager
{
    /// <summary>
    ///     A list of preprocessors to run before the script is combined, in workflow order.
    /// </summary>
    public required ProcessorSet<IScriptPreprocessor> Preprocessors { get; init; }

    /// <summary>
    ///     A combiner to combine the script into a single document.
    /// </summary>
    public required IScriptCombiner Combiner { get; init; }

    /// <summary>
    ///     A list of postprocessors to run after the script is combined, in workflow order.
    /// </summary>
    public required ProcessorSet<IScriptPostprocessor> Postprocessors { get; init; }

    /// <summary>
    ///     A composer to compose the final script.
    /// </summary>
    public required IScriptComposer Composer { get; init; }

    /// <summary>
    ///     A list of post-composition processors to run after the script is composed, in workflow order.
    /// </summary>
    public required ProcessorSet<IScriptPostCompositionProcessor> PostCompositionProcessors { get; init; }

    /// <summary>
    ///     A producer to produce the files for the script, in the desired output folder.
    /// </summary>
    public required IScriptProducer Producer { get; init; }

    /// <summary>
    ///     Begin construction of a new <see cref="ScriptProcessingManager" />. Contains the defaults, and as such
    ///     can either be used as-is, or modified to suit the needs of the user.
    /// </summary>
    /// <returns>A new <see cref="ProcessingManagerBuilder" />.</returns>
    public static ProcessingManagerBuilder Create() =>
        new()
        {
            Preprocessors = DefaultProcessorTypes.Preprocessors.ToImmutableArray(),
            Combiner = DefaultProcessorTypes.Combiner,
            Postprocessors = DefaultProcessorTypes.Postprocessors.ToImmutableArray(),
            Composer = DefaultProcessorTypes.Composer,
            PostCompositionProcessors = DefaultProcessorTypes.PostCompositionProcessors.ToImmutableArray(),
            Producer = DefaultProcessorTypes.Producer
        };

    /// <summary>
    ///     A builder for a <see cref="ScriptProcessingManager" />.
    /// </summary>
    public readonly struct ProcessingManagerBuilder
    {
        /// <summary>
        ///     A list of types to use as preprocessors (see <see cref="ScriptProcessingManager.Preprocessors" />).
        /// </summary>
        public ImmutableArray<Type> Preprocessors { get; init; }

        /// <summary>
        ///     A type to use as a combiner (see <see cref="ScriptProcessingManager.Combiner" />).
        /// </summary>
        public Type? Combiner { get; init; }

        /// <summary>
        ///     A list of types to use as postprocessors (see <see cref="ScriptProcessingManager.Postprocessors" />).
        /// </summary>
        public ImmutableArray<Type> Postprocessors { get; init; }

        /// <summary>
        ///     A type to use as a composer (see <see cref="ScriptProcessingManager.Composer" />).
        /// </summary>
        public Type? Composer { get; init; }

        /// <summary>
        ///     A list of types to use as post-composition processors (see
        ///     <see cref="ScriptProcessingManager.PostCompositionProcessors" />).
        /// </summary>
        public ImmutableArray<Type> PostCompositionProcessors { get; init; }

        /// <summary>
        ///     A type to use as a producer (see <see cref="ScriptProcessingManager.Producer" />).
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
                if (!typeof(IScriptPreprocessor).IsAssignableFrom(type))
                    throw new InvalidOperationException($"Type {type.FullName} does not implement {typeof(IScriptPreprocessor).FullName}.");
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    throw new InvalidOperationException($"Type {type.FullName} does not have a parameterless constructor.");
            }
            return new ProcessingManagerBuilder
            {
                Preprocessors = Preprocessors.AddRange(types)
            };
        }

        /// <summary>
        ///     Replace the combiner in the builder.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ProcessingManagerBuilder WithCombiner(Type? type)
        {
            if (type is null) return this;
            if (!typeof(IScriptCombiner).IsAssignableFrom(type))
                throw new InvalidOperationException($"Type {type.FullName} does not implement {typeof(IScriptCombiner).FullName}.");
            if (type.GetConstructor(Type.EmptyTypes) == null)
                throw new InvalidOperationException($"Type {type.FullName} does not have a parameterless constructor.");
            return new ProcessingManagerBuilder
            {
                Combiner = type
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
                if (!typeof(IScriptPostprocessor).IsAssignableFrom(type))
                    throw new InvalidOperationException($"Type {type.FullName} does not implement {typeof(IScriptPostprocessor).FullName}.");
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    throw new InvalidOperationException($"Type {type.FullName} does not have a parameterless constructor.");
            }
            return new ProcessingManagerBuilder
            {
                Postprocessors = Postprocessors.AddRange(types)
            };
        }

        /// <summary>
        ///     Replace the composer in the builder.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ProcessingManagerBuilder WithComposer(Type? type)
        {
            if (type is null) return this;
            if (!typeof(IScriptComposer).IsAssignableFrom(type))
                throw new InvalidOperationException($"Type {type.FullName} does not implement {typeof(IScriptComposer).FullName}.");
            if (type.GetConstructor(Type.EmptyTypes) == null)
                throw new InvalidOperationException($"Type {type.FullName} does not have a parameterless constructor.");
            return new ProcessingManagerBuilder
            {
                Composer = type
            };
        }

        /// <summary>
        ///     Add additional post-composition processors to the builder.
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ProcessingManagerBuilder WithAdditionalPostCompositionProcessors(params Type[]? types)
        {
            if (types is null || types.Length == 0) return this;
            foreach (var type in types)
            {
                if (!typeof(IScriptPostCompositionProcessor).IsAssignableFrom(type))
                    throw new InvalidOperationException($"Type {type.FullName} does not implement {typeof(IScriptPostCompositionProcessor).FullName}.");
                if (type.GetConstructor(Type.EmptyTypes) == null)
                    throw new InvalidOperationException($"Type {type.FullName} does not have a parameterless constructor.");
            }
            return new ProcessingManagerBuilder
            {
                PostCompositionProcessors = PostCompositionProcessors.AddRange(types)
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
            if (!typeof(IScriptProducer).IsAssignableFrom(type))
                throw new InvalidOperationException($"Type {type.FullName} does not implement {typeof(IScriptProducer).FullName}.");
            if (type.GetConstructor(Type.EmptyTypes) == null)
                throw new InvalidOperationException($"Type {type.FullName} does not have a parameterless constructor.");
            return new ProcessingManagerBuilder
            {
                Producer = type
            };
        }

        /// <summary>
        ///     Build the <see cref="ScriptProcessingManager" />.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public ScriptProcessingManager Build()
        {
            if (Combiner == null)
                throw new InvalidOperationException("Combiner must be specified.");
            if (Composer == null)
                throw new InvalidOperationException("Composer must be specified.");
            if (Producer == null)
                throw new InvalidOperationException("Producer must be specified.");

            return new ScriptProcessingManager
            {
                Preprocessors = new ProcessorSet<IScriptPreprocessor>(Preprocessors),
                Combiner = (IScriptCombiner?)Activator.CreateInstance(Combiner) ?? throw new InvalidOperationException("Combiner could not be created."),
                Postprocessors = new ProcessorSet<IScriptPostprocessor>(Postprocessors),
                Composer = (IScriptComposer?)Activator.CreateInstance(Composer) ?? throw new InvalidOperationException("Composer could not be created."),
                PostCompositionProcessors = new ProcessorSet<IScriptPostCompositionProcessor>(PostCompositionProcessors),
                Producer = (IScriptProducer?)Activator.CreateInstance(Producer) ?? throw new InvalidOperationException("Producer could not be created.")
            };
        }
    }

    static class DefaultProcessorTypes
    {
        public static readonly Type[] Preprocessors = [typeof(PreprocessorConditionals), typeof(DeleteNamespaces)];
        public static readonly Type Combiner = typeof(Combiner);
        public static readonly Type[] Postprocessors = [typeof(PartialMerger), typeof(RegionAnnotator), typeof(TypeSorter), typeof(SymbolProtectionAnnotator)];
        public static readonly Type Composer = typeof(Composer);
        public static readonly Type[] PostCompositionProcessors = Array.Empty<Type>();
        public static readonly Type Producer = typeof(Producer);
    }
}