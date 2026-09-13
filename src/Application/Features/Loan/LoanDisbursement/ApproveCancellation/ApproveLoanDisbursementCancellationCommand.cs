namespace ZARI.Application.Features.Loan.LoanDisbursements.ApproveCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveLoanDisbursementCancellationCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<LoanDisbursementResponse>>;
