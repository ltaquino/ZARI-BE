namespace ZARI.Application.Features.Loan.LoanApplications.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Application.Features.Loan.LoanApplications.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateLoanApplicationCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateLoanApplicationCommand, Result<LoanApplicationResponse>>
{
    public async Task<Result<LoanApplicationResponse>> HandleAsync(UpdateLoanApplicationCommand command, CancellationToken cancellationToken = default)
    {
        var application = await dbContext.LoanApplications
            .Include(a => a.Collaterals)
            .Include(a => a.CoMakers)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (application is null)
            return Result.Failure<LoanApplicationResponse>(Error.NotFound("LoanApplication.NotFound", $"Loan application with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_APPLICATIONS", FormAction.Edit, application.BranchId, cancellationToken))
            return Result.Failure<LoanApplicationResponse>(Error.Forbidden("LoanApplication.Forbidden", "You do not have permission to edit this loan application for this branch."));

        if (application.Status != "DRAFT")
            return Result.Failure<LoanApplicationResponse>(Error.Validation("LoanApplication.NotDraft", "Only draft loan applications can be edited."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<LoanApplicationResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var customerExists = await dbContext.Customers.AnyAsync(c => c.Id == command.CustomerId, cancellationToken);
        if (!customerExists)
            return Result.Failure<LoanApplicationResponse>(Error.NotFound("Customer.NotFound", $"Customer with ID '{command.CustomerId}' was not found."));

        var product = await dbContext.LoanProducts.FirstOrDefaultAsync(p => p.Id == command.LoanProductId, cancellationToken);
        if (product is null)
            return Result.Failure<LoanApplicationResponse>(Error.NotFound("LoanProduct.NotFound", $"Loan product with ID '{command.LoanProductId}' was not found."));

        if (command.RequestedPrincipal < product.MinPrincipal || command.RequestedPrincipal > product.MaxPrincipal)
            return Result.Failure<LoanApplicationResponse>(Error.Validation("LoanApplication.PrincipalOutOfRange",
                $"Requested principal must be between {product.MinPrincipal} and {product.MaxPrincipal} for {product.Name}."));

        if (command.RequestedTermMonths < product.MinTermMonths || command.RequestedTermMonths > product.MaxTermMonths)
            return Result.Failure<LoanApplicationResponse>(Error.Validation("LoanApplication.TermOutOfRange",
                $"Requested term must be between {product.MinTermMonths} and {product.MaxTermMonths} months for {product.Name}."));

        if (product.RequiresCollateral && command.Collaterals.Count == 0)
            return Result.Failure<LoanApplicationResponse>(Error.Validation("LoanApplication.CollateralRequired", $"{product.Name} requires at least one collateral record."));

        if (product.RequiresCoMaker && command.CoMakers.Count == 0)
            return Result.Failure<LoanApplicationResponse>(Error.Validation("LoanApplication.CoMakerRequired", $"{product.Name} requires at least one co-maker."));

        var coMakerCustomerIds = command.CoMakers.Where(c => c.CoMakerCustomerId is not null).Select(c => c.CoMakerCustomerId!.Value).Distinct().ToList();
        if (coMakerCustomerIds.Count > 0)
        {
            var foundCount = await dbContext.Customers.CountAsync(c => coMakerCustomerIds.Contains(c.Id), cancellationToken);
            if (foundCount != coMakerCustomerIds.Count)
                return Result.Failure<LoanApplicationResponse>(Error.NotFound("Customer.NotFound", "One or more co-maker customers were not found."));
        }

        application.BranchId = command.BranchId;
        application.CustomerId = command.CustomerId;
        application.LoanProductId = command.LoanProductId;
        application.ApplicationDate = command.ApplicationDate;
        application.RequestedPrincipal = command.RequestedPrincipal;
        application.RequestedTermMonths = command.RequestedTermMonths;
        application.Purpose = command.Purpose;
        application.Remarks = command.Remarks;

        application.Collaterals.Clear();
        foreach (var c in command.Collaterals)
        {
            application.Collaterals.Add(new LoanCollateral { Description = c.Description, CollateralType = c.CollateralType, EstimatedValue = c.EstimatedValue, DocumentRef = c.DocumentRef });
        }

        application.CoMakers.Clear();
        foreach (var c in command.CoMakers)
        {
            application.CoMakers.Add(new LoanCoMaker { CoMakerCustomerId = c.CoMakerCustomerId, Name = c.Name, ContactNo = c.ContactNo });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.LoanApplications
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.Collaterals)
            .Include(a => a.CoMakers).ThenInclude(c => c.CoMakerCustomer)
            .FirstAsync(a => a.Id == application.Id, cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_APPLICATION", application.Id.ToString(), application.BranchId, "UPDATED", "ACTIVITY",
                "updated this loan application", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanApplicationResponse>(notifyResult.Error!);

        return Result.Success(LoanApplicationMapper.ToResponse(saved));
    }
}
