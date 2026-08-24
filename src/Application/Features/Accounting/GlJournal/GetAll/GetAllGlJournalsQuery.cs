namespace ZARI.Application.Features.Accounting.GlJournals.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllGlJournalsQuery : IQuery<Result<List<GlJournalResponse>>>;

public sealed record GlJournalLineResponse(Guid Id, Guid AccountId, Guid? CostCenterId, decimal DebitAmount, decimal CreditAmount, string? Memo);

public sealed record GlJournalResponse(
    Guid Id,
    string JournalNo,
    string BranchId,
    DateTimeOffset JournalDate,
    string SourceModule,
    string SourceReferenceTable,
    string SourceReferenceId,
    string? Description,
    string Status,
    Guid? ReversalOfJournalId,
    List<GlJournalLineResponse> Lines,
    DateTimeOffset CreatedAt);
