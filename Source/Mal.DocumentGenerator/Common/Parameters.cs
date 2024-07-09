using System.ComponentModel;

namespace Mal.DocumentGenerator.Common;

[Category("MDoc")]
public class Parameters : ConfigObject
{
    [Description("The path to a whitelist file for the programmable block API.")]
    [DefaultValue("pbwhitelist.dat")]
    public string? PbWhitelist { get; set; }

    [Description("The path to a whitelist file for the mod API.")]
    [DefaultValue("modwhitelist.dat")]
    public string? ModWhitelist { get; set; }

    [Description("The path to the terminal action and property file.")]
    [DefaultValue("terminal.dat")]
    public string? Terminal { get; set; }

    [Description("Where to output the documentation.")]
    [DefaultValue("Docs")]
    public string? Output { get; set; }

    [Description("The path to an .ini file containing the documentation configuration.")]
    [Argument(0)]
    public string? Path { get; set; }

    [Shorthand("?")]
    [Description("Show this help message.")]
    public bool Help { get; set; }
}