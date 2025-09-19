using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.Support;
using Microsoft.Win32;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class TypeLoadingContext(string binFolder, Func<TypeDefinition, bool> typeFilterFn) : ContextBase
{
    readonly Func<TypeDefinition, bool> _typeFilterFn = typeFilterFn;
    
    public string BinFolder { get; } = binFolder ?? throw new ArgumentNullException(nameof(binFolder), "Bin folder cannot be null.");

    public void Print(string text) => Console.WriteLine(text);
    
    public bool PassesFilter(TypeDefinition type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type), "Type cannot be null.");
        return _typeFilterFn(type);
    }
}

public class TypeLoader
{
    static string GetFrameworkInstallRoot()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("This method is only supported on Windows.");

        using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
            .OpenSubKey(@"SOFTWARE\Microsoft\.NETFramework");
        var value = key?.GetValue("InstallRoot") as string;
        if (value == null)
            throw new InvalidOperationException("InstallRoot not found in registry");
        return value;
    }

    static IEnumerable<AssemblyDefinition> FindAssemblies(string binFolder, IAssemblyResolver ar)
    {
        var dllFiles = Directory.GetFiles(binFolder, "*.dll", SearchOption.TopDirectoryOnly);
        var readerParams = new ReaderParameters
        {
            AssemblyResolver = ar,
            InMemory = true
        };

        foreach (var dllFile in dllFiles)
        {
            AssemblyDefinition? assembly;
            try
            {
                assembly = AssemblyDefinition.ReadAssembly(dllFile, readerParams);
            }
            catch (BadImageFormatException)
            {
                // Ignore assemblies that cannot be resolved
                continue;
            }
            if (assembly != null)
                yield return assembly;
        }
    }

    public static Documentation LoadTypeInfo(TypeLoadingContext context)
    {
        var ar = new DefaultAssemblyResolver();
        ar.AddSearchDirectory(context.BinFolder);
        ar.AddSearchDirectory(Path.Combine(GetFrameworkInstallRoot(), "v4.0.30319"));

        context.Print($"Loading assemblies from {context.BinFolder}");
        var assemblies = FindAssemblies(context.BinFolder, ar).ToList();
        if (assemblies.Count == 0)
            throw new InvalidOperationException($"No assemblies found in {context.BinFolder}");

        // Remove generated serialization assemblies
        assemblies.RemoveAll(a => a.Name.Name.StartsWith("Microsoft.Xml.Serialization.GeneratedAssembly", StringComparison.OrdinalIgnoreCase));

        var assemblyDocs = new Dictionary<AssemblyDefinition, Doc>(assemblies.Count);
        foreach (var assembly in assemblies)
        {
            var xmlDocPath = Path.ChangeExtension(assembly.MainModule.FileName, ".xml");
            if (!File.Exists(xmlDocPath)) continue;
            var deserializer = new XmlSerializer(typeof(Doc));
            using var xmlStream = new FileStream(xmlDocPath, FileMode.Open, FileAccess.Read);
            using var xmlReader = XmlReader.Create(xmlStream);
            var doc = (Doc?)deserializer.Deserialize(xmlReader);
            if (doc != null)
                assemblyDocs[assembly] = doc;
        }

#if DEBUG

        // Are there any duplicate assemblies?
        var duplicateAssemblies = assemblies
            .GroupBy(a => a.Name.Name)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateAssemblies.Any())
        {
            var duplicatesStr = string.Join(", ", duplicateAssemblies.Select(g => $"{g.Key} ({g.Count()} duplicates)"));
            Debug.Assert(false, $"Duplicate assemblies found: {duplicatesStr}. Assemblies must be unique.");
        }

#endif

        var types = assemblies.SelectMany(assembly => assembly.MainModule.Types.Where(t => t.IsPublic || (t.IsNestedPublic && !t.IsMsType()))).ToList();
        context.BeginProgress("Process types for documentation", types.Count);

