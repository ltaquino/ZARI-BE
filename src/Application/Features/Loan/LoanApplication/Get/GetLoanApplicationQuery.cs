namespace ZARI.Application.Features.Loan.LoanApplications.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Domain.Common;

public sealed record GetLoanApplicationQuery(Guid Id) : IQuery<Result<LoanApplicationResponse>>;
