namespace ZARI.Application.Features.Accounting.GlJournals.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.Shared;
using ZARI.Domain.Common;

public sealed class GetAllGlJournalsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllGlJournalsQuery, Result<List<GlJournalResponse>>>
{
    public async Task<Result<List<GlJournalResponse>>> HandleAsync(GetAllGlJournalsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("GL_JOURNALS", FormAction.View, cancellationToken))
            return Result.Failure<List<GlJournalResponse>>(Error.Forbidden("GlJournal.Forbidden", "You do not have permission to view GL journals."));

        var journals = await dbContext.GlJournals.AsNoTracking()
            .Include(j => j.Lines)
            .OrderByDescending(j => j.JournalDate)
            .ToListAsync(cancellationToken);

        return Result.Success(journals.Select(GlJournalMapper.ToResponse).ToList());
    }
}
