namespace ZARI.Application.Features.Loan.LoanAccounts.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Domain.Common;

public sealed record CreateLoanAccountCommand(
    string BranchId,
    Guid CustomerId,
    Guid LoanProductId,
    Guid? LoanApplicationId,
    decimal PrincipalAmount,
    int TermMonths,
    DateTimeOffset GrantDate,
    DateTimeOffset FirstDueDate,
    string? Remarks,
    Guid? LoanReceivableAccountId,
    Guid? InterestIncomeAccountId,
    Guid? PenaltyIncomeAccountId,
    string? CreatedBy) : ICommand<Result<LoanAccountResponse>>;
