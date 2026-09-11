namespace ZARI.Application.Features.Loan.LoanProducts.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteLoanProductCommand(Guid Id) : ICommand;
