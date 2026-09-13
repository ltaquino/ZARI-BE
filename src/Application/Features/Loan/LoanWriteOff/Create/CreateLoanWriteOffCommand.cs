namespace ZARI.Application.Features.Loan.LoanWriteOffs.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Domain.Common;

public sealed record CreateLoanWriteOffCommand(
    string BranchId,
    Guid LoanAccountId,
    DateTimeOffset WriteOffDate,
    Guid WriteOffExpenseAccountId,
    string Reason,
    string? Remarks,
    string? CreatedBy) : ICommand<Result<LoanWriteOffResponse>>;
