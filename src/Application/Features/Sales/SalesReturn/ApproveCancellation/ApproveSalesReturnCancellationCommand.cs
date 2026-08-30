namespace ZARI.Application.Features.Sales.SalesReturns.ApproveCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveSalesReturnCancellationCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<SalesReturnResponse>>;