#if DEBUG

        // Check for duplicate types
        var duplicates = types
            .GroupBy(t => t.GetFullyQualifiedName())
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Any())
        {
            var duplicatesStr = string.Join(", ", duplicates.Select(g => $"{g.Key} ({g.Count()} duplicates)"));
            Debug.Assert(false, $"Duplicate types found: {duplicatesStr}. Types must be unique.");
        }

#endif

        var namespaces = new Dictionary<string, NamespaceDocumentation.Builder>(StringComparer.OrdinalIgnoreCase);
        var visitedTypes = new HashSet<string>(types.Select(t => t.GetFullyQualifiedName()), StringComparer.OrdinalIgnoreCase);
        var typeDocs = new List<TypeDocumentation.Builder>(types.Count);
        // Stage 1: Collect all types
        for (var index = 0; index < types.Count; index++)
        {
            var type = types[index];
            
            if (!context.PassesFilter(type))
            {
                context.Progress();
                continue; // Skip types that do not pass the filter
            }
            
            string? obsoleteMessage = null;
            if (type.HasCustomAttributes)
            {
                foreach (var attribute in type.CustomAttributes)
                {
                    if (attribute.AttributeType.FullName == "System.ObsoleteAttribute")
                    {
                        if (attribute.ConstructorArguments.Count > 0)
                            obsoleteMessage = attribute.ConstructorArguments[0].Value as string;
                        break;
                    }
                }
            }
            visitedTypes.Add(type.GetFullyQualifiedName());
            var baseType = type.BaseType?.Resolve();
            if (baseType != null && visitedTypes.Add(baseType.GetFullyQualifiedName()))
                types.Add(baseType);
            if (!namespaces.TryGetValue(type.Namespace, out var ns))
            {
                ns = new NamespaceDocumentation.Builder(type.Namespace, type.Module.Assembly.Name.Name);
                namespaces[type.Namespace] = ns;
            }
            var typeDoc = new TypeDocumentation.Builder(ns, type, obsoleteMessage);
            typeDocs.Add(typeDoc);
            context.Progress();
        }

        context.EndProgress();

#if DEBUG

        var duplicates2 = typeDocs
            .GroupBy(t => t.FullyQualifiedName)
            .Where(g => g.Count() > 1)
            .ToList();
        if (duplicates2.Any())
            Debug.Assert(false, $"Duplicate types found: {string.Join(", ", duplicates2.Select(d => d.Key))}. Types must be unique.");

#endif

        // Stage 2: Resolve base types and add documentation
        context.BeginProgress("Resolve members", typeDocs.Count);

#if DEBUG
        var processedTypes = new HashSet<TypeDefinition>();
