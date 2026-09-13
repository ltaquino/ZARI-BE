namespace ZARI.Application.Features.Loan.LoanDisbursements.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Domain.Common;

public sealed record RejectLoanDisbursementCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<LoanDisbursementResponse>>;
