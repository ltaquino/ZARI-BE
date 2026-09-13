namespace ZARI.Application.Features.Loan.LoanApplications.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteLoanApplicationCommand(Guid Id) : ICommand;
