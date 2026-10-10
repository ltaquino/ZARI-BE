namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A physical cash register/till at a branch — operational identity for POS sessions and
/// traceability on SalesInvoice.PosTerminalId.
///
/// By default (HasOwnSeries = false) a terminal does NOT carry its own BIR-OR series or Z-Counter:
/// store tills deliberately share the branch's "BIR-OR" DocumentSequence and Branch.ZCounter (Wave
/// 4/Phase 22) — one continuous series per branch stays trivially sequential to an examiner.
///
/// Route Sales trucks are the exception (RouteSalesContext.md §3): each truck handheld is its own
/// BIR-registered machine that must issue numbered invoices offline, so it gets HasOwnSeries = true
/// and its own series (OrPrefix + OrNextNumber, zero-padded to OrPaddingLength) and its own
/// ZCounter. Invoices from such a terminal never consume the branch series, and the branch
/// X/Z-Reading excludes them (PosClosingAggregator) — the terminal is closed on its own.
/// </summary>
public sealed class PosTerminal : AuditableEntity
{
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    // Per-machine BIR accreditation details, mirrored from Branch's own (Phase 20) fields — printed
    // on invoices from an own-series terminal; informational for shared-series store tills.
    public string? MachineIdentificationNumber { get; set; }
    public string? MachineSerialNumber { get; set; }
    public string? PosPermitNumber { get; set; }
    public DateTime? PosPermitDateIssued { get; set; }

    public string Status { get; set; } = default!;

    public bool HasOwnSeries { get; set; }
    public string? OrPrefix { get; set; }
    public int OrNextNumber { get; set; } = 1;
    public int OrPaddingLength { get; set; } = 6;
    public int ZCounter { get; set; }
}
