namespace ZARI.Application.Features.Loan.LoanApplications.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Domain.Common;

public sealed record RejectLoanApplicationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<LoanApplicationResponse>>;
