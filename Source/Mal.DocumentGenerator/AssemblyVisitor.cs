using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Mal.DocumentGenerator;

public abstract class AssemblyVisitor
{
    protected virtual void VisitAssembly(AssemblyDefinition assembly)
    {
        foreach (var module in assembly.Modules)
        {
            VisitModule(module);
        }
    }

    protected virtual void VisitModule(ModuleDefinition module)
    {
        foreach (var type in module.Types)
        {
            VisitType(type);
        }
    }

    protected virtual void VisitType(TypeDefinition type)
    {
        foreach (var constructor in type.GetConstructors())
        {
            VisitConstructor(constructor);
        }
        foreach (var field in type.Fields)
        {
            VisitField(field);
        }
        foreach (var property in type.Properties)
        {
            VisitProperty(property);
        }
        foreach (var method in type.Methods)
        {
            VisitMethod(method);
        }
        foreach (var @event in type.Events)
        {
            VisitEvent(@event);
        }
        foreach (var nestedType in type.NestedTypes)
        {
            VisitType(nestedType);
        }
    }

    protected virtual void VisitEvent(EventDefinition member)
    {
        
    }

    protected virtual void VisitMethod(MethodDefinition member)
    {
        
    }

    protected virtual void VisitProperty(PropertyDefinition member)
    {
        
    }

    protected virtual void VisitField(FieldDefinition member)
    {
        
    }

    protected virtual void VisitConstructor(MethodDefinition member)
    {
    }
}