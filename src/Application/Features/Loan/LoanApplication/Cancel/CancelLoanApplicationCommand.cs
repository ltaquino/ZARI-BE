namespace ZARI.Application.Features.Loan.LoanApplications.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Domain.Common;

public sealed record CancelLoanApplicationCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<LoanApplicationResponse>>;
