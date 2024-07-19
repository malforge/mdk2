using Mono.Cecil;

namespace Mal.DocumentGenerator;

public interface ITypeContainer
{
    TypeNode GetOrAddType(TypeDefinition type);
}