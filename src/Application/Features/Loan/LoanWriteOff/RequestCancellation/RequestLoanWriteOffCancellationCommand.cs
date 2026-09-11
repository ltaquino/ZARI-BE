namespace ZARI.Application.Features.Loan.LoanWriteOffs.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanWriteOffs.GetAll;
using ZARI.Domain.Common;

public sealed record RequestLoanWriteOffCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<LoanWriteOffResponse>>;
