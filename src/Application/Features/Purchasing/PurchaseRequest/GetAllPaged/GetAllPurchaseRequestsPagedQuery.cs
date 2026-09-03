namespace ZARI.Application.Features.Purchasing.PurchaseRequests.GetAllPaged;

using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllPurchaseRequestsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<PurchaseRequestResponse>>>;
