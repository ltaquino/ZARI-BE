namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Shared;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// DRAFT-only edit. The supplier is immutable once set — every line's invoice must belong to it —
/// but the bank/cash account, date, reference, remarks, and the invoice lines themselves can change.
/// </summary>
public sealed class UpdateOutgoingPaymentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<UpdateOutgoingPaymentCommand, Result<OutgoingPaymentResponse>>
{
    public async Task<Result<OutgoingPaymentResponse>> HandleAsync(UpdateOutgoingPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.OutgoingPayments
            .Include(p => p.Supplier)
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (payment is null)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("OutgoingPayment.NotFound", $"Outgoing payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("OUTGOING_PAYMENTS", FormAction.Edit, payment.BranchId, cancellationToken))
            return Result.Failure<OutgoingPaymentResponse>(Error.Forbidden("OutgoingPayment.Forbidden", "You do not have permission to update outgoing payments for this branch."));

        if (payment.Status != "DRAFT")
            return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.NotDraft", "Only draft outgoing payments can be edited."));

        var bankAccount = await dbContext.BankAccounts.FirstOrDefaultAsync(b => b.Id == command.BankAccountId, cancellationToken);
        if (bankAccount is null)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("BankAccount.NotFound", $"Bank account with ID '{command.BankAccountId}' was not found."));

        var invoiceIds = command.Lines.Select(l => l.ApInvoiceId).ToList();
        if (invoiceIds.Distinct().Count() != invoiceIds.Count)
            return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.DuplicateInvoice", "The same AP invoice cannot appear twice on one payment."));

        var invoices = await dbContext.ApInvoices
            .Include(i => i.Lines)
            .Where(i => invoiceIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);
        if (invoices.Count != invoiceIds.Count)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("ApInvoice.NotFound", "One or more AP invoices on this payment were not found."));

        foreach (var line in command.Lines)
        {
            var invoice = invoices[line.ApInvoiceId];

            if (invoice.SupplierId != payment.SupplierId)
                return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.SupplierMismatch", $"AP invoice '{invoice.InvoiceNo}' does not belong to this payment's supplier."));

            if (invoice.Status is not ("POSTED" or "PARTIALLY_PAID"))
                return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.InvoiceNotPayable", $"AP invoice '{invoice.InvoiceNo}' is not eligible for payment (status: {invoice.Status})."));

            var invoiceTotal = invoice.Lines.Sum(l => Math.Round(l.Qty * l.UnitCost, 4));
            var amountPaid = await ApInvoicePaymentBalance.GetAmountPaidAsync(dbContext, invoice.Id, cancellationToken);
            var balance = invoiceTotal - amountPaid;
            if (line.Amount > balance)
                return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.AmountExceedsBalance", $"The payment amount for AP invoice '{invoice.InvoiceNo}' cannot exceed its remaining balance of {balance}."));
        }

        payment.BankAccountId = command.BankAccountId;
        payment.PaymentDate = command.PaymentDate;
        payment.RefNo = command.RefNo;
        payment.Remarks = command.Remarks;

        payment.Lines.Clear();
        foreach (var line in command.Lines)
        {
            payment.Lines.Add(new OutgoingPaymentLine
            {
                ApInvoiceId = line.ApInvoiceId,
                Amount = line.Amount
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        payment.BankAccount = bankAccount;
        foreach (var line in payment.Lines)
            line.ApInvoice = invoices[line.ApInvoiceId];

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("OUTGOING_PAYMENT", payment.Id.ToString(), payment.BranchId, "UPDATED", "ACTIVITY",
                "updated this outgoing payment", command.UpdatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(notifyResult.Error!);

        return Result.Success(OutgoingPaymentMapper.ToResponse(payment));
    }
}
