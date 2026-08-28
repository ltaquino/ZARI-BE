namespace ZARI.Application.Features.Purchasing.OutgoingPayments.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Domain.Common;

public sealed record RequestOutgoingPaymentCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<OutgoingPaymentResponse>>;
