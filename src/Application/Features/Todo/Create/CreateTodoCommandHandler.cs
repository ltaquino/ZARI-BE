namespace ZARI.Application.Features.Todos.Create;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Data.Repositories.Todo;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateTodoCommandHandler(IAppDbContext dbContext, ITodoItemRepository _todoItemRepository) : ICommandHandler<CreateTodoCommand, Result<CreateTodoResponse>>
{
    public async Task<Result<CreateTodoResponse>> HandleAsync(CreateTodoCommand command, CancellationToken cancellationToken = default)
    {
        var todo = new TodoItem
        {
            Title = command.Title,
            Description = command.Description
        };

        await _todoItemRepository.InsertAsync(todo);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CreateTodoResponse(todo.Id, todo.Title, todo.Description);
        return Result.Success(response);
    }
}
