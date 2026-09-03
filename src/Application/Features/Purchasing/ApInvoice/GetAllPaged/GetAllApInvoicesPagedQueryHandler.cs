namespace ZARI.Application.Features.Purchasing.ApInvoices.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Domain.Common;

public sealed class GetAllApInvoicesPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllApInvoicesPagedQuery, Result<PagedResult<ApInvoiceResponse>>>
{
    public async Task<Result<PagedResult<ApInvoiceResponse>>> HandleAsync(GetAllApInvoicesPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("AP_INVOICES", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<ApInvoiceResponse>>(Error.Forbidden("ApInvoice.Forbidden", "You do not have permission to view AP invoices."));

        var baseQuery = dbContext.ApInvoices.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.InvoiceNo.Contains(query.Search) || x.SupplierInvoiceNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var invoices = await baseQuery
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(i => i.Supplier)
            .Include(i => i.GoodsReceiptPo)
            .Include(i => i.Lines).ThenInclude(l => l.Item)
            .Include(i => i.Lines).ThenInclude(l => l.Uom)
            .Include(i => i.ExpenseLines).ThenInclude(l => l.GlAccount)
            .ToListAsync(cancellationToken);

        var amountsPaid = await ApInvoicePaymentBalance.GetAmountsPaidAsync(dbContext, invoices.Select(i => i.Id), cancellationToken);
        var items = invoices.Select(i => ApInvoiceMapper.ToResponse(i, amountsPaid[i.Id])).ToList();

        return Result.Success(new PagedResult<ApInvoiceResponse>(items, totalCount, query.Page, query.PageSize));
    }
}
