namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Domain.Common;

public sealed record GetOutgoingPaymentQuery(Guid Id) : IQuery<Result<OutgoingPaymentResponse>>;
