namespace ZARI.Application.Features.Loan.LoanDisbursements.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Domain.Common;

public sealed record RequestLoanDisbursementCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<LoanDisbursementResponse>>;
