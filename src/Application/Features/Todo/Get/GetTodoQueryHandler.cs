using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Data.Repositories.Todo;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

namespace ZARI.Application.Features.Todos.Get;

public sealed class GetTodoQueryHandler(ITodoItemRepository _todoItemRepository) : IQueryHandler<GetTodoQuery, Result<TodoDetailResponse>>
{
    public async Task<Result<TodoDetailResponse>> HandleAsync(GetTodoQuery query, CancellationToken cancellationToken = default)
    {
        var todo = await _todoItemRepository.GetByIdAsync(query.Id);
        if (todo is null)
            return Result.Failure<TodoDetailResponse>(Error.NotFound("Todo.NotFound", $"Todo with ID '{query.Id}' was not found."));

        var response = new TodoDetailResponse(todo.Id, todo.Title, todo.Description, todo.IsCompleted, todo.CompletedAt, todo.CreatedAt);
        return Result.Success(response);
    }
}
