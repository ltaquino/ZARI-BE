namespace ZARI.Application.Features.Todos.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record UpdateTodoCommand(Guid Id, string Title, string? Description) : ICommand;
