namespace ZARI.Application.Features.Loan.LoanRestructurings.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Domain.Common;

public sealed record RequestLoanRestructuringCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<LoanRestructuringResponse>>;
