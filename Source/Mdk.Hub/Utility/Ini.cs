using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using Mdk.Hub.Features.Diagnostics;

namespace Mdk.Hub.Utility;

/// <summary>
///     A simple, immutable INI file reader and writer.
/// </summary>
public class Ini
{
    readonly ImmutableArray<Section> _sections;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Ini" /> class.
    /// </summary>
    /// <param name="trailingTrivia"></param>
    public Ini(string? trailingTrivia = null)
        : this(ImmutableArray<Section>.Empty, trailingTrivia) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Ini" /> class.
    /// </summary>
    /// <param name="sections"></param>
    /// <param name="trailingTrivia"></param>
    public Ini(IEnumerable<Section> sections, string? trailingTrivia = null)
        : this(sections.ToImmutableArray(), trailingTrivia) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Ini" /> class.
    /// </summary>
    /// <param name="sections"></param>
    /// <param name="trailingTrivia"></param>
    public Ini(ImmutableArray<Section> sections, string? trailingTrivia = null)
    {
        _sections = sections;
        TrailingTrivia = trailingTrivia;
    }

    /// <summary>
    ///     Gets an optional trailing comment (or whitespace) for the INI file.
    /// </summary>
    public string? TrailingTrivia { get; }

    /// <summary>
    ///     Gets a specific section by name.
    /// </summary>
    /// <param name="section"></param>
    public Section this[string section]
    {
        get
        {
            foreach (var s in _sections)
            {
                if (string.Equals(s.Name, section, StringComparison.OrdinalIgnoreCase))
                    return s;
            }
            return new Section(section);
        }
    }

    /// <summary>
    ///     Creates a clone of the current instance with the specified trailing comment.
    /// </summary>
    /// <param name="trivia"></param>
    /// <returns></returns>
    public Ini WithTrailingTrivia(string trivia) => new(_sections, trivia);

    /// <summary>
    ///     Creates a clone of the current instance without a trailing comment.
    /// </summary>
    /// <returns></returns>
    public Ini WithoutTrailingTrivia() => new(_sections);

