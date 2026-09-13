namespace ZARI.Application.Features.Loan.LoanAccounts.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Domain.Common;

public sealed record GetLoanAccountQuery(Guid Id) : IQuery<Result<LoanAccountResponse>>;
