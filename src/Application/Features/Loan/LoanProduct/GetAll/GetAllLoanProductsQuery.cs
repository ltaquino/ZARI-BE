namespace ZARI.Application.Features.Loan.LoanProducts.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanProducts.Get;
using ZARI.Domain.Common;

public sealed record GetAllLoanProductsQuery : IQuery<Result<List<LoanProductResponse>>>;
