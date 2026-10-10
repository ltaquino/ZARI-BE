namespace ZARI.Application.Features.RouteSales.Trips.RouteSales;

using FluentValidation;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.RouteSales.Trips.Shared;
using ZARI.Application.Features.Sales.CustomerPayments.Create;
using ZARI.Domain.Common;

/// <summary>
/// One line of a sale at a stop: against an assigned sales order line (SalesOrderLineId set) or a
/// van sale of extra stock (null). VatType defaults to the item's own.
/// </summary>
public sealed record RouteSaleLineInput(
    Guid ItemId,
    decimal Qty,
    decimal UnitPrice,
    decimal DiscountPct,
    Guid? SalesOrderLineId = null,
    string? VatType = null,
    Guid? StatutoryDiscountTypeId = null,
    string? StatutoryIdNumber = null);

/// <summary>
/// An invoice the truck handheld issued at a stop, offline: delivery out of the truck warehouse +
/// Sales Invoice carrying the handheld's own BIR number + (if anything was tendered) payment.
/// Idempotent on (truck terminal, BirOrSeriesNumber): re-sending the same sale returns the recorded
/// result, and a retry after a partial failure resumes instead of booking anything twice.
/// Tenders may be short of the total — the rest stays on the customer's account (charge sale),
/// within their credit limit — or over it, with the change coming out of one cash tender.
/// Against a sales order, every quantity still open on this trip must be either delivered or listed in
/// Refusals with a reason (customer refused part of it, or only part could be received).
/// </summary>
public sealed record RecordRouteSaleCommand(
    Guid TripId,
    string BirOrSeriesNumber,
    Guid CustomerId,
    Guid? SalesOrderId,
    DateTimeOffset SoldAt,
    decimal? DiscountPct,
    List<RouteSaleLineInput> Lines,
    List<CustomerPaymentTenderInput> Tenders,
    string? ReceivedByName = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    List<TripRefusalLineInput>? Refusals = null) : ICommand<Result<RouteSaleResponse>>;

public sealed record RouteSaleResponse(
    Guid Id,
    string Status,
    string BirOrSeriesNumber,
    Guid? DeliveryOrderId,
    Guid? SalesInvoiceId,
    Guid? CustomerPaymentId,
    decimal InvoiceTotal,
    decimal AmountPaid,
    decimal ChangeDue,
    decimal BalanceDue);

public sealed class RecordRouteSaleValidator : AbstractValidator<RecordRouteSaleCommand>
{
    public RecordRouteSaleValidator()
    {
        RuleFor(x => x.BirOrSeriesNumber).NotEmpty().MaximumLength(25);
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.SoldAt).NotEmpty();
        RuleFor(x => x.DiscountPct).InclusiveBetween(0, 100).When(x => x.DiscountPct.HasValue);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("A sale needs at least one item.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.ItemId).NotEmpty();
            l.RuleFor(x => x.Qty).GreaterThan(0);
            l.RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.DiscountPct).InclusiveBetween(0, 100);
            l.RuleFor(x => x.StatutoryIdNumber).NotEmpty().When(x => x.StatutoryDiscountTypeId.HasValue)
                .WithMessage("A statutory discount needs the customer's ID number.");
        });
        RuleFor(x => x.Tenders).NotNull();
        RuleForEach(x => x.Tenders).ChildRules(t =>
        {
            t.RuleFor(x => x.PaymentMethodId).NotEmpty();
            t.RuleFor(x => x.Amount).GreaterThan(0);
        });
        RuleFor(x => x.ReceivedByName).MaximumLength(150);
        RuleForEach(x => x.Refusals).ChildRules(TripRefusals.LineRules);
    }
}
