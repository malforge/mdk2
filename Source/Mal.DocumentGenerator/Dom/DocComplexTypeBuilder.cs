using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mal.DocumentGenerator.Dom;

public class DocComplexTypeBuilder : DocTypeBuilder
{
    readonly ComplexType _complexType;
    readonly List<DocTypeMemberBuilder> _members = new();
    readonly List<DocTypeBuilder> _nestedTypes = new();
    readonly List<DocTypeBuilder> _interfaces = new();
    readonly List<DocTypeParameterBuilder> _typeParameters = new();
    readonly TypeDefinition _typeDefinition;
    DocTypeBuilder? _baseType;

    public DocComplexTypeBuilder(DocDomBuilder context, TypeDefinition typeDefinition) : base(context)
    {
        _typeDefinition = typeDefinition;
        _complexType = typeDefinition.IsClass ? new Class(typeDefinition.GetCSharpName(), typeDefinition.GetDocumentationCommentName(), typeDefinition.GetWhitelistName()) :
            typeDefinition.IsValueType ? new Struct(typeDefinition.GetCSharpName(), typeDefinition.GetDocumentationCommentName(), typeDefinition.GetWhitelistName()) :
            typeDefinition.IsInterface ? new Interface(typeDefinition.GetCSharpName(), typeDefinition.GetDocumentationCommentName(), typeDefinition.GetWhitelistName()) :
            throw new InvalidOperationException("Type is not a class, struct, or interface");
    }

    public override string Id => _complexType.FullName;

    public DocComplexTypeBuilder WithNestedType(TypeReference type)
    {
        var existing = _nestedTypes.FirstOrDefault(t => t.Id == type.GetCSharpName());
        if (existing != null)
            return this;

        var nestedType = Context.GetOrAddType(type);
        _nestedTypes.Add(nestedType);
        return this;
    }

    public DocComplexTypeBuilder WithNestedType(DocTypeBuilder builder)
    {
        var existing = _nestedTypes.FirstOrDefault(t => t.Id == builder.Id);
        if (existing != null && existing != builder)
            throw new InvalidOperationException($"Something went seriously wrong: We found a type with the same ID but it's not the same instance. ID: {builder.Id}");
        if (existing != null)
            return this;

        _nestedTypes.Add(builder);
        return this;
    }
    
    public override IDocType Build()
    {
        _complexType.BaseType = _baseType?.Build();
        _complexType.Interfaces = _interfaces.Select(i => i.Build()).ToImmutableArray();
        _complexType.Members = _members.Select(m => m.Build()).ToImmutableArray();
        _complexType.NestedTypes = _nestedTypes.Select(t => t.Build()).ToImmutableArray();
        _complexType.TypeParameters = _typeParameters.Select(p => p.Build()).ToImmutableArray();
        return _complexType;
    }

    protected override void OnVisit()
    {
        var fields = _typeDefinition.Fields;
        if (fields.Count > 0)
            VisitFields(_typeDefinition);

        var events = _typeDefinition.Events;
        if (events.Count > 0)
            VisitEvents(_typeDefinition);

        var properties = _typeDefinition.Properties;
        if (properties.Count > 0)
            VisitProperties(_typeDefinition);

        var methods = _typeDefinition.Methods;
        if (methods.Count > 0)
            VisitMethods(_typeDefinition);

        var nestedTypes = _typeDefinition.NestedTypes;
        if (nestedTypes.Count > 0)
            VisitNestedTypes(_typeDefinition);

        var baseType = _typeDefinition.BaseType;
        if (baseType != null)
            VisitBaseType(_typeDefinition);

        var interfaces = _typeDefinition.Interfaces;
        if (interfaces.Count > 0)
            VisitInterfaces(_typeDefinition);

        var genericParameters = _typeDefinition.HasGenericParameters ? _typeDefinition.GenericParameters : new Collection<GenericParameter>();
        if (genericParameters.Count > 0)
            VisitGenericParameters(genericParameters);
    }

    void VisitGenericParameters(Collection<GenericParameter> genericParameters)
    {
        foreach (var parameter in genericParameters)
        {
            var member = new DocTypeParameterBuilder(Context, parameter);
            _typeParameters.Add(member);
            member.Visit();
        }
    }

    void VisitFields(TypeDefinition typeDefinition)
    {
        var assemblyName = typeDefinition.Module.Assembly.Name.Name;
        var whitelist = Context.Whitelist;

        foreach (var field in typeDefinition.Fields.Where(f => f.IsPublic))
        {
            if (!whitelist.IsWhitelisted(assemblyName, field.GetWhitelistName()))
                continue;

            var member = new DocTypeFieldBuilder(Context, field);
            _members.Add(member);

            member.Visit();
        }
    }

