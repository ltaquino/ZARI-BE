namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Domain.Common;

public sealed record OutgoingPaymentLineInput(Guid ApInvoiceId, decimal Amount);

public sealed record CreateOutgoingPaymentCommand(
    string BranchId,
    Guid SupplierId,
    Guid BankAccountId,
    DateTimeOffset PaymentDate,
    string? RefNo,
    string? Remarks,
    Guid? CostCenterId,
    string? CreatedBy,
    List<OutgoingPaymentLineInput> Lines) : ICommand<Result<OutgoingPaymentResponse>>;
