using System;

namespace Mal.DocumentGenerator.Dom;

public abstract class DocTypeMemberBuilder(DocDomBuilder context) : DocElementBuilder(context)
{
    bool _isVisited;
    public abstract string Id { get; }
    public IDocTypeMember Build() => throw new NotImplementedException();

    public DocTypeMemberBuilder Visit()
    {
        if (_isVisited)
            return this;
        _isVisited = true;
        OnVisit();
        return this;
    }

    protected abstract void OnVisit();

    class DocTypeMember : IDocTypeMember
    {
        readonly string _xmlDocId;
        readonly string _whitelistId;

        public DocTypeMember(string name, IDocType declaringType, string fullName, string xmlDocId, string whitelistId, bool isStatic)
        {
            FullName = fullName;
            Name = name;
            DeclaringType = declaringType;
            _xmlDocId = xmlDocId;
            _whitelistId = whitelistId;
            IsStatic = isStatic;
        }

        public bool IsStatic { get; }
        public string FullName { get; }
        public string Name { get; }
        public IDocType DeclaringType { get; }

        public string ToString(DocStringType type) =>
            type switch
            {
                DocStringType.FullName => FullName,
                DocStringType.XmlDoc => _xmlDocId,
                DocStringType.Whitelist => _whitelistId,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

        public override string ToString() => ToString(DocStringType.FullName);
    }
}