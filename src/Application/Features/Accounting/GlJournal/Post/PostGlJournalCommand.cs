namespace ZARI.Application.Features.Accounting.GlJournals.Post;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Domain.Common;

public sealed record PostGlJournalLineInput(Guid AccountId, Guid? CostCenterId, decimal DebitAmount, decimal CreditAmount, string? Memo);

public sealed record PostGlJournalCommand(
    string BranchId,
    DateTimeOffset JournalDate,
    string SourceModule,
    string SourceReferenceTable,
    string SourceReferenceId,
    string? Description,
    List<PostGlJournalLineInput> Lines) : ICommand<Result<GlJournalResponse>>;
