using Mdk.DocGen3.Web;

namespace Mdk.DocGen3.Pages;

public class NamespaceModel : MemberPageModelBase
{
    public IEnumerable<TypeItemModel>? Types { get; set; }
}