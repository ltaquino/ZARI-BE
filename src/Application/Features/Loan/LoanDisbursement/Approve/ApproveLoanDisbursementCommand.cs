namespace ZARI.Application.Features.Loan.LoanDisbursements.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveLoanDisbursementCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<LoanDisbursementResponse>>;
