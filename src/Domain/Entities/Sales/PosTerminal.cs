namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A physical cash register/till at a branch — purely operational identity for POS sessions
/// (which register a sale was rung up on) and traceability on SalesInvoice.PosTerminalId. Does NOT
/// carry its own BIR-OR numbering series or Z-Counter: all terminals at one branch deliberately
/// share that branch's existing "BIR-OR" DocumentSequence and Branch.ZCounter (Wave 4/Phase 22) —
/// one continuous series per branch stays trivially sequential to an examiner no matter how many
/// terminals feed it, whereas independent per-terminal series would need careful interleaving.
/// </summary>
public sealed class PosTerminal : AuditableEntity
{
    public string BranchId { get; set; } = default!;
    public Branch Branch { get; set; } = default!;

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    // Optional per-machine BIR accreditation details, mirrored from Branch's own (Phase 20) fields
    // in case this specific till is separately accredited — purely informational today, not used
    // to scope any numbering series.
    public string? MachineIdentificationNumber { get; set; }
    public string? MachineSerialNumber { get; set; }
    public string? PosPermitNumber { get; set; }
    public DateTime? PosPermitDateIssued { get; set; }

    public string Status { get; set; } = default!;
}
