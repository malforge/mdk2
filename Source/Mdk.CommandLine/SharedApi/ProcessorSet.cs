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
    readonly List<Processor> _processors = new();

    public ProcessorSet(params Type[] types) : this((IEnumerable<Type>)types) { }

    /// <summary>
    ///     Create a new instance of <see cref="ProcessorSet{T}" />, using the specified types (in random order).
    ///     The processors will be sorted by their dependencies.
    /// </summary>
    /// <param name="types"></param>
    public ProcessorSet(IEnumerable<Type> types)
    {
        _processors.AddRange(types.Select(Processor.Create));
        SortAndValidate();
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

    void SortAndValidate()
    {
        var sorted = new List<Processor>();
        var remaining = new List<Processor>(_processors);

        while (remaining.Count > 0)
        {
            var added = false;
            for (var i = 0; i < remaining.Count; i++)
            {
                var processor = remaining[i];
                if (sorted.Any(p => processor.RunAfter.Contains(p.Type)))
                    continue;

                if (processor.RunBefore.Any(p => sorted.Any(s => s.Type == p)))
                    continue;

                sorted.Add(processor);
                remaining.RemoveAt(i);
                added = true;
                break;
            }

            if (!added)
                throw new InvalidOperationException("Failed to sort processors. There is a circular dependency.");
        }

        _processors.Clear();
        _processors.AddRange(sorted);
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
    }
}