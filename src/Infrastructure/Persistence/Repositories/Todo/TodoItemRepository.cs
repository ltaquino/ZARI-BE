using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data.Repositories.Todo;
using ZARI.Domain.Entities;

namespace ZARI.Infrastructure.Persistence.Repositories.Todo;


public class TodoItemRepository : ITodoItemRepository
{
    private readonly AppDbContext _context;

    public TodoItemRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TodoItem>> GetListAsync()

    {
        return await _context.Todos.ToListAsync();
    }
    public async Task<TodoItem?> GetByIdAsync(Guid todoItemId)
    {

        return await _context.Todos.Where(r => r.Id == todoItemId).FirstOrDefaultAsync();
    }

    public async Task<Guid> InsertAsync(TodoItem todoItem)
    {
        _context.Todos.Add(todoItem);
        await _context.SaveChangesAsync();
        return todoItem.Id;
    }

    public async Task UpdateAsync(TodoItem todoItem)
    {
        _context.Todos.Update(todoItem);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(TodoItem todoItem)
    {
        _context.Todos.Remove(todoItem);
        await _context.SaveChangesAsync();
    }
}

