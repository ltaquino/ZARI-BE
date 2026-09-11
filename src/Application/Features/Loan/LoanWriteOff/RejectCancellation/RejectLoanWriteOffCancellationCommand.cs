namespace ZARI.Application.Features.Loan.LoanWriteOffs.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Domain.Common;

public sealed record RejectLoanWriteOffCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<LoanWriteOffResponse>>;
