namespace ZARI.Application.Features.Loan.LoanApplications.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitLoanApplicationCommand(Guid Id, string RequestedBy) : ICommand<Result<LoanApplicationResponse>>;
