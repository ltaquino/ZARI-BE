namespace ZARI.Application.Features.Purchasing.Reports.ApAging;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <summary>
/// Posted, unpaid AP invoices grouped by supplier and how overdue they are against their due date.
/// AsOfDate defaults to today (server clock) when omitted.
/// </summary>
public sealed record GetApAgingReportQuery(string? BranchId, Guid? SupplierId, DateTimeOffset? AsOfDate) : IQuery<Result<ApAgingReportResponse>>;

public sealed record ApAgingInvoiceRow(
    Guid InvoiceId,
    string InvoiceNo,
    string SupplierInvoiceNo,
    string BranchId,
    DateTimeOffset DueDate,
    int DaysOverdue,
    string Bucket,
    decimal Outstanding);

public sealed record ApAgingSupplierGroup(
    Guid SupplierId,
    string SupplierCode,
    string SupplierName,
    decimal GroupTotal,
    List<ApAgingInvoiceRow> Invoices);

public sealed record ApAgingReportResponse(
    List<ApAgingSupplierGroup> Groups,
    decimal TotalOutstanding,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days90Plus);
