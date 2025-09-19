using Mdk.DocGen3.Web;

namespace Mdk.DocGen3.Pages;

public class MemberPageModel : MemberPageModelBase
{
    public string? Namespace { get; set; }
    public string? Assembly { get; set; }
    public string? Returns { get; set; }
    public IEnumerable<Breadcrumb>? Interfaces { get; set; }
    public IEnumerable<MemberTable>? MemberTables { get; set; }
}