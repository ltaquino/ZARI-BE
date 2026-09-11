namespace ZARI.Application.Features.Loan.LoanAccounts.SetDisputeStatus;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Domain.Common;

public sealed record SetLoanAccountDisputeStatusCommand(Guid Id, bool IsDisputed, string? DisputeNotes, string? UpdatedBy) : ICommand<Result<LoanAccountResponse>>;
