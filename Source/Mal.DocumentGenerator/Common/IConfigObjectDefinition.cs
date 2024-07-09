using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mal.DocumentGenerator.Common;

public interface IConfigObjectDefinition : IReadOnlyDictionary<string, IConfigPropertyDefinition>
{
    /// <summary>
    ///     Gets the type of the object this definition is for.
    /// </summary>
    Type ObjectType { get; }

    void WriteCommandLineUsage(StringBuilder builder);

    void WriteCommandLineUsage(TextWriter writer);
}