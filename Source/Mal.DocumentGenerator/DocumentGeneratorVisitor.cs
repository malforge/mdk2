using System.Collections.Generic;
using Mal.DocumentGenerator.Whitelists;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class DocumentGeneratorVisitor(Whitelist whitelist, Context context, AssemblyDefinition assembly) : AssemblyVisitor
{
    readonly AssemblyDefinition _assembly = assembly;
    readonly Context _context = context;
    readonly Stack<Node> _nodeStack = new();
    readonly HashSet<AssemblyDefinition> _visitedAssemblies = new();
    readonly Whitelist _whitelist = whitelist;

    public void Visit()
    {
        var assemblyNode = _context.GetOrAddAssembly(assembly);
        _nodeStack.Push(assemblyNode);
        VisitModule(assembly.MainModule);
        _nodeStack.Pop();
    }

    protected override void VisitAssembly(AssemblyDefinition assembly)
    {
        if (!_visitedAssemblies.Add(assembly))
            return;
        if (_whitelist.IsAssemblyWhitelisted(assembly.Name.Name) == false)
            return;
        var assemblyNode = _context.GetOrAddAssembly(assembly);
        _nodeStack.Push(assemblyNode);
        base.VisitAssembly(assembly);
        _nodeStack.Pop();
    }

    protected override void VisitModule(ModuleDefinition module)
    {
        base.VisitModule(module);
        foreach (var assemblyNameReference in module.AssemblyReferences)
        {
            try
            {
                var assembly = module.AssemblyResolver.Resolve(assemblyNameReference);
                if (assembly != null)
                    VisitAssembly(assembly);
            }
            catch (AssemblyResolutionException)
            {
                // Ignore
            }
        }
    }

    protected override void VisitType(TypeDefinition type)
    {
        if (type.IsSpecialName || type.IsRuntimeSpecialName)
            return;
        if (!type.IsAccessible())
            return;
        if (!_whitelist.IsWhitelisted(type.Module.Assembly.Name.Name, type.ToWhitespaceFullName()))
            return;

        bool shouldPopAgain = false;
        if (!_nodeStack.TryPeek(out Node? parentNode) || parentNode is not ITypeContainer parent)
        {
            var namespaceName = type.Namespace;
            var assemblyNode = (AssemblyNode)_nodeStack.Peek();
            var namespaceNode = assemblyNode.GetOrAddNamespace(namespaceName);
            _nodeStack.Push(namespaceNode);
            parent = namespaceNode;
            shouldPopAgain = true;
        }
        var typeNode = parent.GetOrAddType(type);
        _nodeStack.Push(typeNode);
        base.VisitType(type);
        _nodeStack.Pop();
        if (shouldPopAgain)
            _nodeStack.Pop();
    }

    protected override void VisitConstructor(MethodDefinition constructor)
    {
        if (constructor.IsSpecialName || constructor.IsRuntimeSpecialName)
            return;
        if (!constructor.IsAccessible())
            return;
        if (!_whitelist.IsWhitelisted(constructor.DeclaringType.Module.Assembly.Name.Name, constructor.ToWhitespaceFullName()))
            return;
        var typeNode = (TypeNode)_nodeStack.Peek();
        typeNode.GetOrAddConstructor(constructor);
        base.VisitConstructor(constructor);
    }

    protected override void VisitField(FieldDefinition field)
    {
        if (field.IsSpecialName || field.IsRuntimeSpecialName)
            return;
        if (!field.IsAccessible())
            return;
        if (!_whitelist.IsWhitelisted(field.DeclaringType.Module.Assembly.Name.Name, field.ToWhitespaceFullName()))
            return;
        var typeNode = (TypeNode)_nodeStack.Peek();
        typeNode.GetOrAddField(field);
        base.VisitField(field);
    }

    protected override void VisitProperty(PropertyDefinition property)
    {
        if (property.IsSpecialName || property.IsRuntimeSpecialName)
            return;
        if (!property.IsAccessible())
            return;
        if (!_whitelist.IsWhitelisted(property.DeclaringType.Module.Assembly.Name.Name, property.ToWhitespaceFullName()))
            return;
        var typeNode = (TypeNode)_nodeStack.Peek();
        typeNode.GetOrAddProperty(property);
        base.VisitProperty(property);
    }

    protected override void VisitMethod(MethodDefinition method)
    {
        if (method.IsSpecialName || method.IsRuntimeSpecialName)
            return;
        if (!method.IsAccessible())
            return;
        if (!_whitelist.IsWhitelisted(method.DeclaringType.Module.Assembly.Name.Name, method.ToWhitespaceFullName()))
            return;
        var typeNode = (TypeNode)_nodeStack.Peek();
        typeNode.GetOrAddMethod(method);
        base.VisitMethod(method);
    }

    protected override void VisitEvent(EventDefinition member)
    {
        if (member.IsSpecialName || member.IsRuntimeSpecialName)
            return;
        if (!member.IsAccessible())
            return;
        if (!_whitelist.IsWhitelisted(member.DeclaringType.Module.Assembly.Name.Name, member.ToWhitespaceFullName()))
            return;
        var typeNode = (TypeNode)_nodeStack.Peek();
        typeNode.GetOrAddEvent(member);
        base.VisitEvent(member);
    }
}