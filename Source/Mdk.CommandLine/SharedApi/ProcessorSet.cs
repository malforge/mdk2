using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Mdk.CommandLine.IngameScript.Api;

namespace Mdk.CommandLine.SharedApi;

/// <summary>
///     A collection of processors of a given type, sorted by their dependencies.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ProcessorSet<T> : IReadOnlyList<T> where T : class
{
    readonly List<Processor> _processors;

    public ProcessorSet(params Type[] types) : this((IEnumerable<Type>)types) { }

    /// <summary>
    ///     Create a new instance of <see cref="ProcessorSet{T}" />, using the specified types (in random order).
    ///     The processors will be sorted by their dependencies.
    /// </summary>
    /// <param name="types"></param>
    public ProcessorSet(IEnumerable<Type> types)
    {
        var processors = types.Select(Processor.Create).ToList();
        foreach (var processor in processors)
        {
            foreach (var runBefore in processor.RunBefore)
            {
                var before = runBefore;
                var targetIndex = processors.FindIndex(p => p.Type == before);
                if (targetIndex == -1)
                    throw new InvalidOperationException($"Processor {processor.Type.FullName} has a dependency on {runBefore.FullName}, which does not exist.");
                var target = processors[targetIndex];
                target = target.WithAdditionalRunAfter(processor.Type);
                processors[targetIndex] = target;
            }
        }

        _processors = SortAndValidate(processors);
    }

    ProcessorSet(List<Processor> processors)
    {
        _processors = SortAndValidate(processors);
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _processors.Select(p => p.Instance).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    ///     The number of processors in the set.
    /// </summary>
    public int Count => _processors.Count;

    /// <summary>
    ///     Get the processor at the specified index.
    /// </summary>
    /// <param name="index"></param>
    public T this[int index] => _processors[index].Instance;

    /// <summary>
    ///     Combine the current set with the specified types (in random order).
    ///     The processors will be sorted by their dependencies.
    /// </summary>
    /// <param name="types">The types to combine with the current set.</param>
    /// <returns>A new <see cref="ProcessorSet{T}" /> containing the combined processors, sorted by their dependencies.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public ProcessorSet<T> CombineWith(params Type[] types) => CombineWith((IEnumerable<Type>)types);

    /// <summary>
    ///     Combine the current set with the specified types (in random order).
    /// </summary>
    /// <param name="types">The types to combine with the current set.</param>
    /// <returns>A new <see cref="ProcessorSet{T}" /> containing the combined processors, sorted by their dependencies.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public ProcessorSet<T> CombineWith(IEnumerable<Type> types)
    {
        var processors = types.Select(Processor.Create).ToList();
        foreach (var processor in processors)
        {
            foreach (var runBefore in processor.RunBefore)
            {
                var before = runBefore;
                var targetIndex = processors.FindIndex(p => p.Type == before);
                if (targetIndex == -1)
                    throw new InvalidOperationException($"Processor {processor.Type.FullName} has a dependency on {runBefore.FullName}, which does not exist.");
                var target = processors[targetIndex];
                target = target.WithAdditionalRunAfter(processor.Type);
                processors[targetIndex] = target;
            }
        }

        return new ProcessorSet<T>(_processors.Concat(processors).ToList());
    }
    
    /// <summary>
    ///    Combine the current set with the specified set.
    ///    The processors will be sorted by their dependencies.
    /// </summary>
    /// <param name="other">The set to combine with the current set.</param>
    /// <returns>A new <see cref="ProcessorSet{T}" /> containing the combined processors, sorted by their dependencies.</returns>
    public ProcessorSet<T> CombineWith(ProcessorSet<T> other)
    {
        return new ProcessorSet<T>(_processors.Concat(other._processors).ToList());
    }

    static List<Processor> SortAndValidate(List<Processor> processors)
    {
        var sorted = new List<Processor>();
        var remaining = new List<Processor>(processors);
        var dependencyMap = processors.ToDictionary(p => p.Type, p => p.RunAfter.ToHashSet());

        while (remaining.Count > 0)
        {
            var added = false;
            foreach (var processor in remaining.ToList()) // ToList to avoid collection modification issues
            {
                if (!dependencyMap[processor.Type].Except(sorted.Select(s => s.Type)).Any())
                {
                    sorted.Add(processor);
                    remaining.Remove(processor);
                    added = true;
                    break; // Exit the loop since we've made a change
                }
            }

            if (!added)
                throw new InvalidOperationException("Failed to sort processors. There is a circular dependency.");
        }

        return sorted;
    }

    readonly struct Processor
    {
        public static Processor Create(Type type)
        {
            if (type.IsAbstract || type.IsInterface)
                throw new ArgumentException($"Type {type.FullName} is abstract or an interface and cannot be used as a processor.");

            if (!typeof(T).IsAssignableFrom(type))
                throw new ArgumentException($"Type {type.FullName} does not implement {typeof(T).FullName}.");

            var instance = (T?)Activator.CreateInstance(type);
            if (instance == null)
                throw new InvalidOperationException($"Failed to create an instance of {type.FullName}.");

            var runAfter = type.GetCustomAttributes(typeof(RunAfterAttribute), false)
                .OfType<RunAfterAttribute>()
                .Select(a => a.Type)
                .ToImmutableArray();

            var runBefore = type.GetCustomAttributes(typeof(RunBeforeAttribute), false)
                .OfType<RunBeforeAttribute>()
                .Select(a => a.Type)
                .ToImmutableArray();

            return new Processor(runAfter, runBefore, instance, type);
        }

        public readonly ImmutableArray<Type> RunAfter;
        public readonly ImmutableArray<Type> RunBefore;
        public readonly T Instance;
        public readonly Type Type;

        Processor(ImmutableArray<Type> runAfter, ImmutableArray<Type> runBefore, T instance, Type type)
        {
            RunAfter = runAfter;
            RunBefore = runBefore;
            Instance = instance;
            Type = type;
        }

        public Processor WithAdditionalRunAfter(Type type)
        {
            var newRunAfter = RunAfter.Add(type);
            return new Processor(newRunAfter, RunBefore, Instance, Type);
        }
    }
}