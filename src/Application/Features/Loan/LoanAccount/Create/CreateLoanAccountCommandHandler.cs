namespace ZARI.Application.Features.Loan.LoanAccounts.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Application.Features.Loan.LoanAccounts.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Direct creation, not a Submit/Approve document — an approved LoanApplication just sits there
/// until this references it (same as PurchaseOrder referencing an approved PurchaseRequest without
/// mutating it). The account starts PENDING_DISBURSEMENT; the schedule is generated here (all the
/// terms needed — principal, rate, term, frequency — are already known), but GL posting and the
/// PENDING_DISBURSEMENT->ACTIVE flip happen later, in LoanDisbursement (build-order step 5).
/// </summary>
public sealed class CreateLoanAccountCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateLoanAccountCommand, Result<LoanAccountResponse>>
{
    public async Task<Result<LoanAccountResponse>> HandleAsync(CreateLoanAccountCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_ACCOUNTS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<LoanAccountResponse>(Error.Forbidden("LoanAccount.Forbidden", "You do not have permission to create loan accounts for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<LoanAccountResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var customerExists = await dbContext.Customers.AnyAsync(c => c.Id == command.CustomerId, cancellationToken);
        if (!customerExists)
            return Result.Failure<LoanAccountResponse>(Error.NotFound("Customer.NotFound", $"Customer with ID '{command.CustomerId}' was not found."));

        var product = await dbContext.LoanProducts.FirstOrDefaultAsync(p => p.Id == command.LoanProductId, cancellationToken);
        if (product is null)
            return Result.Failure<LoanAccountResponse>(Error.NotFound("LoanProduct.NotFound", $"Loan product with ID '{command.LoanProductId}' was not found."));

        if (command.PrincipalAmount < product.MinPrincipal || command.PrincipalAmount > product.MaxPrincipal)
            return Result.Failure<LoanAccountResponse>(Error.Validation("LoanAccount.PrincipalOutOfRange",
                $"Principal must be between {product.MinPrincipal} and {product.MaxPrincipal} for {product.Name}."));

        if (command.TermMonths < product.MinTermMonths || command.TermMonths > product.MaxTermMonths)
            return Result.Failure<LoanAccountResponse>(Error.Validation("LoanAccount.TermOutOfRange",
                $"Term must be between {product.MinTermMonths} and {product.MaxTermMonths} months for {product.Name}."));

        if (command.LoanApplicationId is { } applicationId)
        {
            var application = await dbContext.LoanApplications.FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);
            if (application is null)
                return Result.Failure<LoanAccountResponse>(Error.NotFound("LoanApplication.NotFound", $"Loan application with ID '{applicationId}' was not found."));

            if (application.Status != "APPROVED")
                return Result.Failure<LoanAccountResponse>(Error.Validation("LoanAccount.ApplicationNotApproved", "Only an approved loan application can be converted into a loan account."));

            if (application.CustomerId != command.CustomerId || application.LoanProductId != command.LoanProductId)
                return Result.Failure<LoanAccountResponse>(Error.Validation("LoanAccount.ApplicationMismatch", "The loan account's member and loan product must match the referenced application."));

            var alreadyConverted = await dbContext.LoanAccounts.AnyAsync(a => a.LoanApplicationId == applicationId && a.Status != "CANCELLED", cancellationToken);
            if (alreadyConverted)
                return Result.Failure<LoanAccountResponse>(Error.Conflict("LoanAccount.ApplicationAlreadyConverted", "This loan application already has a loan account."));
        }

        foreach (var (accountId, label) in new[] { (command.LoanReceivableAccountId, "Loan Receivable"), (command.InterestIncomeAccountId, "Interest Income"), (command.PenaltyIncomeAccountId, "Penalty Income") })
        {
            if (accountId is null) continue;
            var accountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == accountId, cancellationToken);
            if (!accountExists)
                return Result.Failure<LoanAccountResponse>(Error.NotFound("GlAccount.NotFound", $"{label} account with ID '{accountId}' was not found."));
        }

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "LOAN-ACCT"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<LoanAccountResponse>(numberResult.Error!);

        var scheduleDrafts = AmortizationScheduleGenerator.Generate(command.PrincipalAmount, product.AnnualInterestRatePct, command.TermMonths, product.RepaymentFrequency, command.FirstDueDate);

        var account = new LoanAccount
        {
            LoanAcctNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            CustomerId = command.CustomerId,
            LoanProductId = command.LoanProductId,
            LoanApplicationId = command.LoanApplicationId,
            PrincipalAmount = command.PrincipalAmount,
            AnnualInterestRatePct = product.AnnualInterestRatePct,
            TermMonths = command.TermMonths,
            RepaymentFrequency = product.RepaymentFrequency,
            GracePeriodDays = product.GracePeriodDays,
            PenaltyRatePct = product.PenaltyRatePct,
            GrantDate = command.GrantDate,
            FirstDueDate = command.FirstDueDate,
            Status = "PENDING_DISBURSEMENT",
            Remarks = command.Remarks,
            LoanReceivableAccountId = command.LoanReceivableAccountId ?? product.LoanReceivableAccountId,
            InterestIncomeAccountId = command.InterestIncomeAccountId ?? product.InterestIncomeAccountId,
            PenaltyIncomeAccountId = command.PenaltyIncomeAccountId ?? product.PenaltyIncomeAccountId,
            CreatedBy = command.CreatedBy,
            ScheduleLines = scheduleDrafts.Select(s => new LoanAmortizationScheduleLine
            {
                InstallmentNo = s.InstallmentNo,
                DueDate = s.DueDate,
                PrincipalDue = s.PrincipalDue,
                InterestDue = s.InterestDue,
                TotalDue = s.PrincipalDue + s.InterestDue,
                OutstandingPrincipalAfter = s.OutstandingPrincipalAfter,
                PrincipalPaid = 0,
                InterestPaid = 0,
                Status = "DUE"
            }).ToList()
        };

        dbContext.LoanAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.LoanAccounts
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.LoanApplication)
            .Include(a => a.ScheduleLines)
            .FirstAsync(a => a.Id == account.Id, cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_ACCOUNT", account.Id.ToString(), account.BranchId, "CREATED", "ACTIVITY",
                "created this loan account", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanAccountResponse>(notifyResult.Error!);

        return Result.Success(LoanAccountMapper.ToResponse(saved));
    }
}
