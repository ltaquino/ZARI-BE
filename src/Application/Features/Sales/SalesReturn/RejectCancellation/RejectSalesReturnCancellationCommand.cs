namespace ZARI.Application.Features.Sales.SalesReturns.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Domain.Common;

public sealed record RejectSalesReturnCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<SalesReturnResponse>>;
