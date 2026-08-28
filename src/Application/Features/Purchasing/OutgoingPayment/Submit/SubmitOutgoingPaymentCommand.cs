namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitOutgoingPaymentCommand(Guid Id, string RequestedBy) : ICommand<Result<OutgoingPaymentResponse>>;
