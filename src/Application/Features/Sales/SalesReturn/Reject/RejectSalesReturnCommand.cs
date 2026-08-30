namespace ZARI.Application.Features.Sales.SalesReturns.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Domain.Common;

public sealed record RejectSalesReturnCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<SalesReturnResponse>>;
