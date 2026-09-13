namespace ZARI.Application.UnitTests.TestSupport;

using ZARI.Domain.Entities;

/// <summary>Entity builders for the Purchasing feature area not already covered by LoanTestFixtures/AccountingTestFixtures/SystemModuleTestFixtures.</summary>
internal static class PurchasingTestFixtures
{
    public static Supplier Supplier(string code = "SUP1", string name = "Test Supplier", string? currencyId = null, Guid? apAccountId = null, string status = "active") => new()
    {
        Code = code,
        Name = name,
        CurrencyId = currencyId,
        ApAccountId = apAccountId,
        Status = status
    };

    public static PurchaseReturnReason PurchaseReturnReason(string code = "DAMAGED", string? description = "Damaged goods", string status = "active") => new()
    {
        Code = code,
        Description = description,
        Status = status
    };

    public static PurchaseRequest PurchaseRequest(string branchId, Guid itemId, Guid uomId, string status = "DRAFT", decimal qty = 10) => new()
    {
        RequestNo = $"PR-{Guid.NewGuid():N}",
        BranchId = branchId,
        RequestDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new PurchaseRequestLine { ItemId = itemId, QtyRequested = qty, UomId = uomId }]
    };

    public static PurchaseOrder PurchaseOrder(string branchId, Guid supplierId, Guid itemId, Guid uomId, string status = "DRAFT", decimal qty = 10, decimal unitCost = 50, Guid? purchaseRequestId = null, Guid? purchaseRequestLineId = null) => new()
    {
        PoNo = $"PO-{Guid.NewGuid():N}",
        BranchId = branchId,
        SupplierId = supplierId,
        OrderDate = DateTimeOffset.UtcNow,
        Status = status,
        PurchaseRequestId = purchaseRequestId,
        Lines = [new PurchaseOrderLine { ItemId = itemId, Qty = qty, UomId = uomId, UnitCost = unitCost, PurchaseRequestLineId = purchaseRequestLineId }]
    };

    public static GoodsReceiptPo GoodsReceiptPo(string branchId, Guid warehouseId, Guid supplierId, Guid itemId, Guid uomId, string status = "DRAFT", decimal qty = 10, decimal unitCost = 50, Guid? purchaseOrderId = null, Guid? purchaseOrderLineId = null) => new()
    {
        GrpoNo = $"GRPO-{Guid.NewGuid():N}",
        BranchId = branchId,
        WarehouseId = warehouseId,
        SupplierId = supplierId,
        PurchaseOrderId = purchaseOrderId,
        ReceiptDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new GoodsReceiptPoLine { ItemId = itemId, QtyReceived = qty, UomId = uomId, UnitCost = unitCost, PurchaseOrderLineId = purchaseOrderLineId }]
    };

    public static GoodsReturn GoodsReturn(string branchId, Guid warehouseId, Guid supplierId, Guid itemId, Guid uomId, string status = "DRAFT", decimal qty = 5, decimal unitCost = 50, string reasonCode = "DAMAGED", Guid? goodsReceiptPoId = null, Guid? goodsReceiptPoLineId = null) => new()
    {
        ReturnNo = $"GRTN-{Guid.NewGuid():N}",
        BranchId = branchId,
        WarehouseId = warehouseId,
        SupplierId = supplierId,
        GoodsReceiptPoId = goodsReceiptPoId,
        ReasonCode = reasonCode,
        ReturnDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new GoodsReturnLine { ItemId = itemId, QtyReturned = qty, UomId = uomId, UnitCost = unitCost, GoodsReceiptPoLineId = goodsReceiptPoLineId }]
    };

    public static ApInvoice ApInvoice(string branchId, Guid supplierId, Guid itemId, Guid uomId, string status = "POSTED", decimal qty = 1, decimal unitCost = 100, Guid? goodsReceiptPoId = null, Guid? goodsReceiptPoLineId = null) => new()
    {
        InvoiceNo = $"APINV-{Guid.NewGuid():N}",
        BranchId = branchId,
        SupplierId = supplierId,
        InvoiceType = "ITEM",
        GoodsReceiptPoId = goodsReceiptPoId,
        SupplierInvoiceNo = $"SINV-{Guid.NewGuid():N}",
        InvoiceDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new ApInvoiceLine { ItemId = itemId, Qty = qty, UomId = uomId, UnitCost = unitCost, GoodsReceiptPoLineId = goodsReceiptPoLineId }]
    };

    public static ApInvoice ApInvoiceExpense(string branchId, Guid supplierId, Guid glAccountId, string status = "DRAFT", decimal amount = 100) => new()
    {
        InvoiceNo = $"APINV-{Guid.NewGuid():N}",
        BranchId = branchId,
        SupplierId = supplierId,
        InvoiceType = "EXPENSE",
        SupplierInvoiceNo = $"SINV-{Guid.NewGuid():N}",
        InvoiceDate = DateTimeOffset.UtcNow,
        Status = status,
        ExpenseLines = [new ApInvoiceExpenseLine { GlAccountId = glAccountId, Description = "utilities", Amount = amount }]
    };

    public static OutgoingPayment OutgoingPayment(string branchId, Guid supplierId, Guid bankAccountId, Guid apInvoiceId, string status = "DRAFT", decimal amount = 100) => new()
    {
        PaymentNo = $"OP-{Guid.NewGuid():N}",
        BranchId = branchId,
        SupplierId = supplierId,
        BankAccountId = bankAccountId,
        PaymentDate = DateTimeOffset.UtcNow,
        Status = status,
        Lines = [new OutgoingPaymentLine { ApInvoiceId = apInvoiceId, Amount = amount }]
    };
}
