namespace ZARI.Application.Features.Loan.Reports.CicCsdf;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetCicCsdfExportQuery(string? BranchId, Guid? CustomerId, DateTimeOffset? AsOfDate) : IQuery<Result<CicCsdfExport>>;

/// <summary>
/// The raw material for a CIC CSDF v1.4 submission file (ZARI-FE/frs/loan-cic/LoanCicContext.md).
/// Each entry in <see cref="IdRecords"/>/<see cref="CiRecords"/> is one data record's fields, in
/// CIC's own 1-based field order, already sized to that record type's real field count (123 for
/// `ID`, 143 for `CI`) — a null/empty slot is an intentionally blank optional field, not a bug. The
/// Api-layer writer's only job is to join each array with "|" and wrap the whole thing in an
/// `HD`/`FT` header/trailer — no field-order knowledge belongs there.
/// </summary>
public sealed record CicCsdfExport(
    string ProviderCode,
    DateTimeOffset FileReferenceDate,
    List<string?[]> IdRecords,
    List<string?[]> CiRecords);
