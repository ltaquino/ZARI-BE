namespace ZARI.Application.Features.Accounting.Reports.GeneralJournal;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetGeneralJournalReportQuery(string? BranchId, DateTimeOffset? FromDate, DateTimeOffset? ToDate)
    : IQuery<Result<GeneralJournalReportResponse>>;

public sealed record GeneralJournalLine(Guid AccountId, string AccountName, string? Memo, decimal Debit, decimal Credit);

public sealed record GeneralJournalEntry(Guid Id, string JournalNo, DateTimeOffset JournalDate, string? Description, string BranchId, string Status, List<GeneralJournalLine> Lines);

public sealed record GeneralJournalReportResponse(List<GeneralJournalEntry> Journals, decimal TotalDebit, decimal TotalCredit);
