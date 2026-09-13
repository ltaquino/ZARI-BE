namespace ZARI.Application.Features.Loan.LoanPayments.RejectCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Domain.Common;

public sealed record RejectLoanPaymentCancellationCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<LoanPaymentResponse>>;
