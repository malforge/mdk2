using System.Text;
using Mdk.DocGen3.CodeSecurity;
using Mdk.DocGen3.Support;
using Mdk.DocGen3.Types;

namespace Mdk.DocGen3.Pages;

public class TypePage(TypeDocumentation typeDocumentation) : DocumentationPage
{
    public TypeDocumentation TypeDocumentation { get; } = typeDocumentation;

    public override IMemberDocumentation GetMemberDocumentation() => TypeDocumentation;

    public override bool IsIgnored(Whitelist whitelist)
    {
        var type = TypeDocumentation;
        if (type.Type.IsMsType())
            return true;

        if (!whitelist.IsAllowed(type.WhitelistKey))
            return true;

        return false;
    }
    
    public IEnumerable<MethodDocumentation> Constructors() =>
        TypeDocumentation.Methods
            .Where(m => m is {IsConstructor: true, IsPublic: true});
    
    public IEnumerable<FieldDocumentation> Fields() =>
        TypeDocumentation.Fields
            .Where(f => f.IsPublic);
    
    public IEnumerable<EventDocumentation> Events() =>
        TypeDocumentation.Events
            .Where(e => e.IsPublic);
    
    public IEnumerable<PropertyDocumentation> Properties() =>
        TypeDocumentation.Properties
            .Where(p => p.IsPublic);
    
    public IEnumerable<MethodDocumentation> Methods() =>
        TypeDocumentation.Methods
            .Where(m => m is {IsConstructor: false, IsPublic: true})
            .Where(m => !m.Method.IsSpecialName); // Exclude property getters/setters
    }