#endif

        foreach (var typeDoc in typeDocs)
        {
#if DEBUG
            if (!processedTypes.Add(typeDoc.Type))
                Debug.Assert(false, $"Type {typeDoc.Type.FullName} has already been processed. This should not happen.");
#endif

            var baseType = typeDoc.Type.BaseType?.Resolve();
            if (baseType != null && visitedTypes.Contains(baseType.GetFullyQualifiedName()))
                typeDoc.WithBaseType(typeDocs.FirstOrDefault(t => t.FullyQualifiedName == baseType.GetFullyQualifiedName()));

            if (namespaces.TryGetValue(typeDoc.Type.Namespace, out var nsDoc))
                nsDoc.WithAdditionalType(typeDoc.Type);

            var interfaces = typeDoc.Type.Interfaces;
            foreach (var iface in interfaces)
            {
                var ifaceType = iface.InterfaceType.Resolve();
                if (ifaceType != null && visitedTypes.Contains(ifaceType.GetFullyQualifiedName()))
                {
                    var ifaceDoc = typeDocs.FirstOrDefault(t => t.Type.FullName == ifaceType.FullName);
                    typeDoc.WithInterface(ifaceDoc ?? TypeDocumentation.Builder.ForExternalType(ifaceType));
                }
            }

            assemblyDocs.TryGetValue(typeDoc.Type.Module.Assembly, out var doc);

            var type = typeDoc.Type;
            string? obsoleteMessage;

            foreach (var field in type.Fields)
            {
                if (field is { IsPublic: false, IsFamily: false, IsFamilyOrAssembly: false })
                    continue;

                obsoleteMessage = null;
                if (field.HasCustomAttributes)
                {
                    foreach (var attribute in field.CustomAttributes)
                    {
                        if (attribute.AttributeType.FullName == "System.ObsoleteAttribute")
                        {
                            if (attribute.ConstructorArguments.Count > 0)
                                obsoleteMessage = attribute.ConstructorArguments[0].Value as string;
                            break;
                        }
                    }
                }
                var docKey = Doc.GetDocKey(field);
                var fieldDoc = new FieldDocumentation(typeDoc.Instance, field, doc?.GetDocumentation(docKey), obsoleteMessage);
                typeDoc.WithAdditionalMember(fieldDoc);
                // TODO: Link to MS documentation where possible
            }

            foreach (var property in type.Properties)
            {
                if (property.GetMethod is null or { IsPublic: false, IsFamily: false, IsFamilyOrAssembly: false } && property.SetMethod is null or { IsPublic: false, IsFamily: false, IsFamilyOrAssembly: false })
                    continue;

                if (property.Name.IndexOf('.') >= 0) Debugger.Break();

                obsoleteMessage = null;
                if (property.HasCustomAttributes)
                {
                    foreach (var attribute in property.CustomAttributes)
                    {
                        if (attribute.AttributeType.FullName == "System.ObsoleteAttribute")
                        {
                            if (attribute.ConstructorArguments.Count > 0)
                                obsoleteMessage = attribute.ConstructorArguments[0].Value as string;
                            break;
                        }
                    }
                }
                var docKey = Doc.GetDocKey(property);
                var propertyDoc = new PropertyDocumentation(typeDoc.Instance, property, doc?.GetDocumentation(docKey), obsoleteMessage);
                typeDoc.WithAdditionalMember(propertyDoc);
                // TODO: Link to MS documentation where possible
            }

            foreach (var method in type.Methods)
            {
                if (method is { IsPublic: false, IsFamily: false, IsFamilyOrAssembly: false })
                    continue;
                if (method.IsSpecialName /* && (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))*/)
                    continue;

                obsoleteMessage = null;
                if (method.HasCustomAttributes)
                {
                    foreach (var attribute in method.CustomAttributes)
                    {
                        if (attribute.AttributeType.FullName == "System.ObsoleteAttribute")
                        {
                            if (attribute.ConstructorArguments.Count > 0)
                                obsoleteMessage = attribute.ConstructorArguments[0].Value as string;
                            break;
                        }
                    }
                }
                var docKey = Doc.GetDocKey(method);
                var methodDoc = new MethodDocumentation(typeDoc.Instance, method, doc?.GetDocumentation(docKey), obsoleteMessage);
                typeDoc.WithAdditionalMember(methodDoc);
                // TODO: Link to MS documentation where possible
            }

            foreach (var evt in type.Events)
            {
                if (!evt.AddMethod?.IsPublic == true && !evt.AddMethod?.IsFamily == true)
                    continue;
                obsoleteMessage = null;
                if (evt.HasCustomAttributes)
                {
                    foreach (var attribute in evt.CustomAttributes)
                    {
                        if (attribute.AttributeType.FullName == "System.ObsoleteAttribute")
                        {
                            if (attribute.ConstructorArguments.Count > 0)
                                obsoleteMessage = attribute.ConstructorArguments[0].Value as string;
                            break;
                        }
                    }
                }
                var docKey = Doc.GetDocKey(evt);
                var eventDoc = new EventDocumentation(typeDoc.Instance, evt, doc?.GetDocumentation(docKey), obsoleteMessage);
                typeDoc.WithAdditionalMember(eventDoc);
                // TODO: Link to MS documentation where possible
            }
            context.Progress();
        }

        return new Documentation(typeDocs.Select(t => t.Build()).ToList());
    }
}