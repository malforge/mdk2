using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Mal.DocumentGenerator;

public class XmlDoc(params string[] directories)
{
    readonly Dictionary<string, XDocument> _docs = new(StringComparer.OrdinalIgnoreCase);
    readonly List<string> _directories = [..directories];

    public void Load(string path)
    {
        Console.WriteLine("Loading XML documentation file: " + path);
        var document = XDocument.Load(path);
        // select assembly name through xpath
        var assemblyName = document.XPathSelectElement("/doc/assembly/name")?.Value;
        if (assemblyName == null)
        {
            throw new InvalidOperationException("Invalid XML documentation file.");
        }
        
        _docs[assemblyName] = document;
    }
    
    public XElement? FindByDocumentationCommentName(string assemblyName, string name)
    {
        if (_docs.TryGetValue(assemblyName, out var document))
        {
            return document.XPathSelectElement($"/doc/members/member[@name='{name}']");
        }
        
        // Try to find an xml file matching the assembly name in one of the directories
        foreach (var directory in _directories)
        {
            var path = System.IO.Path.Combine(directory, assemblyName + ".xml");
            if (System.IO.File.Exists(path))
            {
                Load(path);
                return FindByDocumentationCommentName(assemblyName, name);
            }
        }
        
        return null;
    }
    
    public XElement? Find(INode node)
    {
        return FindByDocumentationCommentName(node.Assembly, node.Key);
    }
}