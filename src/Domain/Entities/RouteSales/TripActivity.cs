namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A stock movement a trip makes between warehouses of its own branch: LOAD_OUT (source → truck),
/// LOAD_IN (truck → source, good stock), DAMAGED (truck → branch Damaged warehouse), SHORTAGE
/// (out of the truck, nothing received) and OVERAGE (into the truck from nowhere). Posted by
/// TripStockMovePostingService; each line's ledger entries are keyed by the line id, so a retried
/// post never moves stock twice.
/// </summary>
public sealed class TripStockMove : AuditableEntity
{
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;

    public string MoveType { get; set; } = default!;
    public Guid? FromWarehouseId { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset? PostedAt { get; set; }

    public List<TripStockMoveLine> Lines { get; set; } = [];
}

public sealed class TripStockMoveLine : BaseEntity
{
    public Guid TripStockMoveId { get; set; }
    public TripStockMove TripStockMove { get; set; } = default!;
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public decimal Qty { get; set; }

    /// <summary>The costing engine's unit cost from the issue side, carried to the receive side.</summary>
    public decimal UnitCost { get; set; }
}

/// <summary>
/// One invoice issued on the road — the idempotency + progress record for RecordRouteSaleCommand.
/// Created first, keyed by (PosTerminalId, BirOrSeriesNumber); each step (delivery, invoice,
/// payment) stores its document id as it completes, so a retry after a crash or a dropped
/// connection resumes where the last attempt stopped instead of booking anything twice.
/// Status: PENDING → COMPLETED, or VOID (a number printed then voided at the door).
/// </summary>
public sealed class RouteSale : AuditableEntity
{
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;

    public Guid PosTerminalId { get; set; }
    public string BirOrSeriesNumber { get; set; } = default!;

    /// <summary>A manual backup-booklet invoice (printer down / device lost) rather than machine-issued.</summary>
    public bool IsManualInvoice { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? SalesOrderId { get; set; }

    /// <summary>The request exactly as first received — a resumed attempt replays this, not the retry's body.</summary>
    public string RequestJson { get; set; } = default!;

    public Guid? DeliveryOrderId { get; set; }
    public Guid? SalesInvoiceId { get; set; }
    public Guid? CustomerPaymentId { get; set; }

    public string Status { get; set; } = default!;
    public decimal InvoiceTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal CashCollected { get; set; }
    public string? VoidReason { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset SoldAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // Proof of delivery.
    public string? ReceivedByName { get; set; }
    public string? SignatureFilePath { get; set; }
    public string? PhotoFilePath { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

/// <summary>A customer return taken onto the truck — posts a Sales Return into the truck warehouse.
/// Idempotent on ClientRef (the handheld's own id for it).</summary>
public sealed class TripReturn : AuditableEntity
{
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;
    public Guid ClientRef { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public Guid? SalesReturnId { get; set; }
    public string Status { get; set; } = default!;
    public string? Reason { get; set; }
    public DateTimeOffset ReturnedAt { get; set; }
    public List<TripReturnLine> Lines { get; set; } = [];
}

public sealed class TripReturnLine : BaseEntity
{
    public Guid TripReturnId { get; set; }
    public TripReturn TripReturn { get; set; } = default!;
    public Guid ItemId { get; set; }
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Damaged goods ride back on the truck but go to the branch Damaged warehouse at close.</summary>
    public bool IsDamaged { get; set; }
}

/// <summary>Payment collected on the road against a customer's open invoices. Idempotent on ClientRef.</summary>
public sealed class TripCollection : AuditableEntity
{
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;
    public Guid ClientRef { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public Guid? CustomerPaymentId { get; set; }
    public decimal Amount { get; set; }
    public decimal CashAmount { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset CollectedAt { get; set; }
}

/// <summary>
/// Ordered quantity not delivered at a stop, and why — the customer refused all or part of it, or only
/// part could be received. Recorded with the route sale for a partial delivery (RouteSaleId set,
/// ClientRef = the route sale's id), or on its own when nothing was accepted (no invoice number used).
/// No stock moves: the goods never left the truck. The sales order line stays open (Undelivered report).
/// Idempotent on ClientRef.
/// </summary>
public sealed class TripRefusal : AuditableEntity
{
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;
    public Guid ClientRef { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = default!;
    public Guid? RouteSaleId { get; set; }
    public DateTimeOffset RefusedAt { get; set; }
    public List<TripRefusalLine> Lines { get; set; } = [];
}

public sealed class TripRefusalLine : BaseEntity
{
    public Guid TripRefusalId { get; set; }
    public TripRefusal TripRefusal { get; set; } = default!;
    public Guid SalesOrderLineId { get; set; }
    public Guid ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public decimal Qty { get; set; }

    /// <summary>One of TripRefusalReason's codes; Notes are required for OTHER.</summary>
    public string ReasonCode { get; set; } = default!;
    public string? Notes { get; set; }
}

/// <summary>Money the driver paid in against a DRIVER_PAYS shortage — Dr the method's account, Cr Due from Employees.</summary>
public sealed class TripShortagePayment : AuditableEntity
{
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;
    public decimal Amount { get; set; }
    public Guid PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = default!;
    public DateTimeOffset PaidAt { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Remarks { get; set; }
}
