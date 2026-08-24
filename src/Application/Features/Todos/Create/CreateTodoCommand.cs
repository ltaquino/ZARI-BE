namespace ZARI.Application.Features.Todos.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record CreateTodoCommand(string Title, string? Description) : ICommand<Result<CreateTodoResponse>>;

public sealed record CreateTodoResponse(Guid Id, string Title, string? Description);
