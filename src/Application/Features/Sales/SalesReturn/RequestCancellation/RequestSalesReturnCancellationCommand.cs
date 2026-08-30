namespace ZARI.Application.Features.Sales.SalesReturns.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Domain.Common;

public sealed record RequestSalesReturnCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<SalesReturnResponse>>;
