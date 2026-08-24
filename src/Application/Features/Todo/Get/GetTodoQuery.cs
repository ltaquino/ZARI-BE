namespace ZARI.Application.Features.Todos.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetTodoQuery(Guid Id) : IQuery<Result<TodoDetailResponse>>;

public sealed record TodoDetailResponse(Guid Id, string Title, string? Description, bool IsCompleted, DateTimeOffset? CompletedAt, DateTimeOffset CreatedAt);
