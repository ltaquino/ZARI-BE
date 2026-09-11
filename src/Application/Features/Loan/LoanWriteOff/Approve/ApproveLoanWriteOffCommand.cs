namespace ZARI.Application.Features.Loan.LoanWriteOffs.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveLoanWriteOffCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<LoanWriteOffResponse>>;
