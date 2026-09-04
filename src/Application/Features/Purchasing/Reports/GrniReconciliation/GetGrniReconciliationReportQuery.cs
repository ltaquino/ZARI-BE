namespace ZARI.Application.Features.Purchasing.Reports.GrniReconciliation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Posted goods receipts (PO) that haven't been fully cleared yet — the "2100 Goods Received Not
/// Invoiced" holding liability still outstanding on the books, cross-checked against the account's
/// actual GL balance. ShowOnlyOutstanding narrows the returned Rows to lines not yet fully cleared;
/// the report's totals (TotalReceived/TotalCleared/TotalOutstanding/LiveGrniBalance/Variance) always
/// reflect every active GRPO regardless of that toggle.
/// </summary>
public sealed record GetGrniReconciliationReportQuery(string? BranchId, bool ShowOnlyOutstanding = false) : IQuery<Result<GrniReconciliationReportResponse>>;

public sealed record GrniGrpoRow(
    Guid GrpoId,
    string GrpoNo,
    string BranchId,
    string SupplierName,
    DateTimeOffset ReceiptDate,
    decimal Value,
    decimal ClearedValue,
    decimal Outstanding,
    List<string> ClearedByDocumentNos);

public sealed record GrniReconciliationReportResponse(
    List<GrniGrpoRow> Rows,
    decimal TotalReceived,
    decimal TotalCleared,
    decimal TotalOutstanding,
    decimal LiveGrniBalance,
    decimal Variance,
    bool IsReconciled);
