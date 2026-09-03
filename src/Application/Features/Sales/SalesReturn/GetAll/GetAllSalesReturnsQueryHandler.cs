namespace ZARI.Application.Features.Sales.SalesReturns.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.Shared;
using ZARI.Domain.Common;

public sealed class GetAllSalesReturnsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllSalesReturnsQuery, Result<List<SalesReturnResponse>>>
{
    public async Task<Result<List<SalesReturnResponse>>> HandleAsync(GetAllSalesReturnsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("SALES_RETURNS", FormAction.View, cancellationToken))
            return Result.Failure<List<SalesReturnResponse>>(Error.Forbidden("SalesReturn.Forbidden", "You do not have permission to view sales returns."));

        var returns = await dbContext.SalesReturns.AsNoTracking()
            .Include(r => r.Customer)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .OrderByDescending(r => r.ReturnDate)
            .ToListAsync(cancellationToken);

        return Result.Success(returns.Select(SalesReturnMapper.ToResponse).ToList());
    }
}
