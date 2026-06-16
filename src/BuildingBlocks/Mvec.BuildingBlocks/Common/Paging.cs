namespace Mvec.BuildingBlocks.Common;

public record PagedRequest(int Page = 1, int PageSize = 20, string? Sort = null, string? Search = null);
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
