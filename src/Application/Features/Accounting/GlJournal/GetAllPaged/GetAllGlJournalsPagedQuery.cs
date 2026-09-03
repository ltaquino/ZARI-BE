namespace ZARI.Application.Features.Accounting.GlJournals.GetAllPaged;

using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllGlJournalsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<GlJournalResponse>>>;
