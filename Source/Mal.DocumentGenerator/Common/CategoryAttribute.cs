using System;

namespace Mal.DocumentGenerator.Common;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class CategoryAttribute : Attribute
{
    public CategoryAttribute(string name)
    {
            Name = name;
        }

    public string Name { get; }
}