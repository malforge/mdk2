using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Serialization;
using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.CodeSecurity;
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

    public static Documentation LoadTypeInfo(string binFolder, Whitelist whitelist)
    {
        var ar = new DefaultAssemblyResolver();
        ar.AddSearchDirectory(binFolder);
        ar.AddSearchDirectory(Path.Combine(GetFrameworkInstallRoot(), "v4.0.30319"));
        var exePath = Path.Combine(binFolder, "SpaceEngineers.exe");
        if (!File.Exists(exePath)) throw new FileNotFoundException($"SpaceEngineers.exe not found in {binFolder}");
        var assembly = AssemblyDefinition.ReadAssembly(exePath);
        var context = new Context();
        ScanAssembly(assembly, context, ar);
        List<TypeDocumentation> types = new();
        while (context.AssembliesToProcess.TryDequeue(out var assemblyToProcess))
        {
            Doc? doc = null;
            context.VisitedAssemblies.TryAdd(assemblyToProcess.FullName, 0);
            // Try to find the xml documentation file
            var xmlDocPath = Path.ChangeExtension(assemblyToProcess.MainModule.FileName, ".xml");
            if (File.Exists(xmlDocPath))
            {
                // Deserialize the XML documentation file into a Doc instance
                var deserializer = new XmlSerializer(typeof(Doc));
                using var xmlStream = new FileStream(xmlDocPath, FileMode.Open, FileAccess.Read);
                using var xmlReader = XmlReader.Create(xmlStream);
                doc = (Doc?)deserializer.Deserialize(xmlReader);
            }

            void scanType(TypeDefinition type, ParallelLoopState _)
            {
                var typeKey = Whitelist.GetTypeKey(type);
                // if (!whitelist.IsAllowed(typeKey))
                //     return;
                if (!context.VisitedKeys.TryAdd(typeKey, 0))
                    return;
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

                var docKey = Doc.GetDocKey(type);
                var typeDoc = new TypeDocumentation(type, doc?.GetDocumentation(docKey));
                types.Add(typeDoc);
                // var contentPage = new TypePage(typeKey, docKey, doc?.GetDocumentation(docKey), type, obsoleteMessage);
                // context.Pages.TryAdd(Guid.NewGuid().ToString(), contentPage);

                void addTypeAssemblyIfNecessary(TypeDefinition typeDef)
                {
                    if (context.VisitedAssemblies.TryAdd(typeDef.Module.Assembly.FullName, 0))
                        context.AssembliesToProcess.Enqueue(typeDef.Module.Assembly);
                }
                
                foreach (var field in type.Fields)
                {
                    if (!field.IsPublic && !field.IsFamily)
                        continue;
                    var fieldKey = Whitelist.GetFieldKey(field);
                    // if (!whitelist.IsAllowed(fieldKey))
                    //     continue;
                    if (!context.VisitedKeys.TryAdd(fieldKey, 0))
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
                    docKey = Doc.GetDocKey(field);
                    var fieldDoc = new FieldDocumentation(field, doc?.GetDocumentation(docKey));
                    typeDoc.Fields.Add(fieldDoc);
                    // var fieldPage = new FieldPage(fieldKey, docKey, doc?.GetDocumentation(docKey), field, obsoleteMessage);
                    // contentPage.Fields.Add(fieldPage);
                    // context.Pages.TryAdd(Guid.NewGuid().ToString(), fieldPage);

                    var fieldType = field.FieldType.Resolve();
                    if (fieldType is { IsGenericParameter: false })
                        addTypeAssemblyIfNecessary(fieldType);
                }

                foreach (var property in type.Properties)
                {
                    if (!property.GetMethod?.IsPublic == true && !property.GetMethod?.IsFamily == true)
                        continue;
                    var propertyKey = Whitelist.GetPropertyKey(property);
                    // if (!whitelist.IsAllowed(propertyKey))
                    //     continue;
                    if (!context.VisitedKeys.TryAdd(propertyKey, 0))
                        continue;
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
                    var propertyType = property.PropertyType.Resolve();
                    docKey = Doc.GetDocKey(property);
                    var propertyDoc = new PropertyDocumentation(property, doc?.GetDocumentation(docKey));
                    typeDoc.Properties.Add(propertyDoc);
                    // var propertyPage = new PropertyPage(propertyKey, docKey, doc?.GetDocumentation(docKey), property, obsoleteMessage);
                    // contentPage.Properties.Add(propertyPage);
                    // context.Pages.TryAdd(Guid.NewGuid().ToString(), propertyPage);

                    if (propertyType is { IsGenericParameter: false })
                        addTypeAssemblyIfNecessary(propertyType);
                }

                foreach (var method in type.Methods)
                {
                    if (!method.IsPublic && !method.IsFamily)
                        continue;
                    if (method.IsSpecialName && (method.Name.StartsWith("get_") || method.Name.StartsWith("set_")))
                        continue;

                    var methodKey = Whitelist.GetMethodKey(method);
                    // if (!whitelist.IsAllowed(methodKey))
                    //     continue;
                    if (!context.VisitedKeys.TryAdd(methodKey, 0))
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
                    docKey = Doc.GetDocKey(method);
                    var methodDoc = new MethodDocumentation(method, doc?.GetDocumentation(docKey));
                    typeDoc.Methods.Add(methodDoc);
                    // var methodPageId = method.GetCSharpName(CSharpNameFlags.Namespace | CSharpNameFlags.NestedParent | CSharpNameFlags.Name);
                    // // Is there an existing method with the same name?
                    // if (context.Pages.TryGetValue(methodPageId, out var existingPage) && existingPage is MethodPage methodPage)
                    // {
                    //     // If so, add this method as an overload
                    //     methodPage.Overloads.Add(method);
                    // }
                    // else
                    // {
                    //     methodPage = new MethodPage(methodPageId, methodKey, docKey, doc?.GetDocumentation(docKey), method, obsoleteMessage);
                    //     contentPage.Methods.Add(methodPage);
                    //     context.Pages.TryAdd(methodPageId, methodPage);
                    // }
                    var returnType = method.ReturnType.Resolve();
                    if (returnType is { IsGenericParameter: false })
                        addTypeAssemblyIfNecessary(returnType);

                    foreach (var parameter in method.Parameters)
                    {
                        var parameterType = parameter.ParameterType.Resolve();
                        if (parameterType is { IsGenericParameter: false })
                            addTypeAssemblyIfNecessary(parameterType);
                    }
                }

                foreach (var evt in type.Events)
                {
                    if (!evt.AddMethod?.IsPublic == true && !evt.AddMethod?.IsFamily == true)
                        continue;
                    var eventKey = Whitelist.GetEventKey(evt);
                    // if (!whitelist.IsAllowed(eventKey))
                    //     continue;
                    if (!context.VisitedKeys.TryAdd(eventKey, 0))
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
                    docKey = Doc.GetDocKey(evt);
                    var eventDoc = new EventDocumentation(evt, doc?.GetDocumentation(docKey));
                    typeDoc.Events.Add(eventDoc);
                    // var eventPage = new EventPage(eventKey, docKey, doc?.GetDocumentation(docKey), evt, obsoleteMessage);
                    // contentPage.Events.Add(eventPage);
                    // context.Pages.TryAdd(Guid.NewGuid().ToString(), eventPage);
                    var eventType = evt.EventType.Resolve();
                    if (eventType is { IsGenericParameter: false })
                        addTypeAssemblyIfNecessary(eventType);
                }
            }

            Parallel.ForEach(assemblyToProcess.Modules,
                (module, _) => { Parallel.ForEach(module.Types, scanType); });
        }

        foreach (var type in types)
        {
            if (type.Type.DeclaringType != null)
            {
                var parentType = types.FirstOrDefault(t => t.Type.FullName == type.Type.DeclaringType.FullName);
                parentType?.NestedTypes.Add(type);
            }
        }
        
        return new Documentation(types);
        // return new TypeInfo(context.Pages.Values);
    }

    static void ScanAssembly(AssemblyDefinition assembly, Context context, DefaultAssemblyResolver ar)
    {
        foreach (var module in assembly.Modules)
        {
            foreach (var assemblyName in module.AssemblyReferences)
            {
                var fullName = assemblyName.FullName;
                if (context.VisitedKeys.ContainsKey(fullName))
                    continue;
                var referencedAssembly = ar.Resolve(assemblyName);
                context.AssembliesToProcess.Enqueue(referencedAssembly);
                context.VisitedKeys.TryAdd(fullName, 0);
            }
        }
    }

    class Context
    {
        public ConcurrentQueue<AssemblyDefinition> AssembliesToProcess { get; } = new();
        public ConcurrentQueue<TypeDefinition> TypesToProcess { get; } = new();
        public ConcurrentDictionary<string, byte> VisitedAssemblies { get; } = new();
        public ConcurrentDictionary<string, byte> VisitedKeys { get; } = new();
        // public ConcurrentDictionary<string, Page> Pages { get; } = new();
    }
}