namespace ZARI.Application.Features.Todos.Update;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Data.Repositories.Todo;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateTodoCommandHandler(IAppDbContext dbContext, ITodoItemRepository _todoItemRepository) : ICommandHandler<UpdateTodoCommand>
{
    public async Task<Result> HandleAsync(UpdateTodoCommand command, CancellationToken cancellationToken = default)
    {
        var todo = await _todoItemRepository.GetByIdAsync(command.Id);
        if (todo is null)
            return Result.Failure(Error.NotFound("Todo.NotFound", $"Todo with ID '{command.Id}' was not found."));

        todo.Title = command.Title;
        todo.Description = command.Description;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
