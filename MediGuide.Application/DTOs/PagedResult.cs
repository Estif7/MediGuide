namespace MediGuide.Application.DTOs;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record CursorPagedResult<T>(
    IReadOnlyList<T> Items,
    bool HasMore
);