namespace ZARI.Application.Features.Loan.LoanDisbursements.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllLoanDisbursementsQuery : IQuery<Result<List<LoanDisbursementResponse>>>;

public sealed record LoanDisbursementResponse(
    Guid Id,
    string DisbursementNo,
    string BranchId,
    Guid LoanAccountId,
    string LoanAccountNo,
    string CustomerName,
    string LoanProductName,
    DateTimeOffset DisbursementDate,
    decimal Amount,
    Guid PaymentMethodId,
    string PaymentMethodName,
    string? ReferenceNo,
    Guid? CostCenterId,
    string? CostCenterName,
    string Status,
    string? Remarks,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
