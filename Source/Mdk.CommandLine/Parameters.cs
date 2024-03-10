using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Mdk.CommandLine;

/// <summary>
///     A parser for command line parameters.
/// </summary>
public class Parameters
{
    /// <summary>
    ///     How boolean values should be formatted.
    /// </summary>
    public enum PreferredBoolStyle
    {
        TrueFalse,
        YesNo,
        OnOff,
        OneZero
    }

    Parameters(ImmutableDictionary<string, Key> keys)
    {
        Keys = keys;
    }

    /// <summary>
    ///     The keys of the parameters.
    /// </summary>
    public ImmutableDictionary<string, Key> Keys { get; }

    /// <summary>
    ///     Gets the key with the specified name.
    /// </summary>
    /// <param name="name"></param>
    public Key this[string name]
    {
        get
        {
            if (!Keys.TryGetValue(name, out var key))
                return new Key(name, null);
            return key;
        }
    }

    /// <summary>
    ///     Creates a new builder for a set of parameters.
    /// </summary>
    /// <param name="programName"></param>
    /// <param name="exeName"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public static Builder Create(string programName, string exeName, in SemanticVersion version) => new(programName, exeName, version);

    /// <summary>
    ///     A builder for a set of parameters.
    /// </summary>
    public readonly struct Builder
    {
        readonly string _programName;
        readonly string _exeName;
        readonly SemanticVersion _version;
        readonly string? _extraHelp;
        readonly PreferredBoolStyle _preferredBoolStyle;
        readonly ImmutableArray<(string name, string help, bool required)> _arguments;
        readonly ImmutableArray<(string name, string help)> _switches;
        readonly ImmutableArray<(string name, string help, string defaultValue)> _options;

        /// <summary>
        ///     Creates a new builder for a set of parameters.
        /// </summary>
        /// <param name="programName"></param>
        /// <param name="exeName"></param>
        /// <param name="version"></param>
        public Builder(string programName, string exeName, in SemanticVersion version)
            : this(programName, exeName, version, null, PreferredBoolStyle.TrueFalse, ImmutableArray<(string name, string help, bool required)>.Empty, ImmutableArray<(string name, string help)>.Empty, ImmutableArray<(string name, string help, string defaultValue)>.Empty) { }

        Builder(string programName, string exeName, in SemanticVersion version, string? extraHelp, PreferredBoolStyle preferredBoolStyle, ImmutableArray<(string name, string help, bool required)> arguments, ImmutableArray<(string name, string help)> switches, ImmutableArray<(string name, string help, string defaultValue)> options)
        {
            _programName = programName;
            _exeName = exeName;
            _version = version;
            _extraHelp = extraHelp;
            _preferredBoolStyle = preferredBoolStyle;
            _arguments = arguments;
            _switches = switches;
            _options = options;
        }

        /// <summary>
        ///     Sets the preferred style for boolean values.
        /// </summary>
        /// <param name="preferredBoolStyle"></param>
        /// <returns></returns>
        public Builder WithPreferredBoolStyle(PreferredBoolStyle preferredBoolStyle) => new(_programName, _exeName, _version, _extraHelp, preferredBoolStyle, _arguments, _switches, _options);

        /// <summary>
        ///     Sets any extra help text that will be displayed after the parameters.
        /// </summary>
        /// <param name="extraHelp"></param>
        /// <returns></returns>
        public Builder WithExtraHelp(string extraHelp) => new(_programName, _exeName, _version, extraHelp, _preferredBoolStyle, _arguments, _switches, _options);

        bool Exists(string name)
        {
            if (_arguments.Any(a => string.Equals(a.name, name, StringComparison.OrdinalIgnoreCase)))
                return true;
            if (_switches.Any(s => string.Equals(s.name, name, StringComparison.OrdinalIgnoreCase)))
                return true;
            if (_options.Any(o => string.Equals(o.name, name, StringComparison.OrdinalIgnoreCase)))
                return true;
            return false;
        }

        /// <summary>
        ///     Adds a required argument to the set of parameters.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="help"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Builder WithArgument(string name, string help)
        {
            if (Exists(name))
                throw new ArgumentException($"Duplicate name '{name}'.");
            var hasOptional = _arguments.Any(a => !a.required);
            if (hasOptional)
                throw new ArgumentException($"Argument '{name}' must be added before any optional arguments.");
            return new Builder(_programName, _exeName, _version, _extraHelp, _preferredBoolStyle, _arguments.Add((name, help, true)), _switches, _options);
        }

        /// <summary>
        ///     Adds an optional argument to the set of parameters.
        /// </summary>
        /// <remarks>
        ///     Optional arguments must be added after all required arguments.
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="help"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Builder WithOptionalArgument(string name, string help)
        {
            if (Exists(name))
                throw new ArgumentException($"Duplicate name '{name}'.");
            return new Builder(_programName, _exeName, _version, _extraHelp, _preferredBoolStyle, _arguments.Add((name, help, false)), _switches, _options);
        }

        /// <summary>
        ///     Adds a switch to the set of parameters.
        /// </summary>
        /// <remarks>
        ///     A switch is a boolean value that is true if it is present and false if it is not.
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="help"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Builder WithSwitch(string name, string help)
        {
            if (Exists(name))
                throw new ArgumentException($"Duplicate name '{name}'.");
            return new Builder(_programName, _exeName, _version, _extraHelp, _preferredBoolStyle, _arguments, _switches.Add((name, help)), _options);
        }

        /// <summary>
        ///     Adds an option to the set of parameters.
        /// </summary>
        /// <remarks>
        ///     An option is a named value that can be set to a default value if not specified.
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="help"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Builder WithOption(string name, string help, string defaultValue)
        {
            if (Exists(name))
                throw new ArgumentException($"Duplicate name '{name}'.");
            return new Builder(_programName, _exeName, _version, _extraHelp, _preferredBoolStyle, _arguments, _switches, _options.Add((name, help, defaultValue)));
        }

        /// <summary>
        ///     Adds an option to the set of parameters.
        /// </summary>
        /// <remarks>
        ///     An option is a named value that can be set to a default value if not specified.
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="help"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public Builder WithOption(string name, string help, int defaultValue)
            => WithOption(name, help, defaultValue.ToString(CultureInfo.InvariantCulture));

        /// <summary>
        ///     Adds an option to the set of parameters.
        /// </summary>
        /// <remarks>
        ///     An option is a named value that can be set to a default value if not specified.
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="help"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public Builder WithOption(string name, string help, long defaultValue)
            => WithOption(name, help, defaultValue.ToString(CultureInfo.InvariantCulture));

        /// <summary>
        ///     Adds an option to the set of parameters.
        /// </summary>
        /// <remarks>
        ///     An option is a named value that can be set to a default value if not specified.
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="help"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public Builder WithOption(string name, string help, float defaultValue)
            => WithOption(name, help, defaultValue.ToString(CultureInfo.InvariantCulture));

        /// <summary>
        ///     Adds an option to the set of parameters.
        /// </summary>
        /// <remarks>
        ///     An option is a named value that can be set to a default value if not specified.
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="help"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public Builder WithOption(string name, string help, double defaultValue)
            => WithOption(name, help, defaultValue.ToString(CultureInfo.InvariantCulture));

        /// <summary>
        ///     Adds an option to the set of parameters.
        /// </summary>
        /// <remarks>
        ///     An option is a named value that can be set to a default value if not specified.
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="help"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public Builder WithOption(string name, string help, decimal defaultValue)
            => WithOption(name, help, defaultValue.ToString(CultureInfo.InvariantCulture));

        /// <summary>
        ///     Adds an option to the set of parameters.
        /// </summary>
        /// <remarks>
        ///     An option is a named value that can be set to a default value if not specified.
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="help"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public Builder WithOption(string name, string help, bool defaultValue)
            => WithOption(name, help, ToBoolString(defaultValue));

        /// <summary>
        ///     Adds an option to the set of parameters.
        /// </summary>
        /// <remarks>
        ///     An option is a named value that can be set to a default value if not specified.
        /// </remarks>
        /// <param name="name"></param>
        /// <param name="help"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public Builder WithOption<TEnum>(string name, string help, TEnum defaultValue) where TEnum : struct, Enum
            => WithOption(name, help, defaultValue.ToString());

        string ToBoolString(bool value) =>
            _preferredBoolStyle switch
            {
                PreferredBoolStyle.TrueFalse => value ? "true" : "false",
                PreferredBoolStyle.YesNo => value ? "yes" : "no",
                PreferredBoolStyle.OnOff => value ? "on" : "off",
                PreferredBoolStyle.OneZero => value ? "1" : "0",
                _ => throw new ArgumentOutOfRangeException()
            };

        /// <summary>
        ///     Writes the help text to the console.
        /// </summary>
        public void Help() => Help(Console.Out);

        /// <summary>
        ///     Writes the help text to the specified writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Help(TextWriter writer)
        {
            writer.WriteLine($"{_programName} v{_version:P}");
            writer.WriteLine($"Usage: {_exeName} {string.Join(" ", _arguments.Select(a => a.required ? $"<{a.name}>" : $"[{a.name}]"))} [options]");
            if (_arguments.Length > 0)
            {
                writer.WriteLine();
                var maxNameLength = _arguments.Max(a => a.name.Length + 2);
                writer.WriteLine("Arguments:");
                foreach (var argument in _arguments)
                {
                    var name = (argument.required ? $"<{argument.name}>" : $"[{argument.name}]").PadRight(maxNameLength);
                    writer.WriteLine(argument.required ? $"  {name}  {argument.help}" : $"  {name}  (Optional) {argument.help}");
                }
            }
            if (_switches.Length > 0 || _options.Length > 0)
            {
                writer.WriteLine();
                const int suffixLength = 7; // Length of " <value>"
                var defaultOff = _preferredBoolStyle switch
                {
                    PreferredBoolStyle.TrueFalse => "false",
                    PreferredBoolStyle.YesNo => "no",
                    PreferredBoolStyle.OnOff => "off",
                    PreferredBoolStyle.OneZero => "0",
                    _ => throw new ArgumentOutOfRangeException()
                };
                var allOptions = _switches.Select(s => (s.name, s.help, defaultValue: defaultOff))
                    .Concat(_options.Select(o => (name: o.name + " <value>", o.help, o.defaultValue)))
                    .OrderBy(o => o.name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var maxNameLength = allOptions.Max(o => o.name.Length);
                writer.WriteLine("Options:");
                foreach (var option in allOptions)
                {
                    var name = option.name.PadRight(maxNameLength + suffixLength);
                    writer.WriteLine($"  -{name}  {option.help} (default: {option.defaultValue})");
                }
            }
            if (!string.IsNullOrWhiteSpace(_extraHelp))
            {
                writer.WriteLine();
                writer.WriteLine(_extraHelp);
            }
        }

        bool TryFindSwitch(string name, out (string name, string help) switchRef)
        {
            switchRef = default;
            foreach (var s in _switches)
            {
                if (string.Equals(s.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    switchRef = s;
                    return true;
                }
            }
            return false;
        }

        bool TryFindOption(string name, out (string name, string help, string defaultValue) optionRef)
        {
            optionRef = default;
            foreach (var o in _options)
            {
                if (string.Equals(o.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    optionRef = o;
                    return true;
                }
            }
            return false;
        }

        bool TryGetArgument(int index, out (string name, string help, bool required) argument)
        {
            if (index < 0 || index >= _arguments.Length)
            {
                argument = default;
                return false;
            }
            argument = _arguments[index];
            return true;
        }

        /// <summary>
        ///     Builds a set of parameters from the specified arguments.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Parameters Build(IReadOnlyList<string> args)
        {
            var values = ImmutableDictionary.CreateBuilder<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var argument in _arguments)
                values[argument.name] = null;
            foreach (var @switch in _switches)
                values[@switch.name] = ToBoolString(false);
            foreach (var option in _options)
                values[option.name] = option.defaultValue;

            var i = 0;
            while (i < args.Count)
            {
                var arg = args[i];
                if (arg.StartsWith('-'))
                {
                    if (TryFindSwitch(arg[1..], out var @switch))
                    {
                        values[@switch.name] = ToBoolString(true);
                        i++;
                    }
                    else if (TryFindOption(arg[1..], out var option))
                    {
                        if (i + 1 >= args.Count)
                            throw new ArgumentException($"Missing value for option '{option.name}'.");
                        values[option.name] = args[i + 1];
                        i += 2;
                    }
                    else
                        throw new ArgumentException($"Unknown option '{arg}'.");
                }
                else
                {
                    if (!TryGetArgument(i, out var argument))
                        throw new ArgumentException("Too many arguments.");
                    values[argument.name] = args[i];
                    i++;
                }
            }

            foreach (var argument in _arguments)
            {
                if (argument.required && values[argument.name] == null)
                    throw new ArgumentException($"Missing required argument '{argument.name}'.");
            }

            return new Parameters(values.ToImmutableDictionary(v => v.Key, v => new Key(v.Key, v.Value)));
        }
    }

    /// <summary>
    ///     A key in a set of parameters.
    /// </summary>
    public readonly struct Key
    {
        /// <summary>
        ///     An empty key.
        /// </summary>
        public static readonly Key Empty = new(string.Empty, null);

        /// <summary>
        ///     Creates a new key with the specified name and value.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        public Key(string name, string? value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            Name = name;
            Value = value;
        }

        /// <summary>
        ///     The name of the key.
        /// </summary>
        public readonly string Name;

        /// <summary>
        ///     The raw value of the key.
        /// </summary>
        public readonly string? Value;

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
        public override string ToString() => Value ?? string.Empty;
    }
}