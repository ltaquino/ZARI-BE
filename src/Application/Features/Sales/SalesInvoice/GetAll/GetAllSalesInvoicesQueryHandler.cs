namespace ZARI.Application.Features.Sales.SalesInvoices.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Domain.Common;

public sealed class GetAllSalesInvoicesQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllSalesInvoicesQuery, Result<List<SalesInvoiceResponse>>>
{
    public async Task<Result<List<SalesInvoiceResponse>>> HandleAsync(GetAllSalesInvoicesQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("SALES_INVOICES", FormAction.View, cancellationToken))
            return Result.Failure<List<SalesInvoiceResponse>>(Error.Forbidden("SalesInvoice.Forbidden", "You do not have permission to view sales invoices."));

        var invoices = await dbContext.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);

        return Result.Success(invoices.Select(SalesInvoiceMapper.ToResponse).ToList());
    }
}
