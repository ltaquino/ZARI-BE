namespace ZARI.Application.Features.Loan.LoanRestructurings.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanRestructurings.GetAll;
using ZARI.Domain.Common;

public sealed record GetLoanRestructuringQuery(Guid Id) : IQuery<Result<LoanRestructuringResponse>>;
