namespace ZARI.Application.Features.Loan.LoanRestructurings.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveLoanRestructuringCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<LoanRestructuringResponse>>;
