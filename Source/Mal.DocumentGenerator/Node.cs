using System.Collections.Generic;
using System.Xml.Linq;

namespace Mal.DocumentGenerator;

public abstract class Node(Context context, string key, string assembly) : INode
{
    public Context Context { get; } = context;
    public string Key { get; } = key;
    public string Assembly { get; } = assembly;
    
    XElement? _xmlDoc;
    bool _didSearchForXmlDoc;

    public XElement? XmlDoc
    {
        get
        {
            if (_didSearchForXmlDoc) return _xmlDoc;
            _xmlDoc = Context.XmlDoc.FindByDocumentationCommentName(Assembly, Key);
            _didSearchForXmlDoc = true;
            return _xmlDoc;
        }
    }
    
    Node? INode.Parent => GetParent();

    IEnumerable<Node> INode.Children => EnumerateChildren();
    protected abstract Node? GetParent();
    protected abstract IEnumerable<Node> EnumerateChildren();

    public override string ToString() => Key;
}