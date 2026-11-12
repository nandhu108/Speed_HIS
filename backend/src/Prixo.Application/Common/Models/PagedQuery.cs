namespace Prixo.Application.Common.Models;

/// <summary>
/// Standard list-endpoint query parameters. PageSize is clamped to protect the server.
/// </summary>
public class PagedQuery
{
    public const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = 20;

    public int Page { get => _page; set => _page = value < 1 ? 1 : value; }
    public int PageSize { get => _pageSize; set => _pageSize = Math.Clamp(value, 1, MaxPageSize); }
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public string Order { get; set; } = "asc";
}
