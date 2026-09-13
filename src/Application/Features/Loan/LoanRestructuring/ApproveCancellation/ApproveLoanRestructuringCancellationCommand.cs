namespace ZARI.Application.Features.Loan.LoanRestructurings.ApproveCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveLoanRestructuringCancellationCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<LoanRestructuringResponse>>;
