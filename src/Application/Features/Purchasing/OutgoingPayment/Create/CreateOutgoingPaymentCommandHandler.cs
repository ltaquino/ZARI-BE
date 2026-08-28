namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.Shared;
using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Application.Features.Purchasing.OutgoingPayments.Shared;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Every line pays down a single POSTED or PARTIALLY_PAID AP Invoice — the amount can be anything
/// from a partial payment up to that invoice's remaining balance, not just its full total. Multiple
/// lines let one payment clear several bills to the same supplier in one bank/cash transaction (a
/// payment run).
/// </summary>
public sealed class CreateOutgoingPaymentCommandHandler(
    IAppDbContext dbContext,
    ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> nextDocumentNumberHandler,
    ICommandHandler<CreateNotificationCommand, Result<NotificationResponse>> createNotificationHandler,
    IPermissionService permissionService)
    : ICommandHandler<CreateOutgoingPaymentCommand, Result<OutgoingPaymentResponse>>
{
    public async Task<Result<OutgoingPaymentResponse>> HandleAsync(CreateOutgoingPaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("OUTGOING_PAYMENTS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<OutgoingPaymentResponse>(Error.Forbidden("OutgoingPayment.Forbidden", "You do not have permission to create outgoing payments for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == command.SupplierId, cancellationToken);
        if (supplier is null)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("Supplier.NotFound", $"Supplier with ID '{command.SupplierId}' was not found."));

        var bankAccount = await dbContext.BankAccounts.FirstOrDefaultAsync(b => b.Id == command.BankAccountId, cancellationToken);
        if (bankAccount is null)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("BankAccount.NotFound", $"Bank account with ID '{command.BankAccountId}' was not found."));

        var invoiceIds = command.Lines.Select(l => l.ApInvoiceId).ToList();
        if (invoiceIds.Distinct().Count() != invoiceIds.Count)
            return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.DuplicateInvoice", "The same AP invoice cannot appear twice on one payment."));

        var invoices = await dbContext.ApInvoices
            .Include(i => i.Lines)
            .Include(i => i.ExpenseLines)
            .Where(i => invoiceIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);
        if (invoices.Count != invoiceIds.Count)
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("ApInvoice.NotFound", "One or more AP invoices on this payment were not found."));

        foreach (var line in command.Lines)
        {
            var invoice = invoices[line.ApInvoiceId];

            if (invoice.SupplierId != command.SupplierId)
                return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.SupplierMismatch", $"AP invoice '{invoice.InvoiceNo}' does not belong to the selected supplier."));

            if (invoice.Status is not ("POSTED" or "PARTIALLY_PAID"))
                return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.InvoiceNotPayable", $"AP invoice '{invoice.InvoiceNo}' is not eligible for payment (status: {invoice.Status})."));

            var invoiceTotal = ApInvoicePaymentBalance.GetInvoiceTotal(invoice);
            var amountPaid = await ApInvoicePaymentBalance.GetAmountPaidAsync(dbContext, invoice.Id, cancellationToken);
            var balance = invoiceTotal - amountPaid;
            if (line.Amount > balance)
                return Result.Failure<OutgoingPaymentResponse>(Error.Validation("OutgoingPayment.AmountExceedsBalance", $"The payment amount for AP invoice '{invoice.InvoiceNo}' cannot exceed its remaining balance of {balance}."));
        }

        if (command.CostCenterId.HasValue && !await dbContext.CostCenters.AnyAsync(c => c.Id == command.CostCenterId.Value, cancellationToken))
            return Result.Failure<OutgoingPaymentResponse>(Error.NotFound("CostCenter.NotFound", $"Cost center with ID '{command.CostCenterId}' was not found."));

        var numberResult = await nextDocumentNumberHandler.HandleAsync(new GetNextDocumentNumberCommand(command.BranchId, "OP"), cancellationToken);
        if (!numberResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(numberResult.Error!);

        var payment = new OutgoingPayment
        {
            PaymentNo = numberResult.Value!.DocumentNumber,
            BranchId = command.BranchId,
            SupplierId = command.SupplierId,
            BankAccountId = command.BankAccountId,
            PaymentDate = command.PaymentDate,
            RefNo = command.RefNo,
            Status = "DRAFT",
            Remarks = command.Remarks,
            CostCenterId = command.CostCenterId,
            CreatedBy = command.CreatedBy,
            Lines = command.Lines.Select(l => new OutgoingPaymentLine
            {
                ApInvoiceId = l.ApInvoiceId,
                Amount = l.Amount
            }).ToList()
        };

        dbContext.OutgoingPayments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        payment.Supplier = supplier;
        payment.BankAccount = bankAccount;
        foreach (var line in payment.Lines)
            line.ApInvoice = invoices[line.ApInvoiceId];

        var notifyResult = await createNotificationHandler.HandleAsync(
            new CreateNotificationCommand("OUTGOING_PAYMENT", payment.Id.ToString(), payment.BranchId, "CREATED", "ACTIVITY",
                "created this outgoing payment", command.CreatedBy),
            cancellationToken);
        if (!notifyResult.IsSuccess)
            return Result.Failure<OutgoingPaymentResponse>(notifyResult.Error!);

        return Result.Success(OutgoingPaymentMapper.ToResponse(payment));
    }
}
