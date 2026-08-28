namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Domain.Common;

public sealed record RejectOutgoingPaymentCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<OutgoingPaymentResponse>>;
