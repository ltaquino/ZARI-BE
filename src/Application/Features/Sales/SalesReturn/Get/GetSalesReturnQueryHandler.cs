namespace ZARI.Application.Features.Sales.SalesReturns.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Application.Features.Sales.SalesReturns.Shared;
using ZARI.Domain.Common;

public sealed class GetSalesReturnQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetSalesReturnQuery, Result<SalesReturnResponse>>
{
    public async Task<Result<SalesReturnResponse>> HandleAsync(GetSalesReturnQuery query, CancellationToken cancellationToken = default)
    {
        var salesReturn = await dbContext.SalesReturns
            .Include(r => r.Customer)
            .Include(r => r.Lines).ThenInclude(l => l.Item)
            .Include(r => r.Lines).ThenInclude(l => l.Uom)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);

        if (salesReturn is null)
            return Result.Failure<SalesReturnResponse>(Error.NotFound("SalesReturn.NotFound", $"Sales return with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("SALES_RETURNS", FormAction.View, salesReturn.BranchId, cancellationToken))
            return Result.Failure<SalesReturnResponse>(Error.Forbidden("SalesReturn.Forbidden", "You do not have permission to view sales returns for this branch."));

        return Result.Success(SalesReturnMapper.ToResponse(salesReturn));
    }
}
