using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Mal.DocumentGenerator.Common;

public abstract class ConfigObject
{
    static readonly Dictionary<Type, IConfigObjectDefinition> Definitions = new();
    readonly IConfigObjectDefinition _definition;

    protected ConfigObject()
    {
        _definition = GetDefinition(GetType());
        foreach (var property in _definition.Values)
        {
            if (property.DefaultValue is null)
                continue;
            property.SetValue(this, property.DefaultValue);
        }
    }

    public void LoadFromArgs(string[] args)
    {
        var appliedProperties = new HashSet<IConfigPropertyDefinition>();
        var argumentIndex = 0;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith('-') || arg.StartsWith('/'))
            {
                var key = arg.Substring(1);
                var property = _definition.FirstOrDefault(p => string.Equals(p.Value.Name, key, StringComparison.OrdinalIgnoreCase) || string.Equals(p.Value.Shorthand, key, StringComparison.OrdinalIgnoreCase)).Value;
                if (property is null)
                    continue;

                // If property type is bool, we don't need to get the value from the next argument.
                if (property.PropertyType == typeof(bool))
                {
                    property.SetValue(this, true);
                    if (!appliedProperties.Add(property))
                        throw new CommandLineException($"Duplicate argument {arg}");
                    continue;
                }

                if (i + 1 >= args.Length)
                    throw new CommandLineException($"Missing value for argument {arg}");

                if (!appliedProperties.Add(property))
                    throw new CommandLineException($"Duplicate argument {arg}");
                property.SetImage(this, args[++i]);
            }
            else
            {
                var property = _definition.FirstOrDefault(p => p.Value.ArgumentPosition == argumentIndex).Value;
                if (property is null)
                    throw new CommandLineException($"Unexpected argument {arg}");
                if (!appliedProperties.Add(property))
                    throw new CommandLineException($"Duplicate argument {arg}");
                property.SetImage(this, arg);
                argumentIndex++;
            }
        }

        foreach (var property in _definition.Values)
        {
            if (property.IsRequired && !appliedProperties.Contains(property))
                throw new CommandLineException($"Missing required argument {property.Name}");
        }
    }

    public void LoadFromIni(string path)
    {
        var ini = Ini.FromFile(path);
        foreach (var property in _definition.Values)
        {
            var key = ini[property.Category][property.Name];
            if (key.IsEmpty())
                continue;

            property.SetImage(this, key.Value);
        }
    }

    public static IConfigObjectDefinition GetDefinition(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return GetDefinition(obj.GetType() ?? throw new ArgumentNullException(nameof(obj)));
    }

    public static IConfigObjectDefinition GetDefinition<T>() => GetDefinition(typeof(T));

    public static IConfigObjectDefinition GetDefinition(Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (!Definitions.TryGetValue(type, out var definition))
        {
            if (!type.IsClass || type.IsAbstract)
                throw new ArgumentException("Type must be a concrete class.", nameof(type));

            definition = new ReflectionConfigObjectDefinition(type);
            Definitions[type] = definition;
        }
        return definition;
    }

    static IConfigPropertyDefinition GetPropertyDefinition(PropertyInfo propertyInfo, string defaultCategory)
    {
        var type = typeof(ConfigPropertyDefinition<>).MakeGenericType(propertyInfo.PropertyType);
        return (IConfigPropertyDefinition)Activator.CreateInstance(type, propertyInfo, defaultCategory)!;
    }

    class ReflectionConfigObjectDefinition : IConfigObjectDefinition
    {
        readonly Dictionary<string, IConfigPropertyDefinition> _properties;

        public ReflectionConfigObjectDefinition(Type type)
        {
            ObjectType = type;
            var defaultCategory = type.GetCustomAttribute<CategoryAttribute>()?.Name ?? type.Name;
            _properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p is { CanRead: true, CanWrite: true }
                            && p.GetCustomAttribute<IgnoreAttribute>() == null)
                .ToDictionary(p => p.Name, p => GetPropertyDefinition(p, defaultCategory));
        }

        public IConfigPropertyDefinition this[string key]
        {
            get
            {
                if (_properties.TryGetValue(key, out var value))
                    return value;
                return NullPropertyDefinition.Instance;
            }
        }

        public IEnumerable<string> Keys => _properties.Keys;
        public IEnumerable<IConfigPropertyDefinition> Values => _properties.Values;
        public int Count => _properties.Count;

        public bool ContainsKey(string key) => _properties.ContainsKey(key);
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out IConfigPropertyDefinition value) => _properties.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<string, IConfigPropertyDefinition>> GetEnumerator() => _properties.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public Type ObjectType { get; }

        public void WriteCommandLineUsage(StringBuilder builder)
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null)
                throw new InvalidOperationException("Entry assembly not found.");
            var arguments = Values.Where(p => p.ArgumentPosition.HasValue).OrderBy(p => p.ArgumentPosition).ToList();
            var options = Values.Where(p => !p.ArgumentPosition.HasValue).OrderBy(p => p.Name).ToList();
            var displayName = entryAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
            var version = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (version != null)
            {
                var plusIndex = version.IndexOf('+');
                if (plusIndex != -1)
                    version = version.Substring(0, plusIndex);
            }

            // Write the title line
            var titleLine = $"{displayName} v{version}";
            var underline = new string('=', titleLine.Length);
            builder.AppendLine(titleLine).AppendLine(underline).AppendLine();

            var exeName = Path.GetFileNameWithoutExtension(entryAssembly.Location);
            builder.AppendLine("Usage:");

            string decoratedName(IConfigPropertyDefinition prop)
            {
                if (prop.IsRequired)
                    return $"<{prop.Name}>";
                return $"[{prop.Name}]";
            }

            builder.AppendLine($"  {exeName} [options] {string.Join(" ", arguments.Select(decoratedName))}");
            builder.AppendLine();
            builder.AppendLine("Arguments:");
            foreach (var argument in arguments)
            {
                builder.AppendLine($"  {argument.Name}: {argument.Description}");
                if (argument.DefaultImage != null)
                    builder.AppendLine($"    Default: {argument.DefaultImage}");
            }
            builder.AppendLine();
            builder.AppendLine("Options:");
            foreach (var option in options)
            {
                builder.AppendLine($"  -{option.Name}{(option.Shorthand != null ? $", -{option.Shorthand}" : "")}: {option.Description}");
                if (option.DefaultImage != null)
                    builder.AppendLine($"    Default: {option.DefaultImage}");
            }
        }

        public void WriteCommandLineUsage(TextWriter writer)
        {
            var builder = new StringBuilder();
            WriteCommandLineUsage(builder);
            writer.Write(builder.ToString());
        }
    }


    class ConfigPropertyDefinition<T> : IConfigPropertyDefinition<T>
    {
        readonly TypeConverter _converter;
        readonly PropertyInfo _propertyInfo;

        public ConfigPropertyDefinition(PropertyInfo propertyInfo, string defaultCategory)
        {
            _propertyInfo = propertyInfo;
            Category = (propertyInfo.GetCustomAttribute<CategoryAttribute>()?.Name ?? defaultCategory).ToLowerInvariant();
            Name = propertyInfo.Name.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(propertyInfo));
            _converter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
            DefaultValue = propertyInfo.GetCustomAttribute<DefaultValueAttribute>()?.Value ?? (propertyInfo.PropertyType.IsValueType ? Activator.CreateInstance(propertyInfo.PropertyType) : null);
            DefaultImage = DefaultValue is null ? null : _converter.ConvertToString(DefaultValue);
            Shorthand = propertyInfo.GetCustomAttribute<ShorthandAttribute>()?.Name.ToLowerInvariant();
            Description = propertyInfo.GetCustomAttribute<DescriptionAttribute>()?.Description;

            var argumentAttribute = propertyInfo.GetCustomAttribute<ArgumentAttribute>();
            if (argumentAttribute != null)
            {
                ArgumentPosition = argumentAttribute.Position;
                if (argumentAttribute.Required)
                    IsRequired = true;
            }
        }

        public Type PropertyType => _propertyInfo.PropertyType;

        public string? Shorthand { get; }
        public int? ArgumentPosition { get; }
        public bool IsRequired { get; }
        public string? DefaultImage { get; }
        public object? DefaultValue { get; }
        public string? Description { get; }

        public bool Exists => true;
        public string Category { get; }
        public string Name { get; }

        public string? GetImage(object instance) => _converter.ConvertToString(_propertyInfo.GetValue(instance));
        public void SetImage(object instance, string? value) => _propertyInfo.SetValue(instance, value is null ? null : _converter.ConvertFromString(value));

        object? IConfigPropertyDefinition.GetValue(object instance) => _propertyInfo.GetValue(instance);
        void IConfigPropertyDefinition.SetValue(object instance, object? value) => _propertyInfo.SetValue(instance, value);

        public T GetValue(object instance) => (T)_propertyInfo.GetValue(instance)!;
        public void SetValue(object instance, T value) => _propertyInfo.SetValue(instance, value);
    }
}