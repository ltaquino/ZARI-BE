namespace ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllLoanWriteOffsQuery : IQuery<Result<List<LoanWriteOffResponse>>>;

public sealed record LoanWriteOffResponse(
    Guid Id,
    string WriteOffNo,
    string BranchId,
    Guid LoanAccountId,
    string LoanAcctNo,
    string CustomerName,
    string LoanProductName,
    DateTimeOffset WriteOffDate,
    decimal Amount,
    Guid WriteOffExpenseAccountId,
    string WriteOffExpenseAccountName,
    string Reason,
    string Status,
    string? Remarks,
    string? CancelledBy,
    DateTimeOffset? CancelledAt,
    string? CancelReason,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
