namespace ZARI.Application.Features.Todos.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteTodoCommand(Guid Id) : ICommand;
