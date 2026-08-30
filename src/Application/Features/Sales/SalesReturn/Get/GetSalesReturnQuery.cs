namespace ZARI.Application.Features.Sales.SalesReturns.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Domain.Common;

public sealed record GetSalesReturnQuery(Guid Id) : IQuery<Result<SalesReturnResponse>>;
