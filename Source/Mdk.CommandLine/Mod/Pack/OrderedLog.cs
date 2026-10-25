using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Mdk.CommandLine.Shared.Api;

namespace Mdk.CommandLine.Mod.Pack;

/// <summary>
///    Overly-complicated class to keep the log messages in order when processing documents in parallel
///    but keeping messages in (almost) real time without blocking or wait for the task to finish.
/// </summary>
public class OrderedLog
{
    readonly IConsole _parent;
    readonly BufferSlot[] _slots;
    readonly Channel<Entry> _channel = Channel.CreateUnbounded<Entry>(new UnboundedChannelOptions { SingleReader = true });
    readonly Task _writer;

    public OrderedLog(IConsole parent, int count)
    {
        _parent = parent;
        _slots = Enumerable.Range(0, count).Select(i => new BufferSlot(this, i)).ToArray();
        _writer = Task.Run(WriteAsync);
    }

    public IConsole this[int index] => _slots[index];

    /// <summary>
    ///     Marks a document as finished, letting the writer move past it.
    /// </summary>
    public void Push(int index) => _channel.Writer.TryWrite(new Entry(index, true, false, null, 0));

    public Task CompleteAsync()
    {
        _channel.Writer.TryComplete();
        return _writer;
    }

    void Post(int index, bool isTrace, string? message, int wrapIndent) => _channel.Writer.TryWrite(new Entry(index, false, isTrace, message, wrapIndent));

    async Task WriteAsync()
    {
        var buffers = new List<Entry>?[_slots.Length];
        var completed = new bool[_slots.Length];
        var current = 0;

        await foreach (var entry in _channel.Reader.ReadAllAsync())
        {
            if (!entry.IsCompletion)
            {
                if (entry.Index == current)
                    Write(entry);
                else
                    (buffers[entry.Index] ??= []).Add(entry);
                continue;
            }

            completed[entry.Index] = true;
            while (current < completed.Length && completed[current])
            {
                current++;
                if (current < buffers.Length && buffers[current] is { } buffered)
                {
                    foreach (var message in buffered)
                        Write(message);
                    buffers[current] = null;
                }
            }
        }
    }

    void Write(Entry entry)
    {
        if (entry.IsTrace)
            _parent.Trace(entry.Message, entry.WrapIndent);
        else
            _parent.Print(entry.Message, entry.WrapIndent);
    }

    readonly record struct Entry(int Index, bool IsCompletion, bool IsTrace, string? Message, int WrapIndent);

    class BufferSlot(OrderedLog owner, int index) : IConsole
    {
        /// <inheritdoc />
        public bool TraceEnabled => owner._parent.TraceEnabled;

        /// <inheritdoc />
        public IConsole Trace(string? message = null, int wrapIndent = 4)
        {
            if (TraceEnabled)
                owner.Post(index, true, message, wrapIndent);
            return this;
        }

        /// <inheritdoc />
        public IConsole Print(string? message = null, int wrapIndent = 4)
        {
            owner.Post(index, false, message, wrapIndent);
            return this;
        }
    }
}