namespace ZARI.Application.Features.Purchasing.PurchaseRequests.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.PurchaseRequests.GetAll;
using ZARI.Domain.Common;

public sealed record RejectPurchaseRequestCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<PurchaseRequestResponse>>;
