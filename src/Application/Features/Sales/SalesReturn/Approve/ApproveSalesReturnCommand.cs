namespace ZARI.Application.Features.Sales.SalesReturns.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveSalesReturnCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<SalesReturnResponse>>;
