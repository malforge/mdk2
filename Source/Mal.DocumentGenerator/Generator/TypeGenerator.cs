using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using Mal.DocumentGenerator.Common;
using Mal.DocumentGenerator.HtmlDom;

namespace Mal.DocumentGenerator.Generator;

public class TypeGenerator(GeneratorContext context, TypeNode typeNode) : Generator
{
    public GeneratorContext Context { get; } = context;
    public TypeNode TypeNode { get; } = typeNode;

    public override void Generate()
    {
        var pathBits = new List<string>
        {
            Context.OutputDirectory,
            "api"
        };
        pathBits.AddRange(TypeNode.Type.FullName.Split(".", StringSplitOptions.RemoveEmptyEntries).Select(FileName.GenerateSafeFileName));
        var fileInfo = new FileInfo(Path.Combine(pathBits.ToArray()) + ".html");
        if (!fileInfo.Directory?.Exists ?? false)
            fileInfo.Directory?.Create();
        var fileName = Path.Combine(pathBits.ToArray()) + ".html";
        var template = new BlazorTypeTemplate
        {
            Title = WebUtility.HtmlEncode(TypeNode.Type.Name),
            Fields = TypeNode.Fields().OrderBy(f => f.Name).Select(f => new BlazorTypeTemplate.FieldsItem
            {
                Description = GenerateDescriptionHtml(f),
                Signature = WebUtility.HtmlEncode(f.Signature())
            }).ToList(),
            Events = TypeNode.Events().OrderBy(f => f.Name).Select(e => new BlazorTypeTemplate.EventsItem
            {
                Description = GenerateDescriptionHtml(e),
                Signature = WebUtility.HtmlEncode(e.Signature())
            }).ToList(),
            Properties = TypeNode.Properties().OrderBy(f => f.Name).Select(p => new BlazorTypeTemplate.PropertiesItem
            {
                Description = GenerateDescriptionHtml(p),
                Signature = WebUtility.HtmlEncode(p.Signature())
            }).ToList(),
            Methods = TypeNode.Methods().OrderBy(f => f.Name).Select(m => new BlazorTypeTemplate.MethodsItem
            {
                Description = GenerateDescriptionHtml(m),
                Signature = WebUtility.HtmlEncode(m.Signature())
            }).ToList()
        };

        var markup = template.ToString();
        File.WriteAllText(fileName, markup);
    }

    string GenerateDescriptionHtml(FieldNode fieldNode)
    {
        if (fieldNode.XmlDoc == null)
            return string.Empty;

        var div = Html.Div();
        DecodeXmlDoc(fieldNode.XmlDoc, div);
        return div.ToString();
    }
    
    string GenerateDescriptionHtml(EventNode eventNode)
    {
        if (eventNode.XmlDoc == null)
            return string.Empty;

        var div = Html.Div();
        DecodeXmlDoc(eventNode.XmlDoc, div);
        return div.ToString();
    }
    
    string GenerateDescriptionHtml(PropertyNode propertyNode)
    {
        if (propertyNode.XmlDoc == null)
            return string.Empty;

        var div = Html.Div();
        DecodeXmlDoc(propertyNode.XmlDoc, div);
        return div.ToString();
    }
    
    string GenerateDescriptionHtml(MethodNode methodNode)
    {
        if (methodNode.XmlDoc == null)
            return string.Empty;

        var div = Html.Div();
        DecodeXmlDoc(methodNode.XmlDoc, div);
        return div.ToString();
    }

    void DecodeXmlDoc(XElement summary, XElement doc)
    {
        foreach (var node in summary.Nodes())
        {
            switch (node)
            {
                case XText text:
                    doc.Add(Html.Text(text.Value));
                    break;
                case XElement element:
                    var cref = element.Attribute("cref")?.Value;
                    TypeNode? type = null;
                    if (cref != null)
                    {
                        type = Context.TypeContext.FindType(cref);
                        if (type != null)
                        {
                            var link = Html.A($"api/{type.Url()}");
                            doc.Add(link);
                            doc = link;
                        }
                    }

                    switch (element.Name.LocalName)
                    {
                        case "see":
                            if (type != null)
                                doc.Add(Html.Text(type.Type.Name));
                            else if (cref != null)
                                doc.Add(Html.Text(cref));
                            break;
                        case "paramref":
                            var name = element.Attribute("name")?.Value;
                            if (name != null)
                                doc.Add(Html.C(name));
                            break;
                        case "c":
                            doc.Add(Html.C(element.Value));
                            break;
                        case "code":
                            doc.Add(Html.Pre(element.Value).WithClass("code"));
                            break;
                        case "list":
                            var listItems = element.Elements("item").Select(e => e.Value).ToList();
                            var list = Html.Ul();
                            doc.Add(list);
                            foreach (var item in listItems)
                                list.Add(Html.Li(item));
                            break;
                        case "para":
                        case "example":
                        case "exception":
                        case "remarks":
                        case "summary":
                            var p = Html.P();
                            doc.Add(p);
                            DecodeXmlDoc(element, p);
                            break;
                        default:
                            doc.Add(Html.Raw(element.ToString()));
                            break;
                    }
                    break;
            }
        }
    }
}