    /// <summary>
    ///     Creates a new <see cref="Ini" /> instance from the specified file.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Ini FromFile(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        var ini = File.ReadAllText(fileName);
        if (!TryParse(ini, out var result))
            throw new ArgumentException("Invalid INI file format.", nameof(fileName));
        return result;
    }

    /// <summary>
    ///     Attempts to parse the specified INI file content.
    /// </summary>
    /// <param name="ini"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryParse(string? ini, out Ini result) =>
        TryParse(ini, null, out result);

    /// <summary>
    ///     Attempts to parse the specified INI file content with optional diagnostic logging.
    /// </summary>
    /// <param name="ini"></param>
    /// <param name="logger"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static bool TryParse(string? ini, ILogger? logger, out Ini result)
    {
        if (string.IsNullOrWhiteSpace(ini))
        {
            result = new Ini();
            return false;
        }

        var sections = new List<Section>();
        Section? currentSection = null;
        StringBuilder comment = new();
        bool hasTrivia = false;  // Track if we have any trivia (comments or blank lines)

        bool tryReadLine(string text, ref int index, out ReadOnlySpan<char> line)
        {
            var start = index;
            while (index < text.Length && text[index] != '\n')
                index++;
            if (index < text.Length && text[index] == '\n')
                index++;
            line = text.AsSpan(start, index - start);
            if (line.EndsWith("\r\n"))
                line = line[..^2];
            else if (line.EndsWith("\n"))
                line = line[..^1];
            return start < text.Length;
        }

        var addNewline = false;

        var index = 0;
        var lineNumber = 0;
        while (tryReadLine(ini, ref index, out var line))
        {
            lineNumber++;
            var trimmed = line.Trim();
            if (trimmed.StartsWith(";"))
            {
                // Comment line - append with newline separator if needed
                if (comment.Length > 0 || hasTrivia)
                    comment.Append("\r\n");
                comment.Append(line.ToString());
                hasTrivia = true;
                continue;
            }
            if (trimmed.IsEmpty || trimmed.IsWhiteSpace())
            {
                // Blank line - record it in trivia
                if (comment.Length > 0 || hasTrivia)
                    comment.Append("\r\n");
                // Append the line content (may be empty for completely blank lines)
                comment.Append(line.ToString());
                hasTrivia = true;
                continue;
            }
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                if (currentSection is not null)
                    sections.Add(currentSection.Value);
                currentSection = new Section(trimmed[1..^1].ToString());
                if (hasTrivia)
                {
                    // Add final newline to section trivia
                    comment.Append("\r\n");
                    currentSection = currentSection.Value.WithComment(comment.ToString());
                    comment.Clear();
                    hasTrivia = false;
                }
            }
            else if (currentSection is not null)
            {
                var equals = trimmed.IndexOf('=');
                if (equals < 0)
                {
                    logger?.Warning($"Skipping malformed INI line {lineNumber} (missing '='): {trimmed.ToString()}");
                    continue;
                }
                var keyName = trimmed[..equals].Trim().ToString();
                var keyValue = trimmed[(equals + 1)..].Trim().ToString();
                
                // Create key with its comment (if any was accumulated before this key)
                string? keyComment = null;
                if (hasTrivia)
                {
                    // Add final newline to key trivia
                    comment.Append("\r\n");
                    keyComment = comment.ToString();
                    comment.Clear();
                    hasTrivia = false;
                }
                
                var newKey = new Key(keyName, keyValue, keyComment);
                currentSection = currentSection.Value.WithKey(newKey);
            }
            else
            {
                // Key=value line outside of any section
                logger?.Warning($"Skipping INI line {lineNumber} outside of section: {trimmed.ToString()}");
            }
        }
        if (currentSection is not null)
            sections.Add(currentSection.Value);
        
        // Handle trailing trivia (comments/blank lines after last section)
        if (hasTrivia)
        {
            // Add final \r\n for consistency with other trivia
            comment.Append("\r\n");
            result = new Ini(sections, comment.ToString());
        }
        else
        {
            result = new Ini(sections);
        }
        return true;
    }

    /// <summary>
    ///     Creates a clone of the current instance with the specified section.
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public Ini WithSection(Section section)
    {
        var builder = ImmutableArray.CreateBuilder<Section>();
        bool found = false;
        foreach (var s in _sections)
        {
            if (string.Equals(s.Name, section.Name, StringComparison.OrdinalIgnoreCase))
            {
                builder.Add(section);
                found = true;
            }
            else
            {
                builder.Add(s);
            }
        }
        if (!found)
            builder.Add(section);
        return new Ini(builder.ToImmutable(), TrailingTrivia);
    }

    /// <summary>
    ///     Creates a clone of the current instance with the specified section.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="buildSection"></param>
    /// <returns></returns>
    public Ini WithSection(string name, Func<Section, Section> buildSection)
    {
        var builder = ImmutableArray.CreateBuilder<Section>();
        bool found = false;
        foreach (var s in _sections)
        {
            if (string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                builder.Add(buildSection(s));
                found = true;
            }
            else
            {
                builder.Add(s);
            }
        }
        if (!found)
            builder.Add(buildSection(new Section(name)));
        return new Ini(builder.ToImmutable(), TrailingTrivia);
    }

    /// <summary>
    ///     Creates a clone of the current instance with the specified section.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="keys"></param>
    /// <returns></returns>
    public Ini WithSection(string name, IEnumerable<Key> keys)
    {
        var builder = ImmutableArray.CreateBuilder<Section>();
        bool found = false;
        foreach (var s in _sections)
        {
            if (string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                builder.Add(new Section(name, keys));
                found = true;
            }
            else
            {
                builder.Add(s);
            }
        }
        if (!found)
            builder.Add(new Section(name, keys));
        return new Ini(builder.ToImmutable(), TrailingTrivia);
    }

    /// <summary>
    ///     Creates a clone of the current instance with the specified section.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Ini WithSection(string name)
    {
        // If the section already exists, return the current instance
        foreach (var s in _sections)
        {
            if (string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                return this;
        }

        return new Ini(_sections.Add(new Section(name)), TrailingTrivia);
    }

    /// <summary>
    ///     Creates a clone of the current instance without the specified section.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Ini WithoutSection(string name)
    {
        var builder = ImmutableArray.CreateBuilder<Section>();
        foreach (var s in _sections)
        {
            if (!string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                builder.Add(s);
        }
        return new Ini(builder.ToImmutable(), TrailingTrivia);
    }

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public Ini WithKey(string section, Key key)
    {
        var builder = ImmutableArray.CreateBuilder<Section>();
        bool found = false;
        foreach (var s in _sections)
        {
            if (string.Equals(s.Name, section, StringComparison.OrdinalIgnoreCase))
            {
                builder.Add(s.WithKey(key));
                found = true;
            }
            else
            {
                builder.Add(s);
            }
        }
        if (!found)
            builder.Add(new Section(section, new[] { key }));
        return new Ini(builder.ToImmutable(), TrailingTrivia);
    }

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ini WithKey(string section, string name, string value)
    {
        var builder = ImmutableArray.CreateBuilder<Section>();
        bool found = false;
        foreach (var s in _sections)
        {
            if (string.Equals(s.Name, section, StringComparison.OrdinalIgnoreCase))
            {
                builder.Add(s.WithKey(name, value));
                found = true;
            }
            else
            {
                builder.Add(s);
            }
        }
        if (!found)
            builder.Add(new Section(section, new[] { new Key(name, value, null) }));
        return new Ini(builder.ToImmutable(), TrailingTrivia);
    }

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ini WithKey<T>(string section, string name, T value) where T : Enum
        => WithKey(section, name, value.ToString());

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ini WithKey(string section, string name, bool value)
        => WithKey(section, name, value ? "true" : "false");

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ini WithKey(string section, string name, int value)
        => WithKey(section, name, value.ToString());

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ini WithKey(string section, string name, long value)
        => WithKey(section, name, value.ToString());

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ini WithKey(string section, string name, short value)
        => WithKey(section, name, value.ToString());

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ini WithKey(string section, string name, byte value)
        => WithKey(section, name, value.ToString());

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ini WithKey(string section, string name, sbyte value)
        => WithKey(section, name, value.ToString());

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ini WithKey(string section, string name, char value)
        => WithKey(section, name, value.ToString());

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ini WithKey(string section, string name, float value)
        => WithKey(section, name, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ini WithKey(string section, string name, double value)
        => WithKey(section, name, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    ///     Creates a clone of the current instance with the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public Ini WithKey(string section, string name, decimal value)
        => WithKey(section, name, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    ///     Creates a clone of the current instance without the specified key.
    /// </summary>
    /// <param name="section"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public Ini WithoutKey(string section, string name)
    {
        var builder = ImmutableArray.CreateBuilder<Section>();
        foreach (var s in _sections)
        {
            if (string.Equals(s.Name, section, StringComparison.OrdinalIgnoreCase))
            {
                builder.Add(s.WithoutKey(name));
            }
            else
            {
                builder.Add(s);
            }
        }
        return new Ini(builder.ToImmutable(), TrailingTrivia);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var builder = new StringBuilder();
        foreach (var section in _sections)
        {
            // Write section leading trivia exactly as stored (already includes ; and blank lines)
            if (!string.IsNullOrEmpty(section.LeadingComment))
                builder.Append(section.LeadingComment);
                
            builder.AppendLine($"[{section.Name}]");
            
            foreach (var key in section.Keys)
            {
                // Write key trivia exactly as stored (already includes ; and blank lines)
                if (!string.IsNullOrEmpty(key.Comment))
                    builder.Append(key.Comment);
                    
                builder.AppendLine($"{key.Name}={key.Value}");
            }
        }
        if (!string.IsNullOrEmpty(TrailingTrivia))
            builder.Append(TrailingTrivia);
        return builder.ToString();
    }

    /// <summary>
    ///     A section in an INI file.
    /// </summary>
    public readonly struct Section
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Section" /> class.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="leadingComment"></param>
        public Section(string name, string? leadingComment = null)
            : this(name, ImmutableArray<Key>.Empty, leadingComment) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Section" /> class.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="keys"></param>
        /// <param name="leadingComment"></param>
        public Section(string name, IEnumerable<Key> keys, string? leadingComment = null)
            : this(name, keys.ToImmutableArray(), leadingComment) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Section" /> class.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="keys"></param>
        /// <param name="leadingComment"></param>
        public Section(string name, ImmutableArray<Key> keys, string? leadingComment = null)
        {
            LeadingComment = leadingComment;
            Name = name;
            Keys = keys;
        }

        /// <summary>
        ///     Gets an optional leading comment (or whitespace) for the section.
        /// </summary>
        public string? LeadingComment { get; }

        /// <summary>
        ///     Gets the name of the section.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the keys in the section (ordered).
        /// </summary>
        public ImmutableArray<Key> Keys { get; }

        /// <summary>
        ///     Gets a specific key by name.
        /// </summary>
        /// <param name="key"></param>
        public Key this[string key]
        {
            get
            {
                foreach (var k in Keys)
                {
                    if (string.Equals(k.Name, key, StringComparison.OrdinalIgnoreCase))
                        return k;
                }
                return Key.Empty;
            }
        }

        /// <summary>
        ///     Creates a clone of the current instance with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Section WithKey(Key key)
        {
            // Find existing key index
            var index = -1;
            for (var i = 0; i < Keys.Length; i++)
            {
                if (string.Equals(Keys[i].Name, key.Name, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            // Replace existing or append new
            var newKeys = index >= 0 
                ? Keys.SetItem(index, key) 
                : Keys.Add(key);
            
            return new(Name, newKeys, LeadingComment);
        }

        /// <summary>
        ///     Creates a clone of the current instance with the specified key.
        ///     Preserves the comment from the existing key if present.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Section WithKey(string name, string value)
        {
            // Find existing key to preserve comment
            string? existingComment = null;
            var index = -1;
            for (var i = 0; i < Keys.Length; i++)
            {
                if (string.Equals(Keys[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    existingComment = Keys[i].Comment;
                    index = i;
                    break;
                }
            }

            var newKey = new Key(name, value, existingComment);
            var newKeys = index >= 0 
                ? Keys.SetItem(index, newKey) 
                : Keys.Add(newKey);
            
            return new(Name, newKeys, LeadingComment);
        }

        /// <summary>
        ///     Creates a clone of the current instance without the specified key.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Section WithoutKey(string name)
        {
            var index = -1;
            for (var i = 0; i < Keys.Length; i++)
            {
                if (string.Equals(Keys[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                return this;

            return new(Name, Keys.RemoveAt(index), LeadingComment);
        }

        /// <summary>
        ///     Creates a clone of the current instance with the specified leading comment.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public Section WithComment(string comment) => new(Name, Keys, comment);

        /// <summary>
        ///     Creates a clone of the current instance without a leading comment.
        /// </summary>
        /// <returns></returns>
        public Section WithoutComment() => new(Name, Keys);

        /// <summary>
        ///     Attempts to get the specified key.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool TryGet(string name, out Key key)
        {
            foreach (var k in Keys)
            {
                if (string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    key = k;
                    return true;
                }
            }
            key = new Key(name, null, null);
            return false;
        }

        /// <summary>
        ///     Attempts to get the value of the specified key.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(string name, out bool value)
        {
            foreach (var k in Keys)
            {
                if (string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = k.ToBool();
                    return true;
                }
            }
            value = false;
            return false;
        }

        /// <summary>
        ///     Attempts to get the value of the specified key.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(string name, [MaybeNullWhen(false)] out string value)
        {
            foreach (var k in Keys)
            {
                if (string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = k.ToString()!;
                    return true;
                }
            }
            value = string.Empty;
            return false;
        }

        /// <summary>
        ///     Attempts to get the value of the specified key.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(string name, out int value)
        {
            foreach (var k in Keys)
            {
                if (string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = k.ToInt();
                    return true;
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        ///     Attempts to get the value of the specified key.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(string name, out long value)
        {
            foreach (var k in Keys)
            {
                if (string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = k.ToLong();
                    return true;
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        ///     Attempts to get the value of the specified key.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(string name, out float value)
        {
            foreach (var k in Keys)
            {
                if (string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = k.ToFloat();
                    return true;
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        ///     Attempts to get the value of the specified key.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(string name, out double value)
        {
            foreach (var k in Keys)
            {
                if (string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = k.ToDouble();
                    return true;
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        ///     Attempts to get the value of the specified key.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGet(string name, out decimal value)
        {
            foreach (var k in Keys)
            {
                if (string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = k.ToDecimal();
                    return true;
                }
            }
            value = 0;
            return false;
        }

        /// <summary>
        ///     Attempts to get the value of the specified key.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryGet<T>(string name, out T value) where T : struct, Enum
        {
            foreach (var k in Keys)
            {
                if (string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = k.ToEnum<T>();
                    return true;
                }
            }
            value = default;
            return false;
        }

        /// <summary>
        ///     Determines whether the section contains the specified key.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasKey(string name)
        {
            foreach (var k in Keys)
            {
                if (string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    ///     A key in an INI file.
    /// </summary>
    public readonly struct Key
    {
        /// <summary>
        ///     An empty key.
        /// </summary>
        public static readonly Key Empty = new();

        /// <summary>
        ///     Creates a new key with the specified name and value.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="comment"></param>
        /// <exception cref="ArgumentException"></exception>
        public Key(string name, string? value, string? comment)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            Name = name;
            Value = value;
            Comment = comment;
        }

        /// <summary>
        ///     Determines whether the key is empty.
        /// </summary>
        public bool IsEmpty() => string.IsNullOrWhiteSpace(Name);

        /// <summary>
        ///     An optional comment (or whitespace) for the key.
        /// </summary>
        public readonly string? Comment;

        /// <summary>
        ///     The name of the key.
        /// </summary>
        public readonly string Name;

        /// <summary>
        ///     The raw value of the key.
        /// </summary>
        public readonly string? Value;

        /// <summary>
        ///     Creates a clone of the current instance with the specified comment.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public Key WithComment(string comment) => new(Name, Value, comment);

        /// <summary>
        ///     Creates a clone of the current instance without a comment.
        /// </summary>
        /// <returns></returns>
        public Key WithoutComment() => new(Name, Value, null);

        /// <summary>
        ///     Creates a clone of the current instance with the specified value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Key WithValue(string value) => new(Name, value, Comment);

        /// <summary>
        ///     Creates a clone of the current instance without a value.
        /// </summary>
        /// <returns></returns>
        public Key WithoutValue() => new(Name, null, Comment);

        /// <summary>
        ///     Attempts to read the raw value as the desired enum type. Returns the default value if the value is null or cannot
        ///     be parsed.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ToEnum<T>(T defaultValue = default) where T : struct, Enum
        {
            if (Value is null)
                return defaultValue;
            if (!Enum.TryParse<T>(Value, true, out var result))
                return defaultValue;
            return result;
        }

        /// <summary>
        ///     Attempts to read the raw value as a boolean. Returns the default value if the value is null or cannot be parsed.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public bool ToBool(bool defaultValue = false)
        {
            if (Value is null)
                return defaultValue;
            if (string.Equals(Value, "true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(Value, "on", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(Value, "yes", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(Value, "1", StringComparison.OrdinalIgnoreCase))
                return true;
            return defaultValue;
        }

        /// <summary>
        ///     Attempts to read the raw value as an integer. Returns the default value if the value is null or cannot be parsed.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public int ToInt(int defaultValue = 0)
        {
            if (Value is null)
                return defaultValue;
            if (int.TryParse(Value, out var result))
                return result;
            return defaultValue;
        }

        /// <summary>
        ///     Attempts to read the raw value as a long integer. Returns the default value if the value is null or cannot be
        ///     parsed.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public long ToLong(long defaultValue = 0)
        {
            if (Value is null)
                return defaultValue;
            if (long.TryParse(Value, out var result))
                return result;
            return defaultValue;
        }

        /// <summary>
        ///     Attempts to read the raw value as a float. Returns the default value if the value is null or cannot be parsed.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public float ToFloat(float defaultValue = 0)
        {
            if (Value is null)
                return defaultValue;
            if (float.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return result;
            return defaultValue;
        }

        /// <summary>
        ///     Attempts to read the raw value as a double. Returns the default value if the value is null or cannot be parsed.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public double ToDouble(double defaultValue = 0)
        {
            if (Value is null)
                return defaultValue;
            if (double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return result;
            return defaultValue;
        }

        /// <summary>
        ///     Attempts to read the raw value as a decimal. Returns the default value if the value is null or cannot be parsed.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public decimal ToDecimal(decimal defaultValue = 0)
        {
            if (Value is null)
                return defaultValue;
            if (decimal.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return result;
            return defaultValue;
        }

        /// <summary>
        ///     Returns the raw value or the specified default value if the value is null.
        /// </summary>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string ToString(string defaultValue) => Value ?? defaultValue;

        /// <summary>
        ///     Returns the raw value or an empty string if the value is null.
        /// </summary>
        /// <returns></returns>
        public override string? ToString() => Value;
    }
}
