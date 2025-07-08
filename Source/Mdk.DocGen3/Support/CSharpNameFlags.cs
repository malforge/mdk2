namespace Mdk.DocGen3.Support;

[Flags]
public enum CSharpNameFlags
{
    None = 0,
    Namespace = 0b00000001,
    NestedParent = 0b00000100,
    Name = 0b00000010,
    Generics = 0b00001000,
    Parameters = 0b00010000,


    FullName = Namespace | NestedParent | Name | Generics | Parameters
}