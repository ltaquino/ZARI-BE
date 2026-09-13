namespace ZARI.Application.Features.Loan.LoanDisbursements.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Application.Features.Loan.LoanDisbursements.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// v1 supports exactly one disbursement per LoanAccount — Amount must equal the account's full
/// PrincipalAmount, and a 1:1 guard blocks a second non-cancelled disbursement against the same
/// account. Staggered/partial release is deferred (design doc §4.6). Nothing is posted here — this
/// only stages the document at DRAFT; GL posting and the account activation happen on Approve.
/// </summary>
public sealed class CreateLoanDisbursementCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateLoanDisbursementCommand, Result<LoanDisbursementResponse>>
{
    public async Task<Result<LoanDisbursementResponse>> HandleAsync(CreateLoanDisbursementCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<LoanDisbursementResponse>(Error.Forbidden("LoanDisbursement.Forbidden", "You do not have permission to create loan disbursements for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var account = await dbContext.LoanAccounts.FirstOrDefaultAsync(a => a.Id == command.LoanAccountId, cancellationToken);
        if (account is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("LoanAccount.NotFound", $"Loan account with ID '{command.LoanAccountId}' was not found."));

        if (account.Status != "PENDING_DISBURSEMENT")
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.AccountNotPendingDisbursement", "Only a loan account still pending disbursement can be disbursed."));

        if (command.Amount != account.PrincipalAmount)
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.AmountMustMatchPrincipal",
                $"Disbursement amount must equal the loan account's full principal ({account.PrincipalAmount}) — partial/staggered release isn't supported yet."));

        var alreadyExists = await dbContext.LoanDisbursements.AnyAsync(d => d.LoanAccountId == command.LoanAccountId && d.Status != "CANCELLED", cancellationToken);
        if (alreadyExists)
            return Result.Failure<LoanDisbursementResponse>(Error.Conflict("LoanDisbursement.AlreadyExists", "This loan account already has a disbursement in progress or posted."));

        var paymentMethod = await dbContext.PaymentMethods.FirstOrDefaultAsync(p => p.Id == command.PaymentMethodId, cancellationToken);
        if (paymentMethod is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("PaymentMethod.NotFound", $"Payment method with ID '{command.PaymentMethodId}' was not found."));

        if (command.CostCenterId is { } costCenterId)
        {
            var costCenterExists = await dbContext.CostCenters.AnyAsync(c => c.Id == costCenterId, cancellationToken);
            if (!costCenterExists)
                return Result.Failure<LoanDisbursementResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{costCenterId}' was not found."));
        }

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "LOAN-DISB"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(numberResult.Error!);

        var disbursement = new LoanDisbursement
        {
            DisbursementNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            LoanAccountId = command.LoanAccountId,
            DisbursementDate = command.DisbursementDate,
            Amount = command.Amount,
            PaymentMethodId = command.PaymentMethodId,
            ReferenceNo = command.ReferenceNo,
            CostCenterId = command.CostCenterId,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CreatedBy = command.CreatedBy
        };

        dbContext.LoanDisbursements.Add(disbursement);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.LoanDisbursements
            .Include(d => d.LoanAccount).ThenInclude(a => a.Customer)
            .Include(d => d.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(d => d.PaymentMethod)
            .Include(d => d.CostCenter)
            .FirstAsync(d => d.Id == disbursement.Id, cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_DISBURSEMENT", disbursement.Id.ToString(), disbursement.BranchId, "CREATED", "ACTIVITY",
                "created this loan disbursement", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(notifyResult.Error!);

        return Result.Success(LoanDisbursementMapper.ToResponse(saved));
    }
}
