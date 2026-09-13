namespace ZARI.Application.Features.Loan.LoanDisbursements.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanDisbursements.GetAll;
using ZARI.Application.Features.Loan.LoanDisbursements.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>Only while DRAFT. The LoanAccount a disbursement is against never changes once created — only the disbursement's own terms do.</summary>
public sealed class UpdateLoanDisbursementCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateLoanDisbursementCommand, Result<LoanDisbursementResponse>>
{
    public async Task<Result<LoanDisbursementResponse>> HandleAsync(UpdateLoanDisbursementCommand command, CancellationToken cancellationToken = default)
    {
        var disbursement = await dbContext.LoanDisbursements
            .Include(d => d.LoanAccount)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (disbursement is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("LoanDisbursement.NotFound", $"Loan disbursement with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Edit, disbursement.BranchId, cancellationToken))
            return Result.Failure<LoanDisbursementResponse>(Error.Forbidden("LoanDisbursement.Forbidden", "You do not have permission to edit this loan disbursement for this branch."));

        if (disbursement.Status != "DRAFT")
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.NotDraft", "Only a draft loan disbursement can be edited."));

        if (command.Amount != disbursement.LoanAccount.PrincipalAmount)
            return Result.Failure<LoanDisbursementResponse>(Error.Validation("LoanDisbursement.AmountMustMatchPrincipal",
                $"Disbursement amount must equal the loan account's full principal ({disbursement.LoanAccount.PrincipalAmount}) — partial/staggered release isn't supported yet."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var paymentMethod = await dbContext.PaymentMethods.FirstOrDefaultAsync(p => p.Id == command.PaymentMethodId, cancellationToken);
        if (paymentMethod is null)
            return Result.Failure<LoanDisbursementResponse>(Error.NotFound("PaymentMethod.NotFound", $"Payment method with ID '{command.PaymentMethodId}' was not found."));

        if (command.CostCenterId is { } costCenterId)
        {
            var costCenterExists = await dbContext.CostCenters.AnyAsync(c => c.Id == costCenterId, cancellationToken);
            if (!costCenterExists)
                return Result.Failure<LoanDisbursementResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{costCenterId}' was not found."));
        }

        disbursement.BranchId = command.BranchId;
        disbursement.DisbursementDate = command.DisbursementDate;
        disbursement.Amount = command.Amount;
        disbursement.PaymentMethodId = command.PaymentMethodId;
        disbursement.ReferenceNo = command.ReferenceNo;
        disbursement.CostCenterId = command.CostCenterId;
        disbursement.Remarks = command.Remarks;
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.LoanDisbursements
            .Include(d => d.LoanAccount).ThenInclude(a => a.Customer)
            .Include(d => d.LoanAccount).ThenInclude(a => a.LoanProduct)
            .Include(d => d.PaymentMethod)
            .Include(d => d.CostCenter)
            .FirstAsync(d => d.Id == disbursement.Id, cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_DISBURSEMENT", disbursement.Id.ToString(), disbursement.BranchId, "UPDATED", "ACTIVITY",
                "updated this loan disbursement", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanDisbursementResponse>(notifyResult.Error!);

        return Result.Success(LoanDisbursementMapper.ToResponse(saved));
    }
}
