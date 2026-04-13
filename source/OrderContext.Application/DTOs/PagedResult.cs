namespace OrderContext.Application.DTOs;

/// <summary>
/// Paginated result for queries.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
/// <param name="Items">The items in the current page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
/// <param name="Page">The current page number (1-based).</param>
/// <param name="PageSize">The number of items per page.</param>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Gets whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
}
