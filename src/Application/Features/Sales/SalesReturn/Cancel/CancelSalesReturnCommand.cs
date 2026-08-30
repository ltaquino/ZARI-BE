namespace ZARI.Application.Features.Sales.SalesReturns.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Domain.Common;

public sealed record CancelSalesReturnCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<SalesReturnResponse>>;
