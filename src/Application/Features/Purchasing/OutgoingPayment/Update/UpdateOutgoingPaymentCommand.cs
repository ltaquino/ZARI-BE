namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Create;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateOutgoingPaymentCommand(
    Guid Id,
    Guid BankAccountId,
    DateTimeOffset PaymentDate,
    string? RefNo,
    string? Remarks,
    string? UpdatedBy,
    List<OutgoingPaymentLineInput> Lines) : ICommand<Result<OutgoingPaymentResponse>>;
