namespace ZARI.Application.Features.Loan.LoanAccounts.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanAccounts.GetAll;
using ZARI.Application.Features.Loan.LoanAccounts.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Only while PENDING_DISBURSEMENT — once a loan is ACTIVE its schedule is a live financial record,
/// not something an edit should silently regenerate. Any principal/term/date change here
/// regenerates the whole schedule from scratch, same as it was generated at Create.
/// </summary>
public sealed class UpdateLoanAccountCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateLoanAccountCommand, Result<LoanAccountResponse>>
{
    public async Task<Result<LoanAccountResponse>> HandleAsync(UpdateLoanAccountCommand command, CancellationToken cancellationToken = default)
    {
        var account = await dbContext.LoanAccounts
            .Include(a => a.ScheduleLines)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (account is null)
            return Result.Failure<LoanAccountResponse>(Error.NotFound("LoanAccount.NotFound", $"Loan account with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("LOAN_ACCOUNTS", FormAction.Edit, account.BranchId, cancellationToken))
            return Result.Failure<LoanAccountResponse>(Error.Forbidden("LoanAccount.Forbidden", "You do not have permission to edit this loan account for this branch."));

        if (account.Status != "PENDING_DISBURSEMENT")
            return Result.Failure<LoanAccountResponse>(Error.Validation("LoanAccount.NotEditable", "Only a loan account still pending disbursement can be edited."));

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

        foreach (var (accountId, label) in new[] { (command.LoanReceivableAccountId, "Loan Receivable"), (command.InterestIncomeAccountId, "Interest Income"), (command.PenaltyIncomeAccountId, "Penalty Income") })
        {
            if (accountId is null) continue;
            var glAccountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == accountId, cancellationToken);
            if (!glAccountExists)
                return Result.Failure<LoanAccountResponse>(Error.NotFound("GlAccount.NotFound", $"{label} account with ID '{accountId}' was not found."));
        }

        account.BranchId = command.BranchId;
        account.CustomerId = command.CustomerId;
        account.LoanProductId = command.LoanProductId;
        account.PrincipalAmount = command.PrincipalAmount;
        account.AnnualInterestRatePct = product.AnnualInterestRatePct;
        account.TermMonths = command.TermMonths;
        account.RepaymentFrequency = product.RepaymentFrequency;
        account.GracePeriodDays = product.GracePeriodDays;
        account.PenaltyRatePct = product.PenaltyRatePct;
        account.GrantDate = command.GrantDate;
        account.FirstDueDate = command.FirstDueDate;
        account.Remarks = command.Remarks;
        account.LoanReceivableAccountId = command.LoanReceivableAccountId ?? product.LoanReceivableAccountId;
        account.InterestIncomeAccountId = command.InterestIncomeAccountId ?? product.InterestIncomeAccountId;
        account.PenaltyIncomeAccountId = command.PenaltyIncomeAccountId ?? product.PenaltyIncomeAccountId;

        var scheduleDrafts = AmortizationScheduleGenerator.Generate(command.PrincipalAmount, product.AnnualInterestRatePct, command.TermMonths, product.RepaymentFrequency, command.FirstDueDate);
        account.ScheduleLines.Clear();
        foreach (var s in scheduleDrafts)
        {
            account.ScheduleLines.Add(new LoanAmortizationScheduleLine
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
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await dbContext.LoanAccounts
            .Include(a => a.Customer)
            .Include(a => a.LoanProduct)
            .Include(a => a.LoanApplication)
            .Include(a => a.ScheduleLines)
            .FirstAsync(a => a.Id == account.Id, cancellationToken);

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("LOAN_ACCOUNT", account.Id.ToString(), account.BranchId, "UPDATED", "ACTIVITY",
                "updated this loan account", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<LoanAccountResponse>(notifyResult.Error!);

        return Result.Success(LoanAccountMapper.ToResponse(saved));
    }
}
