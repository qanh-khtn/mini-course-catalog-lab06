using System.Collections.Generic;

namespace MiniCourseCatalog.Mvc.ViewModels;

public class PaginationViewModel<T>
{
    public List<T> Items { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
}
