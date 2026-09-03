namespace ZARI.Application.Features.Sales.CustomerPayments.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Features.Sales.CustomerPayments.Shared;
using ZARI.Application.Features.Sales.SalesInvoices.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// DRAFT-only edit. The customer is immutable once set — every line's invoice must belong to it —
/// but the cash/bank account, method, date, reference, remarks, and the invoice lines themselves
/// can change.
/// </summary>
public sealed class UpdateCustomerPaymentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateCustomerPaymentCommand, Result<CustomerPaymentResponse>>
{
    public async Task<Result<CustomerPaymentResponse>> HandleAsync(UpdateCustomerPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.CustomerPayments
            .Include(p => p.Customer)
            .Include(p => p.Lines)
            .Include(p => p.Tenders).ThenInclude(t => t.PaymentMethod)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<CustomerPaymentResponse>(Error.NotFound("CustomerPayment.NotFound", $"Customer payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMER_PAYMENTS", FormAction.Edit, payment.BranchId, cancellationToken))
            return Result.Failure<CustomerPaymentResponse>(Error.Forbidden("CustomerPayment.Forbidden", "You do not have permission to update customer payments for this branch."));

        if (payment.Status != "DRAFT")
            return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.NotDraft", "Only draft customer payments can be edited."));

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

        foreach (var line in command.Lines)
        {
            var invoice = invoices[line.SalesInvoiceId];

            if (invoice.CustomerId != payment.CustomerId)
                return Result.Failure<CustomerPaymentResponse>(Error.Validation("CustomerPayment.CustomerMismatch", $"Sales invoice '{invoice.InvoiceNo}' does not belong to this payment's customer."));

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

        payment.PaymentMethod = command.PaymentMethod;
        payment.CashAccountId = command.CashAccountId;
        payment.PaymentDate = command.PaymentDate;
        payment.ReferenceNo = command.ReferenceNo;
        payment.Remarks = command.Remarks;
        payment.CostCenterId = command.CostCenterId;

        payment.Lines.Clear();
        foreach (var line in command.Lines)
        {
            payment.Lines.Add(new CustomerPaymentLine
            {
                SalesInvoiceId = line.SalesInvoiceId,
                AmountApplied = line.AmountApplied
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        payment.CashAccount = cashAccount;
        foreach (var line in payment.Lines)
            line.SalesInvoice = invoices[line.SalesInvoiceId];

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("CUSTOMER_PAYMENT", payment.Id.ToString(), payment.BranchId, "UPDATED", "ACTIVITY",
                "updated this customer payment", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<CustomerPaymentResponse>(notifyResult.Error!);

        return Result.Success(CustomerPaymentMapper.ToResponse(payment));
    }
}
