namespace ZARI.Application.Features.Todos.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Todos.Get;
using ZARI.Domain.Common;

public sealed record GetAllTodosQuery(int Page = 1, int PageSize = 10) : IQuery<Result<PagedResult<TodoDetailResponse>>>;
