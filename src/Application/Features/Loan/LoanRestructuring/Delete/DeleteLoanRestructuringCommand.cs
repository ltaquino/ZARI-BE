namespace ZARI.Application.Features.Loan.LoanRestructurings.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteLoanRestructuringCommand(Guid Id) : ICommand;
