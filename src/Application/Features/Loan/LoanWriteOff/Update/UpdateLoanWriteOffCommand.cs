namespace ZARI.Application.Features.Loan.LoanWriteOffs.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateLoanWriteOffCommand(
    Guid Id,
    string BranchId,
    DateTimeOffset WriteOffDate,
    Guid WriteOffExpenseAccountId,
    string Reason,
    string? Remarks,
    string? UpdatedBy) : ICommand<Result<LoanWriteOffResponse>>;
