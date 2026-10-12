namespace Jobsy.Web.Models;

public sealed class VacancyListPage
{
    public List<VacancyListItem> Items { get; set; } = [];
    public int Skip { get; set; }
    public int Take { get; set; }
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}
