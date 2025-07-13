using Mdk.DocGen3.CodeDoc;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public interface IMemberDocumentation
{
    MemberReference Member { get; }
    DocMember? Documentation { get; }
    string WhitelistKey { get; }
    string DocKey { get; }
    string FullName { get; }
    string AssemblyName { get; }
    string Namespace { get; }
    string Title { get; }
    string Name { get; }
    string ShortSignature();
   
}