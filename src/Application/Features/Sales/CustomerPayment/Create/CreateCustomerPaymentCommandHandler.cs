namespace ZARI.Application.Features.Sales.CustomerPayments.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Post;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Features.Sales.CustomerPayments.Shared;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Every line applies a portion of this payment to a single POSTED or PARTIALLY_PAID Sales
/// Invoice for the same customer — from a partial amount up to that invoice's remaining balance.
/// Multiple lines let one payment clear several invoices in one cash/bank receipt. When
/// Company.CustomerPaymentQuickPostEnabled is on, this posts straight to POSTED via
/// CustomerPaymentPostingService — the same engine ApproveCustomerPaymentCommandHandler calls.
/// </summary>
public sealed class CreateCustomerPaymentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<PostGlJournalCommand, Result<GlJournalResponse>> postGlJournalHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateCustomerPaymentCommand, Result<CustomerPaymentResponse>>
{
    public async Task<Result<CustomerPaymentResponse>> HandleAsync(CreateCustomerPaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMER_PAYMENTS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<CustomerPaymentResponse>(Error.Forbidden("CustomerPayment.Forbidden", "You do not have permission to create customer payments for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == command.CustomerId, cancellationToken);
        if (customer is null)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("Customer.NotFound", $"Customer with ID '{command.CustomerId}' was not found."));

        var cashAccount = await dbContext.GlAccounts.FirstOrDefaultAsync(a => a.Id == command.CashAccountId, cancellationToken);
        if (cashAccount is null)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.CashAccountId}' was not found."));

        var invoiceIds = command.Lines.Select(l => l.SalesInvoiceId).ToList();
        if (invoiceIds.Distinct().Count() != invoiceIds.Count)
            return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.DuplicateInvoice", "The same sales invoice cannot appear twice on one payment."));

        var invoices = await dbContext.SalesInvoices
            .Include(i => i.Lines).ThenInclude(l => l.StatutoryDiscountType)
            .Where(i => invoiceIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);
        if (invoices.Count != invoiceIds.Count)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("SalesInvoice.NotFound", "One or more sales invoices on this payment were not found."));

        // Friendly, non-authoritative check — CustomerPaymentPostingService re-checks this
        // authoritatively (against every OTHER payment too) right before Approve/quick-post
        // actually posts, closing the race two concurrent drafts could otherwise open.
        foreach (var line in command.Lines)
        {
            var invoice = invoices[line.SalesInvoiceId];

            if (invoice.CustomerId != command.CustomerId)
                return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.CustomerMismatch", $"Sales invoice '{invoice.InvoiceNo}' does not belong to the selected customer."));

            if (invoice.Status is not ("POSTED" or "PARTIALLY_PAID"))
                return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.InvoiceNotPayable", $"Sales invoice '{invoice.InvoiceNo}' is not eligible for payment (status: {invoice.Status})."));

            var invoiceTotal = SalesInvoicePaymentBalance.GetInvoiceTotal(invoice);
            var amountPaid = await SalesInvoicePaymentBalance.GetAmountPaidAsync(dbContext, invoice.Id, cancellationToken);
            var balance = invoiceTotal - amountPaid;
            if (line.AmountApplied > balance)
                return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.AmountExceedsBalance", $"The payment amount for sales invoice '{invoice.InvoiceNo}' cannot exceed its remaining balance of {balance}."));
        }

        if (command.CostCenterId.HasValue && !await dbContext.CostCenters.AnyAsync(c => c.Id == command.CostCenterId.Value, cancellationToken))
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.CostCenterId}' was not found."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "CP"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(numberResult.Error!);

        var company = await dbContext.Companies.FirstOrDefaultAsync(cancellationToken);
        var quickPost = company is { CustomerPaymentQuickPostEnabled: true };

        var payment = new CustomerPayment
        {
            PaymentNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            CustomerId = command.CustomerId,
            PaymentMethod = command.PaymentMethod,
            CashAccountId = command.CashAccountId,
            PaymentDate = command.PaymentDate,
            ReferenceNo = command.ReferenceNo,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CostCenterId = command.CostCenterId,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new CustomerPaymentLine
            {
                SalesInvoiceId = l.SalesInvoiceId,
                AmountApplied = l.AmountApplied
            }).ToList()
        };

        dbContext.CustomerPayments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        payment.Customer = customer;
        payment.CashAccount = cashAccount;
        foreach (var line in payment.Lines)
            line.SalesInvoice = invoices[line.SalesInvoiceId];

        if (quickPost)
        {
            var postResult = await CustomerPaymentPostingService.PostAsync(dbContext, postGlJournalHandler, payment, cancellationToken);
            if (!postResult.IsSuccess)
                return Result.Failure<CustomerPaymentResponse>(postResult.Error!);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("CUSTOMER_PAYMENT", payment.Id.ToString(), payment.BranchId, "CREATED", "ACTIVITY",
                quickPost ? "created this customer payment (posted directly — quick-post enabled)" : "created this customer payment",
                command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(notifyResult.Error!);

        return Result.Success(CustomerPaymentMapper.ToResponse(payment));
    }
}
