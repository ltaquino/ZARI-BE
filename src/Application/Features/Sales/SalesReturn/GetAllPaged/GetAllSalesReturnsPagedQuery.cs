namespace ZARI.Application.Features.Sales.SalesReturns.GetAllPaged;

using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllSalesReturnsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<SalesReturnResponse>>>;
