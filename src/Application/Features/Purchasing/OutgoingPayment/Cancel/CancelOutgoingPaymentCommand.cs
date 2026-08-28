namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Domain.Common;

public sealed record CancelOutgoingPaymentCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<OutgoingPaymentResponse>>;
