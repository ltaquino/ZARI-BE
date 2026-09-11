namespace ZARI.Application.Features.Loan.LoanWriteOffs.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Domain.Common;

public sealed record GetLoanWriteOffQuery(Guid Id) : IQuery<Result<LoanWriteOffResponse>>;
