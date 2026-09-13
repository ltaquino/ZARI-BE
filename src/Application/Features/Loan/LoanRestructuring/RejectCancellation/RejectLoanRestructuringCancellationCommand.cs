namespace ZARI.Application.Features.Loan.LoanRestructurings.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Domain.Common;

public sealed record RejectLoanRestructuringCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<LoanRestructuringResponse>>;
