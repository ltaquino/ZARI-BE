namespace ZARI.Application.Features.Loan.LoanAccounts.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Domain.Common;

public sealed record CancelLoanAccountCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<LoanAccountResponse>>;
