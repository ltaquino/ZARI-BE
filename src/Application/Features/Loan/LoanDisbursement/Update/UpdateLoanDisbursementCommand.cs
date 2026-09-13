namespace ZARI.Application.Features.Loan.LoanDisbursements.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateLoanDisbursementCommand(
    Guid Id,
    string BranchId,
    DateTimeOffset DisbursementDate,
    decimal Amount,
    Guid PaymentMethodId,
    string? ReferenceNo,
    Guid? CostCenterId,
    string? Remarks,
    string? UpdatedBy) : ICommand<Result<LoanDisbursementResponse>>;
