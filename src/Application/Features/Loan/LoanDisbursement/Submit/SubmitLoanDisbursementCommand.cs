namespace ZARI.Application.Features.Loan.LoanDisbursements.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitLoanDisbursementCommand(Guid Id, string RequestedBy) : ICommand<Result<LoanDisbursementResponse>>;
