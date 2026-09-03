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
        // POS_MODE:Create is an alternate, equally-real grant for this one action — same reasoning
        // as CreateSalesInvoiceCommandHandler's own check: a cashier role can hold POS_MODE without
        // full back-office CUSTOMER_PAYMENTS access, and both checks are real permission lookups
        // against the authenticated caller, never a client-suppliable flag.
        var canCreate = await permissionService.HasPermissionOnBranchAsync("CUSTOMER_PAYMENTS", FormAction.Create, command.BranchId, cancellationToken)
            || await permissionService.HasPermissionOnBranchAsync("POS_MODE", FormAction.Create, command.BranchId, cancellationToken);
        if (!canCreate)
            return Result.Failure<CustomerPaymentResponse>(Error.Forbidden("CustomerPayment.Forbidden", "You do not have permission to create customer payments for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == command.CustomerId, cancellationToken);
        if (customer is null)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("Customer.NotFound", $"Customer with ID '{command.CustomerId}' was not found."));

        // Resolve the funding side first: either the caller's own Tenders (POS Mode's split-tender
        // shape — PaymentMethod/CashAccountId are then derived from it) or the original single-
        // method fields (Wave 4's shape, unchanged). The validator already guarantees one of these
        // two paths has what it needs.
        Dictionary<Guid, PaymentMethod>? paymentMethodsById = null;
        string resolvedPaymentMethod;
        Guid resolvedCashAccountId;

        if (command.Tenders is { Count: > 0 })
        {
            var methodIds = command.Tenders.Select(t => t.PaymentMethodId).Distinct().ToList();
            paymentMethodsById = await dbContext.PaymentMethods.Where(m => methodIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, cancellationToken);
            if (paymentMethodsById.Count != methodIds.Count)
                return Result.Failure<CustomerPaymentResponse>(Error.NotFound("CustomerPayment.PaymentMethodNotFound", "One or more payment methods on this payment were not found."));

            var tenderTotal = command.Tenders.Sum(t => t.Amount);
            var allocatedTotal = command.Lines.Sum(l => l.AmountApplied);
            if (Math.Round(tenderTotal, 4) != Math.Round(allocatedTotal, 4))
                return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.TenderMismatch", $"Total tendered ({tenderTotal}) must equal the total amount applied to invoices ({allocatedTotal})."));

            foreach (var tender in command.Tenders)
            {
                var method = paymentMethodsById[tender.PaymentMethodId];
                if (method.RequiresReferenceNo && string.IsNullOrWhiteSpace(tender.ReferenceNo))
                    return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.ReferenceNoRequired", $"'{method.ReferenceNoLabel ?? "Reference number"}' is required for {method.Name}."));
                if (method.RequiresBankOrPartnerName && string.IsNullOrWhiteSpace(tender.BankOrPartnerName))
                    return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.BankOrPartnerNameRequired", $"Bank/partner name is required for {method.Name}."));
            }

            var distinctMethodNames = command.Tenders.Select(t => paymentMethodsById[t.PaymentMethodId].Name).Distinct().ToList();
            resolvedPaymentMethod = command.PaymentMethod ?? (distinctMethodNames.Count == 1 ? distinctMethodNames[0] : "MIXED");
            resolvedCashAccountId = command.CashAccountId ?? paymentMethodsById[command.Tenders[0].PaymentMethodId].GlAccountId;
        }
        else
        {
            resolvedPaymentMethod = command.PaymentMethod!;
            resolvedCashAccountId = command.CashAccountId!.Value;
        }

        var cashAccount = await dbContext.GlAccounts.FirstOrDefaultAsync(a => a.Id == resolvedCashAccountId, cancellationToken);
        if (cashAccount is null)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{resolvedCashAccountId}' was not found."));

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
        var quickPost = company is { CustomerPaymentQuickPostEnabled: true } || command.ForceQuickPost;

        var payment = new CustomerPayment
        {
            PaymentNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            CustomerId = command.CustomerId,
            PaymentMethod = resolvedPaymentMethod,
            CashAccountId = resolvedCashAccountId,
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
            }).ToList(),
            Tenders = (command.Tenders ?? []).Select(t => new CustomerPaymentTender
            {
                PaymentMethodId = t.PaymentMethodId,
                Amount = t.Amount,
                ReferenceNo = t.ReferenceNo,
                BankOrPartnerName = t.BankOrPartnerName
            }).ToList()
        };

        dbContext.CustomerPayments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        payment.Customer = customer;
        payment.CashAccount = cashAccount;
        foreach (var line in payment.Lines)
            line.SalesInvoice = invoices[line.SalesInvoiceId];
        if (paymentMethodsById is not null)
            foreach (var tender in payment.Tenders)
                tender.PaymentMethod = paymentMethodsById[tender.PaymentMethodId];

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
