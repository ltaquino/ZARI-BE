namespace ZARI.Application.Features.Purchasing.ApInvoices.GetAllPaged;

using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllApInvoicesPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<ApInvoiceResponse>>>;
