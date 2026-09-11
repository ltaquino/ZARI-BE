namespace ZARI.Application.Features.Loan.LoanDisbursements.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Domain.Common;

public sealed record CreateLoanDisbursementCommand(
    string BranchId,
    Guid LoanAccountId,
    DateTimeOffset DisbursementDate,
    decimal Amount,
    Guid PaymentMethodId,
    string? ReferenceNo,
    Guid? CostCenterId,
    string? Remarks,
    string? CreatedBy) : ICommand<Result<LoanDisbursementResponse>>;
