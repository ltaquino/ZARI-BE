namespace ZARI.Application.Features.Loan.LoanDisbursements.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Domain.Common;

public sealed record CancelLoanDisbursementCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<LoanDisbursementResponse>>;
