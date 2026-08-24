namespace ZARI.Application.Features.Todos.Complete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record CompleteTodoCommand(Guid Id) : ICommand;
