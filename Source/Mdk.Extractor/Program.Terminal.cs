// Mdk.Extractor
// 
// Copyright 2023 Morten A. Lyrstad

using System;
using System.Collections.Generic;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable UnusedType.Global
// ReSharper disable InvertIf
// ReSharper disable InvocationIsSkipped
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable HeuristicUnreachableCode

namespace Mdk.Extractor;

/// <summary>
///     An error happened during the execution of a terminal application.
/// </summary>
public class TerminalException : Exception
{
    /// <summary>
    ///     Creates a new <see cref="TerminalException" /> with the given error message.
    /// </summary>
    /// <param name="message"></param>
    public TerminalException(string message) : base(message)
    { }

    /// <summary>
    ///     Creates a new <see cref="TerminalException" /> with the given error message, passing on an originating exception
    ///     that caused the throwing of this one.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="inner"></param>
    public TerminalException(string message, Exception inner) : base(message, inner)
    { }

    /// <summary>
    ///     A predefined exception happening when the application requires the specification of a verb, but did not receive
    ///     one.
    /// </summary>
    public class MissingVerb : TerminalException
    {
        /// <summary>
        ///     Creates a new <see cref="TerminalException.MissingVerb" /> exception.
        /// </summary>
        public MissingVerb() : base("No verb was given")
        { }
    }

    /// <summary>
    ///     A predefined exception happening when the application receives a verb it does not understand.
    /// </summary>
    public class UnknownVerb : TerminalException
    {
        /// <summary>
        ///     Creates a new <see cref="TerminalException.UnknownVerb" /> exception.
        /// </summary>
        /// <param name="verbName"></param>
        public UnknownVerb(string verbName) : base($"The verb '{verbName}' was not recognized")
        {
            VerbName = verbName;
        }

        /// <summary>
        ///     The name of the verb that was not understood.
        /// </summary>
        public string VerbName { get; }
    }

    /// <summary>
    ///     A predefined exception happening when the application finds a switch it cannot resolve.
    /// </summary>
    public class UnknownSwitch : TerminalException
    {
        /// <summary>
        ///     Creates a new <see cref="TerminalException.UnknownSwitch" /> exception.
        /// </summary>
        /// <param name="switchName"></param>
        public UnknownSwitch(string switchName) : base($"The switch '{switchName}' was not recognized")
        {
            SwitchName = switchName;
        }

        /// <summary>
        ///     The name of the switch that was not understood.
        /// </summary>
        public string SwitchName { get; }
    }

    /// <summary>
    ///     A predefined exception happening when a provided switch requires a valid value, but did not receive one.
    /// </summary>
    public class MissingSwitchValue : TerminalException
    {
        /// <summary>
        ///     Creates a new <see cref="TerminalException.MissingSwitchValue" /> exception.
        /// </summary>
        /// <param name="switchName"></param>
        public MissingSwitchValue(string switchName) : base($"The switch '{switchName}' is missing its value")
        {
            SwitchName = switchName;
        }

        /// <summary>
        ///     The name of the switch that did not receive a value.
        /// </summary>
        public string SwitchName { get; }
    }

    /// <summary>
    ///     A predefined exception happening when the value of a provided switch did not match the expected values or value
    ///     types.
    /// </summary>
    public class BadSwitchValue : TerminalException
    {
        /// <summary>
        ///     Creates a new <see cref="TerminalException.BadSwitchValue" /> exception.
        /// </summary>
        /// <param name="switchName"></param>
        /// <param name="switchValue"></param>
        public BadSwitchValue(string switchName, string switchValue) : base($"The switch '{switchName}' has a bad value '{switchValue}'")
        {
            SwitchName = switchName;
            SwitchValue = switchValue;
        }

        /// <summary>
        ///     The name of the switch that received a bad value.
        /// </summary>
        public string SwitchName { get; }

        public string SwitchValue { get; }
    }
}

partial class Program
{
    /// <summary>
    ///     This is the primary entry point of the terminal application. It will analyze the provided arguments and route
    ///     the actions accordingly.
    /// </summary>
    /// <param name="args"></param>
    /// <returns>0, or an error code</returns>
    public static int Main(string[] args)
    {
        try
        {
            var switches = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var valueSwitches = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "-modwhitelist",
                "-pbwhitelist",
                "-terminal",
                "-sepath"
            };
            // var simpleSwitches = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            // {
            //     "-pb",
            //     "-mods"
            // };
            var parameters = new Queue<string>();
            for (int index = 0, n = args.Length; index < n; index++)
            {
                var arg = args[index];
                if (valueSwitches.Contains(arg))
                {
                    index++;
                    if (index >= n)
                        throw new TerminalException.MissingSwitchValue(arg);
                    var value = args[index];
                    switches[arg] = value;
                    continue;
                }

                // if (simpleSwitches.Contains(arg))
                // {
                //     index++;
                //     if (index >= 0)
                //         throw new TerminalException.MissingSwitchValue(arg);
                //     switches[arg] = "true";
                //     continue;
                // }

                if (arg.StartsWith("-"))
                    throw new TerminalException.UnknownSwitch(arg);
                
                parameters.Enqueue(arg);
            }

            var verb = parameters.Count > 0 ? parameters.Dequeue() : "help";
            switches.TryGetValue("-modwhitelist", out var modWhitelist);
            switches.TryGetValue("-pbwhitelist", out var pbWhitelist);
            switches.TryGetValue("-terminal", out var terminal);
            switches.TryGetValue("-sepath", out var sepath);

            Main(modWhitelist, pbWhitelist, terminal, sepath);
            return 0;
            //
            // if (string.Equals(verb, "help", StringComparison.OrdinalIgnoreCase))
            // {
            //     var what = parameters.Count > 0 ? parameters.Dequeue() : null;
            //     Help(what);
            //     return 0;
            // }
            //
            // throw new TerminalException.UnknownVerb(verb);
        }
        catch (TerminalException e)
        {
            return Error(e);
        }
    }

    public static int Error(Exception e)
    {
        Console.WriteLine(e.Message);
        Console.WriteLine(@"----------");
        Help();
        return -1;
    }

    public static void Help(string verb = null)
    {
        Console.WriteLine(@"Usage:");
        if (verb == null || string.Equals(verb, "whitelist", StringComparison.OrdinalIgnoreCase))
            Console.WriteLine(@"mdkx whitelist [cacheFileName] [-sepath pathtobin64] [-target pb|mods]");
    }
}