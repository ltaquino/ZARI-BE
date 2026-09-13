namespace ZARI.Application.UnitTests.TestSupport;

using ZARI.Domain.Entities;

/// <summary>Entity builders for the Sales feature area not already covered by LoanTestFixtures/AccountingTestFixtures/InventoryTestFixtures.</summary>
internal static class SalesTestFixtures
{
    public static ItemCategory ItemCategory(string code = "CAT1", string name = "Test Category") => new()
    {
        Code = code,
        Name = name
    };

    public static DiscountRule DiscountRule(string code = "DISC1", string scope = "ALL", string discountType = "PERCENT", decimal discountValue = 10, string status = "active") => new()
    {
        Code = code,
        Name = "Test Discount",
        Scope = scope,
        DiscountType = discountType,
        DiscountValue = discountValue,
        Priority = 1,
        Status = status
    };

    public static StatutoryDiscountType StatutoryDiscountType(string code = "SENIOR", decimal discountPct = 20, bool isVatExempt = true, string status = "active") => new()
    {
        Code = code,
        Name = "Senior Citizen",
        DiscountPct = discountPct,
        IsVatExempt = isVatExempt,
        RequiredIdLabel = "Senior Citizen ID",
        Status = status
    };

    public static PosPromoSlide PosPromoSlide(string title = "Promo", int displayOrder = 1, string status = "active") => new()
    {
        Title = title,
        DisplayOrder = displayOrder,
        Status = status
    };

    public static PosTerminal PosTerminal(string branchId, string code = "POS1", string status = "active") => new()
    {
        BranchId = branchId,
        Code = code,
        Name = "Test Terminal",
        Status = status
    };

    public static SalesOrder SalesOrder(string branchId, Guid customerId, Guid itemId, Guid uomId, string status = "DRAFT", decimal qty = 10, decimal unitPrice = 100) => new()
    {
        SoNo = $"SO-{Guid.NewGuid():N}",
        BranchId = branchId,
        CustomerId = customerId,
        OrderDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new SalesOrderLine { ItemId = itemId, Qty = qty, UomId = uomId, UnitPrice = unitPrice }]
    };

    public static DeliveryOrder DeliveryOrder(string branchId, Guid warehouseId, Guid customerId, Guid itemId, Guid uomId, string status = "DRAFT", decimal qty = 10, decimal unitCost = 40, Guid? salesOrderId = null, Guid? salesOrderLineId = null) => new()
    {
        DoNo = $"DO-{Guid.NewGuid():N}",
        BranchId = branchId,
        WarehouseId = warehouseId,
        CustomerId = customerId,
        SalesOrderId = salesOrderId,
        DeliveryDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new DeliveryOrderLine { ItemId = itemId, QtyShipped = qty, UomId = uomId, UnitCost = unitCost, SalesOrderLineId = salesOrderLineId }]
    };

    public static SalesReturn SalesReturn(string branchId, Guid warehouseId, Guid customerId, Guid itemId, Guid uomId, string status = "DRAFT", decimal qty = 5, decimal unitPrice = 100, Guid? deliveryOrderId = null, Guid? deliveryOrderLineId = null) => new()
    {
        ReturnNo = $"SRTN-{Guid.NewGuid():N}",
        BranchId = branchId,
        WarehouseId = warehouseId,
        CustomerId = customerId,
        DeliveryOrderId = deliveryOrderId,
        ReturnDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new SalesReturnLine { ItemId = itemId, QtyReturned = qty, UomId = uomId, UnitPrice = unitPrice, DeliveryOrderLineId = deliveryOrderLineId }]
    };

    public static SalesInvoice SalesInvoice(string branchId, Guid customerId, Guid itemId, Guid uomId, string status = "POSTED", decimal qty = 1, decimal unitPrice = 100, Guid? deliveryOrderId = null, Guid? deliveryOrderLineId = null) => new()
    {
        InvoiceNo = $"SINV-{Guid.NewGuid():N}",
        BranchId = branchId,
        CustomerId = customerId,
        DeliveryOrderId = deliveryOrderId,
        InvoiceDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new SalesInvoiceLine { ItemId = itemId, Qty = qty, UomId = uomId, UnitPrice = unitPrice, DeliveryOrderLineId = deliveryOrderLineId }]
    };

    public static CustomerPayment CustomerPayment(string branchId, Guid customerId, Guid cashAccountId, Guid salesInvoiceId, string status = "DRAFT", decimal amount = 100) => new()
    {
        PaymentNo = $"CRV-{Guid.NewGuid():N}",
        BranchId = branchId,
        CustomerId = customerId,
        PaymentMethod = "CASH",
        CashAccountId = cashAccountId,
        PaymentDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new CustomerPaymentLine { SalesInvoiceId = salesInvoiceId, AmountApplied = amount }]
    };
}
