using System.Collections.Concurrent;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.Support;
using Microsoft.Win32;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

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

    public static Documentation LoadTypeInfo(string binFolder)
    {
        var ar = new DefaultAssemblyResolver();
        ar.AddSearchDirectory(binFolder);
        ar.AddSearchDirectory(Path.Combine(GetFrameworkInstallRoot(), "v4.0.30319"));

        var assemblies = FindAssemblies(binFolder, ar).ToList();
        // Stage 0: Collect documentation for all assemblies
        if (assemblies.Count == 0)
            throw new InvalidOperationException($"No assemblies found in {binFolder}");
        var assemblyDocs = new Dictionary<AssemblyDefinition, Doc>(assemblies.Count);
        foreach (var assembly in assemblies)
        {
            var xmlDocPath = Path.ChangeExtension(assembly.MainModule.FileName, ".xml");
            if (!File.Exists(xmlDocPath)) continue;
            var deserializer = new XmlSerializer(typeof(Doc));
            using var xmlStream = new FileStream(xmlDocPath, FileMode.Open, FileAccess.Read);
            using var xmlReader = XmlReader.Create(xmlStream);
            var doc = (Doc?) deserializer.Deserialize(xmlReader);
            if (doc != null)
                assemblyDocs[assembly] = doc;
        }

        int progress = 0, max;
        var stopwatch = Stopwatch.StartNew();

        var types = assemblies.SelectMany(assembly => assembly.MainModule.Types.Where(t => t.IsPublic || (t.IsNestedPublic && !t.IsMsType()))).ToList();
        max = types.Count;

        void progressed(string step)
        {
            progress++;
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                Console.WriteLine($"{step}: {progress}/{max} ({(double) progress / max:P2})");
                stopwatch.Restart();
            }
        }

        Dictionary<string, NamespaceDocumentation.Builder> namespaces = new Dictionary<string, NamespaceDocumentation.Builder>(StringComparer.OrdinalIgnoreCase);
        var visitedTypes = new HashSet<TypeDefinition>(types);
        var typeDocs = new List<TypeDocumentation.Builder>(types.Count);
        // Stage 1: Collect all types
        for (var index = 0; index < types.Count; index++)
        {
            var type = types[index];
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
            visitedTypes.Add(type);
            var baseType = type.BaseType?.Resolve();
            if (baseType != null && visitedTypes.Add(baseType))
                types.Add(baseType);
            if (!namespaces.TryGetValue(type.Namespace, out var ns))
            {
                ns = new NamespaceDocumentation.Builder(type.Namespace, type.Module.Assembly.Name.Name);
                namespaces[type.Namespace] = ns;
            }
            var typeDoc = new TypeDocumentation.Builder(ns, type, obsoleteMessage);
            typeDocs.Add(typeDoc);
            progressed("Collecting types");
        }
        // Stage 2: Resolve base types and add documentation
        progress = 0;
        max = typeDocs.Count;
        foreach (var typeDoc in typeDocs)
        {
            var baseType = typeDoc.Type.BaseType?.Resolve();
            if (baseType != null && visitedTypes.Contains(baseType))
                typeDoc.WithBaseType(typeDocs.FirstOrDefault(t => t.Type.FullName == baseType.FullName));
            
            if (namespaces.TryGetValue(typeDoc.Type.Namespace, out var nsDoc))
                nsDoc.WithAdditionalType(typeDoc.Type);
            
            var interfaces = typeDoc.Type.Interfaces;
            foreach (var iface in interfaces)
            {
                var ifaceType = iface.InterfaceType.Resolve();
                if (ifaceType != null && visitedTypes.Contains(ifaceType))
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
                if (field is {IsPublic: false, IsFamily: false, IsFamilyOrAssembly: false})
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
                if (property.GetMethod is null or {IsPublic: false, IsFamily: false, IsFamilyOrAssembly: false} && property.SetMethod is null or {IsPublic: false, IsFamily: false, IsFamilyOrAssembly: false})
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
                if (method is {IsPublic: false, IsFamily: false, IsFamilyOrAssembly: false})
                    continue;
                if (method.IsSpecialName && (method.Name.StartsWith("get_") || method.Name.StartsWith("set_")))
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

            progressed("Producing documentation");
        }

        return new Documentation(typeDocs.Select(t => t.Build()).ToList());
    }
}