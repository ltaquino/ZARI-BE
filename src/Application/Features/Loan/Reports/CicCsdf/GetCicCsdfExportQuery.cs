namespace ZARI.Application.Features.Loan.Reports.CicCsdf;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

/// <param name="ProviderCode">
/// CIC's assigned 8-alphanumeric code for the cooperative (LoanCicContext.md §6 #1 — not yet known
/// to this repo). Threaded in from the Api layer's configuration rather than read here directly, so
/// the Application layer stays config-agnostic; defaults to a clearly-fake placeholder when neither
/// is set, so an unconfigured environment produces an obviously-wrong file rather than a silently
/// wrong one.
/// </param>
public sealed record GetCicCsdfExportQuery(string? BranchId, Guid? CustomerId, DateTimeOffset? AsOfDate, string? ProviderCode = null) : IQuery<Result<CicCsdfExport>>;

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
