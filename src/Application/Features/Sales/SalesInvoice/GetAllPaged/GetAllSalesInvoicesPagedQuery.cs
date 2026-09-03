namespace ZARI.Application.Features.Sales.SalesInvoices.GetAllPaged;

using ZARI.Application.Features.Sales.SalesInvoices.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllSalesInvoicesPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<SalesInvoiceResponse>>>;
