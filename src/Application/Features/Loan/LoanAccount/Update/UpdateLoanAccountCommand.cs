namespace ZARI.Application.Features.Loan.LoanAccounts.Update;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Domain.Common;

public sealed record UpdateLoanAccountCommand(
    Guid Id,
    string BranchId,
    Guid CustomerId,
    Guid LoanProductId,
    decimal PrincipalAmount,
    int TermMonths,
    DateTimeOffset GrantDate,
    DateTimeOffset FirstDueDate,
    string? Remarks,
    Guid? LoanReceivableAccountId,
    Guid? InterestIncomeAccountId,
    Guid? PenaltyIncomeAccountId,
    string? UpdatedBy) : ICommand<Result<LoanAccountResponse>>;
