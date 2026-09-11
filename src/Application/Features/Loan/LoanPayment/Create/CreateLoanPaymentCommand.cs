namespace ZARI.Application.Features.Loan.LoanPayments.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Domain.Common;

public sealed record CreateLoanPaymentCommand(
    string BranchId,
    Guid LoanAccountId,
    DateTimeOffset PaymentDate,
    decimal Amount,
    Guid PaymentMethodId,
    string? ReferenceNo,
    Guid? CostCenterId,
    string? Remarks,
    string? CreatedBy) : ICommand<Result<LoanPaymentResponse>>;
