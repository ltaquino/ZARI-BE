namespace ZARI.Application.Features.Loan.LoanApplications.Submit;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Application.Features.Loan.LoanApplications.Shared;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;

/// <summary>DRAFT -> PENDING_APPROVAL. Creates the ApprovalRequest a checker will act on.</summary>
public sealed class SubmitLoanApplicationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<SubmitForApprovalCommand, Result<ApprovalRequestResponse>> submitForApprovalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<SubmitLoanApplicationCommand, Result<LoanApplicationResponse>>
{
    public async Task<Result<LoanApplicationResponse>> HandleAsync(SubmitLoanApplicationCommand command, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.Collaterals)
            .Include(a => a.CoMakers).ThenInclude(c => c.CoMakerCustomer)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (application is null)
            return Result.Failure<LoanApplicationResponse>(Error.NotFound("LoanApplication.NotFound", $"Loan application with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_APPLICATIONS", FormAction.Edit, application.BranchId, cancellationToken))
            return Result.Failure<LoanApplicationResponse>(Error.Forbidden("LoanApplication.Forbidden", "You do not have permission to submit this loan application for this branch."));

        if (application.Status != "DRAFT")
            return Result.Failure<LoanApplicationResponse>(Error.Validation("LoanApplication.NotDraft", "Only draft loan applications can be submitted for approval."));

        var submitResult = await submitForApprovalHandler.HandleAsync(
            new SubmitForApprovalCommand("LOAN_APPLICATION", application.Id.ToString(), application.BranchId, command.RequestedBy, null, null),
            cancellationToken);
        if (!submitResult.IsSuccess)
            return Result.Failure<LoanApplicationResponse>(submitResult.Error!);

        application.Status = "PENDING_APPROVAL";
        await dbContext.SaveChangesAsync(cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_APPLICATION", application.Id.ToString(), application.BranchId, "SUBMITTED", "APPROVAL_NEEDED",
                "submitted this loan application for approval", command.RequestedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanApplicationResponse>(notifyResult.Error!);

        return Result.Success(LoanApplicationMapper.ToResponse(application));
    }
}
