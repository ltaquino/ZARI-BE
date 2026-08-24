using Microsoft.EntityFrameworkCore;
using ZARI.Domain.Entities;

namespace ZARI.Application.Abstractions.Data;

public interface IAppDbContext
{
    DbSet<TodoItem> Todos { get; }
    DbSet<Uom> Uoms { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}
