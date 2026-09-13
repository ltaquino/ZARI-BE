namespace ZARI.Application.Features.Loan.LoanWriteOffs.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Domain.Common;

public sealed record CancelLoanWriteOffCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<LoanWriteOffResponse>>;
