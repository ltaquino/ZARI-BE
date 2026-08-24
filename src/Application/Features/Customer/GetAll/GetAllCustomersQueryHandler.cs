namespace ZARI.Application.Features.Customers.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.Get;
using ZARI.Domain.Common;

public sealed class GetAllCustomersQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllCustomersQuery, Result<List<CustomerResponse>>>
{
    public async Task<Result<List<CustomerResponse>>> HandleAsync(GetAllCustomersQuery query, CancellationToken cancellationToken = default)
    {
        var customers = await dbContext.Customers
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CustomerResponse(c.Id, c.Name, c.Type, c.Email, c.Phone, c.BranchId, c.Status, c.Owner, c.Address, c.Notes, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(customers);
    }
}
