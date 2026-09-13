namespace ZARI.Application.Features.Loan.LoanAccounts.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteLoanAccountCommand(Guid Id) : ICommand;
