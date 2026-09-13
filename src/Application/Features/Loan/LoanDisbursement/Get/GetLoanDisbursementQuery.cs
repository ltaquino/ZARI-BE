namespace ZARI.Application.Features.Loan.LoanDisbursements.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Domain.Common;

public sealed record GetLoanDisbursementQuery(Guid Id) : IQuery<Result<LoanDisbursementResponse>>;
