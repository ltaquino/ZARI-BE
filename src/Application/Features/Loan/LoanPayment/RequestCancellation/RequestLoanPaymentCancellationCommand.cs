namespace ZARI.Application.Features.Loan.LoanPayments.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Domain.Common;

public sealed record RequestLoanPaymentCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<LoanPaymentResponse>>;
