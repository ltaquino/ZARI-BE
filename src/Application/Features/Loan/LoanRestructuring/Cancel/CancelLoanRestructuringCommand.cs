namespace ZARI.Application.Features.Loan.LoanRestructurings.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Domain.Common;

public sealed record CancelLoanRestructuringCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<LoanRestructuringResponse>>;
