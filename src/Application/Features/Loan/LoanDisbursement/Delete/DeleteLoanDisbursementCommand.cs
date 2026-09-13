namespace ZARI.Application.Features.Loan.LoanDisbursements.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteLoanDisbursementCommand(Guid Id) : ICommand;
