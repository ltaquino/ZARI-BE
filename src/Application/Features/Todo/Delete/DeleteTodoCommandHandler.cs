namespace ZARI.Application.Features.Todos.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Application.Abstractions.Data.Repositories.Todo;

public sealed class DeleteTodoCommandHandler(IAppDbContext dbContext, ITodoItemRepository _todoItemRepository) : ICommandHandler<DeleteTodoCommand>
{
    public async Task<Result> HandleAsync(DeleteTodoCommand command, CancellationToken cancellationToken = default)
    {
        var todo = await dbContext.Todos.FindAsync([command.Id], cancellationToken);
        if (todo is null)
            return Result.Failure(Error.NotFound("Todo.NotFound", $"Todo with ID '{command.Id}' was not found."));

        await _todoItemRepository.DeleteAsync(todo);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
