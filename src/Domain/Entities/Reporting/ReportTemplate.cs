namespace ZARI.Domain.Entities;

using ZARI.Domain.Common;

/// <summary>
/// A user-designed, saved report definition: which dataset it reads from, which fields (as
/// ordered columns), which filters (each either a fixed value set at design time, or left blank
/// to be prompted for every time the report is run), paper size/orientation, and header/footer
/// text. The actual report data is never stored here — RunReportTemplateQueryHandler re-executes
/// the template's matching IReportDataset against live data every time it's run, so "viewing the
/// result" always reflects current data, not a snapshot.
/// </summary>
public sealed class ReportTemplate : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    /// <summary>Key of the IReportDataset this template reads from (e.g. "SALES_INVOICES").</summary>
    public string DatasetKey { get; set; } = default!;

    public string PaperSize { get; set; } = "A4"; // A4 | Letter | Legal
    public string Orientation { get; set; } = "Portrait"; // Portrait | Landscape
    public string? HeaderText { get; set; }
    public string? FooterText { get; set; }
    public bool ShowColumnTotals { get; set; }

    /// <summary>JSON-serialized List&lt;ReportTemplateColumn&gt; — the chosen fields, in order.</summary>
    public string ColumnsJson { get; set; } = "[]";
    /// <summary>JSON-serialized List&lt;ReportTemplateFilter&gt;.</summary>
    public string FiltersJson { get; set; } = "[]";
    /// <summary>JSON-serialized ReportTemplateSort, or null for no explicit sort.</summary>
    public string? SortJson { get; set; }

    /// <summary>JSON-serialized List&lt;string&gt; of FieldKeys to group by. Empty ("[]", the
    /// default) means plain detail-mode (one row per record) — RunReportTemplateQueryHandler only
    /// aggregates when this is non-empty, so every existing template keeps behaving exactly as
    /// before this field was added.</summary>
    public string GroupByJson { get; set; } = "[]";

    /// <summary>If true, any user with REPORT_DESIGNER view access can browse/run this template
    /// (not just its owner). Only the owner or an Admin can edit/delete it either way.</summary>
    public bool IsShared { get; set; }
    public string OwnerUserId { get; set; } = default!;

    public string Status { get; set; } = "Active";
}
