using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Mdk.DocGen3;

partial class Program
{
    public static int Main(string[] args)
    {
        var type = typeof(Program);
        var method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "Main" && m.ReturnType == typeof(void) && m.GetParameters().All(p => p.ParameterType == typeof(string) || p.ParameterType == typeof(bool)));
        if (method == null)
        {
            Console.WriteLine("No Main method found");
            return 1;
        }

        var parameters = method.GetParameters();
        var callParams = new object?[parameters.Length];
        var isRequired = new bool[parameters.Length];
        var isSet = new bool[parameters.Length];

        var switches = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var arguments = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var argMap = new Dictionary<int, int>();
        int argCount = 0;
        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            var defaultValue = parameter.HasDefaultValue ? parameter.DefaultValue : null;
            var switchAttr = parameter.GetCustomAttribute<SwitchAttribute>();
            if (switchAttr != null)
            {
                // This is a switch
                if (switchAttr.AltName != null) switches.Add(switchAttr.AltName, index);
                switches.Add(parameter.Name!, index);
                callParams[index] = defaultValue;
                isRequired[index] = false;
            }
            else
            {
                // This is an argument
                argMap.Add(argCount, index);
                arguments.Add(parameter.Name!, argCount);
                argCount++;
                callParams[index] = defaultValue;
                isRequired[index] = !parameter.HasDefaultValue;
            }
        }

        var argQueue = new Queue<string>(args);
        var argIndex = 0;
        while (argQueue.Count > 0)
        {
            int index;
            var arg = argQueue.Dequeue();
            if (arg.StartsWith('-'))
            {
                // This is a switch
                arg = arg[1..];
                if (switches.TryGetValue(arg, out index))
                {
                    // This is a switch
                    isSet[index] = true;
                    var parameter = parameters[index];
                    if (parameter.ParameterType == typeof(bool))
                    {
                        callParams[index] = true;
                        continue;
                    }

                    if (argQueue.Count == 0)
                    {
                        Console.WriteLine($"Missing value for switch {arg}");
                        Console.WriteLine(GenerateHelp(parameters, switches, arguments));
                        return -1;
                    }
                    var value = argQueue.Dequeue();
                    callParams[index] = value;
                    continue;
                }
                
                Console.WriteLine($"Unknown switch {arg}");
                Console.WriteLine(GenerateHelp(parameters, switches, arguments));
                return -1;
            }

            if (argMap.TryGetValue(argIndex, out index))
            {
                // This is an argument
                isSet[index] = true;
                callParams[index] = arg;
                argIndex++;
                continue;
            }

            Console.WriteLine($"Unknown argument {arg}");
            Console.WriteLine(GenerateHelp(parameters, switches, arguments));
            return -1;
        }

        // Was there any required arguments that were not set?
        for (var index = 0; index < parameters.Length; index++)
        {
            if (isRequired[index] && !isSet[index])
            {
                Console.WriteLine($"Missing required argument {parameters[index].Name}");
                Console.WriteLine(GenerateHelp(parameters, switches, arguments));
                return -1;
            }
        }

        try
        {
            method.Invoke(null, callParams);
            return 0;
        }
        catch (ConsoleException e)
        {
            Console.WriteLine(e.Message);
            return e.Code;
        }
    }

    static string GenerateHelp(ParameterInfo[] parameters, Dictionary<string, int> switches, Dictionary<string, int> arguments)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Usage: ");
        var exeName = Path.GetFileName(Environment.ProcessPath);
        builder.Append($"  {exeName} ");
        if (switches.Count > 0)
            builder.Append($"-{"-" + string.Join(" -", switches.Keys)} ");
        if (arguments.Count > 0)
        {
            // Optionals are in [], required are in <>
            foreach (var (name, index) in arguments)
            {
                var parameter = parameters[index];
                if (parameter.HasDefaultValue)
                    builder.Append($"[{name}] ");
                else
                    builder.Append($"<{name}> ");
            }
        }
        builder.AppendLine();
        if (arguments.Count > 0)
        {
            builder.AppendLine("Arguments:");
            foreach (var (name, index) in arguments)
            {
                var parameter = parameters[index];
                var description = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;
                var isOptional = parameter.HasDefaultValue;
                builder.Append("  ").Append(name);
                if (isOptional)
                    builder.Append(" (optional)");
                if (description != null)
                    builder.Append(": ").Append(description);
                builder.AppendLine();
            }
        }
        if (switches.Count > 0)
        {
            builder.AppendLine("Options:");
            foreach (var (name, index) in switches)
            {
                var parameter = parameters[index];
                var description = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;
                builder.Append("  -").Append(name);
                if (parameter.ParameterType != typeof(bool))
                {
                    builder.Append(" <value>");
                }
                if (description != null)
                    builder.Append(": ").Append(description);
                builder.AppendLine();
            }
        }
        return builder.ToString();
    }

    public class ConsoleException : Exception
    {
        public ConsoleException() { }

        public ConsoleException(string message, int code = -1) : base(message)
        {
            Code = code;
        }

        public ConsoleException(string message, Exception inner, int code = -1) : base(message, inner)
        {
            Code = code;
        }

        public int Code { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class SwitchAttribute(string? altName = null) : Attribute
    {
        public string? AltName { get; } = altName;
    }
}