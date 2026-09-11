namespace ZARI.Application.Features.Loan.LoanDisbursements.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Domain.Common;

public sealed record RejectLoanDisbursementCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<LoanDisbursementResponse>>;
