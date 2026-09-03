namespace ZARI.Application.Features.Accounting.GlJournals.GetAllPaged;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.GetAll;
using ZARI.Application.Features.Accounting.GlJournals.Shared;
using ZARI.Domain.Common;

public sealed class GetAllGlJournalsPagedQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllGlJournalsPagedQuery, Result<PagedResult<GlJournalResponse>>>
{
    public async Task<Result<PagedResult<GlJournalResponse>>> HandleAsync(GetAllGlJournalsPagedQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GL_JOURNALS", FormAction.View, cancellationToken))
            return Result.Failure<PagedResult<GlJournalResponse>>(Error.Forbidden("GlJournal.Forbidden", "You do not have permission to view gl journals."));

        var baseQuery = dbContext.GlJournals.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
            baseQuery = baseQuery.Where(x => x.JournalNo.Contains(query.Search));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var journals = await baseQuery
            .OrderByDescending(j => j.JournalDate)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Include(j => j.Lines)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<GlJournalResponse>(journals.Select(GlJournalMapper.ToResponse).ToList(), totalCount, query.Page, query.PageSize));
    }
}
