namespace DenariusAI.Web.ViewModels;

/// <summary>
/// Contains definitions for PaginationViewModel.
/// </summary>
public sealed record PaginationViewModel(int Page, int PageSize, int TotalItems)
{
    public static readonly int[] AllowedPageSizes = [10, 25, 50, 100];

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public int FirstItem => TotalItems == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItem => Math.Min(Page * PageSize, TotalItems);

    public static PaginationViewModel Create(int totalItems, int page, int pageSize)
    {
        var normalizedSize = AllowedPageSizes.Contains(pageSize) ? pageSize : AllowedPageSizes[0];
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)normalizedSize));
        return new(Math.Clamp(page, 1, totalPages), normalizedSize, totalItems);
    }
}
