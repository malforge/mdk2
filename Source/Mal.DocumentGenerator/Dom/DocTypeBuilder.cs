using System;
using System.Collections.Generic;
using System.Linq;

namespace Mal.DocumentGenerator.Dom;

public abstract class DocTypeBuilder(DocDomBuilder context) : DocElementBuilder(context)
{
    bool _isVisited;
    readonly List<DocTypeMemberBuilder> _extensionMembers = new();
    public abstract string Id { get; }
    public abstract IDocType Build();

    public DocTypeBuilder Visit()
    {
        if (_isVisited)
            return this;
        _isVisited = true;
        OnVisit();
        return this;
    }

    public void AddExtensionMethod(DocTypeMethodBuilder member)
    {
        var existing = _extensionMembers.FirstOrDefault(m => m.Id == member.Id);
        if (existing != null && existing != member)
            throw new InvalidOperationException($"Something went seriously wrong: We found a member with the same ID but it's not the same instance. ID: {member.Id}");
        if (existing != null)
            return;
        _extensionMembers.Add(member);
    }

    protected abstract void OnVisit();

    protected abstract class DocType(string fullName, string xmlDocId, string whitelistId) : IDocType
    {
        public abstract DocTypeKind Kind { get; }
        public string FullName { get; } = fullName;
        public IDocType? DeclaringType { get; set; }
        public abstract IEnumerable<IDocTypeElement> Everything(); 

        public string XmlDocId { get; } = xmlDocId;
        public string WhitelistId { get; } = whitelistId;

        public virtual string ToString(DocStringType type) =>
            type switch
            {
                DocStringType.FullName => FullName ?? throw new InvalidOperationException("FullName is not set"),
                DocStringType.XmlDoc => XmlDocId ?? throw new InvalidOperationException("XmlDocId is not set"),
                DocStringType.Whitelist => WhitelistId ?? throw new InvalidOperationException("WhitelistId is not set"),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

        public override string ToString() => ToString(DocStringType.FullName);
    }
}