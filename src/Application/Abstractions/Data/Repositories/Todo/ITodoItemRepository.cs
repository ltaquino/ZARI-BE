using ZARI.Domain.Entities;

namespace ZARI.Application.Abstractions.Data.Repositories.Todo;

public interface ITodoItemRepository
{
    Task<List<TodoItem>> GetListAsync();

    Task<TodoItem?> GetByIdAsync(Guid todoItemId);

    Task<Guid> InsertAsync(TodoItem todoItem);

    Task UpdateAsync(TodoItem todoItem);

    Task DeleteAsync(TodoItem todoItem);

}
