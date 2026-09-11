namespace ZARI.Application.Features.Loan.LoanPayments.ApproveCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveLoanPaymentCancellationCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<LoanPaymentResponse>>;
