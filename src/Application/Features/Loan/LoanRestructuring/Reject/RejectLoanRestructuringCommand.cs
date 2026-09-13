namespace ZARI.Application.Features.Loan.LoanRestructurings.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Domain.Common;

public sealed record RejectLoanRestructuringCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<LoanRestructuringResponse>>;
