namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Domain.Common;

public sealed record ApprovePurchaseRequestCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<PurchaseRequestResponse>>;
