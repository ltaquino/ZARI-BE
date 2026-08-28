namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteOutgoingPaymentCommand(Guid Id) : ICommand<Result>;
