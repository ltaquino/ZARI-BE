namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveOutgoingPaymentCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<OutgoingPaymentResponse>>;
