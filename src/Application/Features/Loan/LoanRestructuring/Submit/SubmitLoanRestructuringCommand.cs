namespace ZARI.Application.Features.Loan.LoanRestructurings.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitLoanRestructuringCommand(Guid Id, string RequestedBy) : ICommand<Result<LoanRestructuringResponse>>;
