namespace ZARI.Application.Features.Loan.LoanWriteOffs.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitLoanWriteOffCommand(Guid Id, string RequestedBy) : ICommand<Result<LoanWriteOffResponse>>;
