namespace ZARI.Application.Features.Sales.CustomerPayments.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Domain.Common;

public sealed record CustomerPaymentLineInput(Guid SalesInvoiceId, decimal AmountApplied);

/// <summary>One split-tender funding line — see CustomerPaymentTender's own doc comment.</summary>
public sealed record CustomerPaymentTenderInput(Guid PaymentMethodId, decimal Amount, string? ReferenceNo, string? BankOrPartnerName);

public sealed record CreateCustomerPaymentCommand(
    string BranchId,
    Guid CustomerId,
    DateTimeOffset PaymentDate,
    string? Remarks,
    Guid? CostCenterId,
    string? CreatedBy,
    List<CustomerPaymentLineInput> Lines,
    // The original (Wave 4) single-method shape — required unless Tenders below is used instead.
    string? PaymentMethod = null,
    Guid? CashAccountId = null,
    string? ReferenceNo = null,
    // POS Mode's split-tender shape — when non-empty, PaymentMethod/CashAccountId above are
    // auto-derived server-side (first tender's method/account, or "MIXED" if more than one) rather
    // than required from the caller. See CustomerPaymentPostingService.PostPaymentJournalAsync for
    // how this drives the actual GL split.
    List<CustomerPaymentTenderInput>? Tenders = null,
    // POS Mode's own checkout call (CreatePosSaleCommand) sets this — see the identical flag on
    // CreateSalesInvoiceCommand for why (immediate posting regardless of the Company toggle).
    bool ForceQuickPost = false) : ICommand<Result<CustomerPaymentResponse>>;
