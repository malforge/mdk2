// Mdk.Extractor
// 
// Copyright 2023 Morten A. Lyrstad

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mdk.Extractor;

public class CommandLine
{
    public static IEnumerable<string> Split(string input)
    {
        var start = new TextPtr(input, 0)
            .SkipWhitespace();
        var end = start;
        var isQuoted = false;
        var buffer = new StringBuilder(1024);
        while (!end.IsOutOfBounds)
        {
            if (!isQuoted && end.IsWhitespace())
            {
                start.ReadTo(end, buffer);
                if (buffer.Length > 0)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                }

                end = start.SkipWhitespace();
                start = end;
                continue;
            }

            if (end == '\"')
            {
                isQuoted = !isQuoted;
                start.ReadTo(end, buffer);
                start = end + 1;
                continue;
            }

            end++;
        }

        start.ReadTo(end, buffer);
        if (buffer.Length <= 0)
            yield break;
        yield return buffer.ToString();
    }

    readonly List<string> _args;

    public CommandLine(string input)
    {
        _args = Split(input).ToList();
    }

    public int Count => _args.Count;

    public string this[int index] => index < 0 || index >= _args.Count ? null : _args[index];

    public int IndexOf(string what) => _args.FindIndex(s => string.Equals(what, s, StringComparison.OrdinalIgnoreCase));

    readonly struct TextPtr
    {
        public static TextPtr operator +(in TextPtr ptr, int n) => new TextPtr(ptr.Text, ptr.Index + n);

        public static TextPtr operator ++(in TextPtr ptr) => ptr + 1;

        public static TextPtr operator -(in TextPtr ptr, int n) => new TextPtr(ptr.Text, ptr.Index + n);

        public static TextPtr operator --(in TextPtr ptr) => ptr - 1;

        public static implicit operator char(in TextPtr ptr) => ptr[0];

        public readonly string Text;
        public readonly int Index;

        public TextPtr(string text, int index)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Index = index;
        }

        public char this[int index]
        {
            get
            {
                if (Text == null)
                    return '\0';
                index += Index;
                if (index < 0 || index >= Text.Length)
                    return '\0';
                return Text[index];
            }
        }

        public bool IsOutOfBounds => Text == null || Index < 0 || Index >= Text.Length;

        public bool IsEmpty() => Text == null;

        public TextPtr SkipWhitespace()
        {
            var me = this;
            while (char.IsWhiteSpace(me))
                me++;
            return me;
        }

        public bool IsWhitespace() => char.IsWhiteSpace(this);

        public string TakeUntil(TextPtr end) => Text.Substring(Index, end.Index - Index);

        public void ReadTo(TextPtr end, StringBuilder buffer)
        {
            if (end.Index <= Index)
                return;
            buffer.Append(Text, Index, end.Index - Index);
        }
    }

    public bool HasSwitch(string switchName) => IndexOf(switchName) >= 0;
    
    public bool TryGetSwitch(string switchName, out string value)
    {
        var index = IndexOf(switchName);
        if (index >= 0 && Count > index + 1)
        {
            value = this[index + 1];
            return true;
        }

        value = default;
        return false;
    }
}