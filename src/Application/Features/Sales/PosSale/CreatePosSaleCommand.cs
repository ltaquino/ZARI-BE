namespace ZARI.Application.Features.Sales.PosSale;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Application.Features.Sales.SalesInvoices.Create;
using ZARI.Domain.Common;

/// <summary>
/// One POS checkout: creates and immediately posts a Sales Invoice, then creates and immediately
/// posts the Customer Payment that fully settles it — the two-step composition documented on
/// CreatePosSaleCommandHandler. Lines/Tenders reuse the exact same input shapes the regular admin
/// forms use (SalesInvoiceLineInput/CustomerPaymentTenderInput), not a parallel POS-only schema.
/// </summary>
public sealed record CreatePosSaleCommand(
    string BranchId,
    Guid PosTerminalId,
    // Null defaults to the branch's seeded "Walk-in Customer" — see the handler.
    Guid? CustomerId,
    DateTimeOffset InvoiceDate,
    decimal? DiscountPct,
    Guid? CostCenterId,
    List<SalesInvoiceLineInput> Lines,
    // As literally tendered at the register (e.g. a 500 cash tender against a 450 total) — the
    // handler computes change and caps the CASH tender(s) down to what's actually kept as AR
    // settlement before handing off to CreateCustomerPaymentCommand, which requires tenders to sum
    // to exactly the invoice total.
    List<CustomerPaymentTenderInput> Tenders,
    string? CreatedBy) : ICommand<Result<PosSaleResponse>>;

public sealed record PosSaleResponse(
    Guid SalesInvoiceId,
    string InvoiceNo,
    string? BirOrSeriesNumber,
    decimal InvoiceTotal,
    Guid CustomerPaymentId,
    string PaymentNo,
    decimal ChangeDue);
