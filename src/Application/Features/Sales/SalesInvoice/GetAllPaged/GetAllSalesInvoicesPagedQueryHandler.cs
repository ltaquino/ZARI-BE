namespace ZARI.Application.Features.Sales.SalesInvoices.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Domain.Common;

public sealed class GetAllSalesInvoicesPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllSalesInvoicesPagedQuery, Result<PagedResult<SalesInvoiceResponse>>>
{
    public async Task<Result<PagedResult<SalesInvoiceResponse>>> HandleAsync(GetAllSalesInvoicesPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("SALES_INVOICES", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<SalesInvoiceResponse>>(Error.Forbidden("SalesInvoice.Forbidden", "You do not have permission to view sales invoices."));

        var baseQuery = dbContext.SalesInvoices.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.InvoiceNo.Contains(query.Search) || (x.BirOrSeriesNumber != null && x.BirOrSeriesNumber.Contains(query.Search)));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var invoices = await baseQuery
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(i => i.Customer)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .ToListAsync(cancellationToken);

        var amountsPaid = await SalesInvoicePaymentBalance.GetAmountsPaidAsync(dbContext, invoices.Select(i => i.Id), cancellationToken);
        var items = invoices.Select(i => SalesInvoiceMapper.ToResponse(i, amountsPaid.GetValueOrDefault(i.Id))).ToList();

        return Result.Success(new PagedResult<SalesInvoiceResponse>(items, totalCount, query.Page, query.PageSize));
    }
}