    void VisitEvents(TypeDefinition typeDefinition)
    {
        var assemblyName = typeDefinition.Module.Assembly.Name.Name;
        var whitelist = Context.Whitelist;

        foreach (var @event in typeDefinition.Events.Where(e => e.AddMethod.IsPublic))
        {
            if (!whitelist.IsWhitelisted(assemblyName, @event.GetWhitelistName()))
                continue;

            var member = new DocTypeEventBuilder(Context, @event);
            _members.Add(member);

            member.Visit();
        }
    }

    void VisitProperties(TypeDefinition typeDefinition)
    {
        var assemblyName = typeDefinition.Module.Assembly.Name.Name;
        var whitelist = Context.Whitelist;

        foreach (var property in typeDefinition.Properties.Where(p => p.GetMethod.IsPublic))
        {
            if (!whitelist.IsWhitelisted(assemblyName, property.GetWhitelistName()))
                continue;

            var member = new DocTypePropertyBuilder(Context, property);
            _members.Add(member);

            member.Visit();
        }
    }

    void VisitMethods(TypeDefinition typeDefinition)
    {
        var assemblyName = typeDefinition.Module.Assembly.Name.Name;
        var whitelist = Context.Whitelist;

        foreach (var method in typeDefinition.Methods.Where(m => m.IsPublic))
        {
            if (!whitelist.IsWhitelisted(assemblyName, method.GetWhitelistName()))
                continue;

            var extensionMethodTargetType = method.IsExtensionMethod() ? method.Parameters[0].ParameterType : null;
            var extensionMethodTarget = extensionMethodTargetType != null ? Context.GetOrAddType(extensionMethodTargetType) : null;

            var member = new DocTypeMethodBuilder(Context, method, extensionMethodTarget);
            _members.Add(member);

            member.Visit();

            extensionMethodTarget?.AddExtensionMethod(member);
        }
    }

    void VisitNestedTypes(TypeDefinition typeDefinition)
    {
        var assemblyName = typeDefinition.Module.Assembly.Name.Name;
        var whitelist = Context.Whitelist;

        foreach (var nestedType in typeDefinition.NestedTypes.Select(n => n.Resolve()))
        {
            if (!whitelist.IsWhitelisted(assemblyName, nestedType.GetWhitelistName()))
                continue;

            var builder = Context.GetOrAddType(nestedType);
            _nestedTypes.Add(builder);
            builder.Visit();
        }
    }

    void VisitBaseType(TypeDefinition typeDefinition)
    {
        var assemblyName = typeDefinition.Module.Assembly.Name.Name;
        var whitelist = Context.Whitelist;

        var baseType = typeDefinition.BaseType.Resolve();
        if (baseType == null)
            return;

        if (!whitelist.IsWhitelisted(assemblyName, baseType.GetWhitelistName()))
            return;

        var builder = Context.GetOrAddType(baseType);
        _baseType = builder;
        builder.Visit();
    }

    void VisitInterfaces(TypeDefinition typeDefinition)
    {
        var assemblyName = typeDefinition.Module.Assembly.Name.Name;
        var whitelist = Context.Whitelist;

        foreach (var iface in typeDefinition.Interfaces.Select(i => i.InterfaceType.Resolve()))
        {
            if (!whitelist.IsWhitelisted(assemblyName, iface.GetWhitelistName()))
                continue;

            var builder = Context.GetOrAddType(iface);
            _interfaces.Add(builder);
            builder.Visit();
        }
    }

    protected abstract class ComplexType(string fullName, string xmlDocId, string whitelistId) : DocType(fullName, xmlDocId, whitelistId), IDocComplexType
    {
        public IDocType? BaseType { get; set; }
        public ImmutableArray<IDocType> Interfaces { get; set; }
        public ImmutableArray<IDocTypeMember> Members { get; set; }
        public ImmutableArray<IDocType> NestedTypes { get; set; }
        public ImmutableArray<IDocGenericParameter> TypeParameters { get; set; }
    }

    protected class Class(string fullName, string xmlDocId, string whitelistId) : ComplexType(fullName, xmlDocId, whitelistId), IDocClass
    {
        public override DocTypeKind Kind => DocTypeKind.Class;
    }

    protected class Struct(string fullName, string xmlDocId, string whitelistId) : ComplexType(fullName, xmlDocId, whitelistId), IDocStruct
    {
        public override DocTypeKind Kind => DocTypeKind.Struct;
    }

    protected class Interface(string fullName, string xmlDocId, string whitelistId) : ComplexType(fullName, xmlDocId, whitelistId), IDocInterface
    {
        public override DocTypeKind Kind => DocTypeKind.Interface;
    }
}