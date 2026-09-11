namespace ZARI.Application.Features.Loan.LoanWriteOffs.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteLoanWriteOffCommand(Guid Id) : ICommand;
