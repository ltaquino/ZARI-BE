namespace ZARI.Application.Features.Purchasing.OutgoingPayments.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Domain.Common;

public sealed record RejectOutgoingPaymentCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<OutgoingPaymentResponse>>;
