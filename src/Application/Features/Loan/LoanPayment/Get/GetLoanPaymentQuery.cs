namespace ZARI.Application.Features.Loan.LoanPayments.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Domain.Common;

public sealed record GetLoanPaymentQuery(Guid Id) : IQuery<Result<LoanPaymentResponse>>;
