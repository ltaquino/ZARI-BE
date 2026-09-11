namespace ZARI.Application.Features.Loan.LoanApplications.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveLoanApplicationCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<LoanApplicationResponse>>;
