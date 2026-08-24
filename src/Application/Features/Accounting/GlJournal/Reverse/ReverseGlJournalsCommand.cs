namespace ZARI.Application.Features.Accounting.GlJournals.Reverse;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Domain.Common;

public sealed record ReverseGlJournalsCommand(
    string SourceReferenceTable,
    string SourceReferenceId,
    DateTimeOffset JournalDate,
    string? Description) : ICommand<Result<List<GlJournalResponse>>>;
