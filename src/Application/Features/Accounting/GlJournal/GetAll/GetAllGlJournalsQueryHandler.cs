namespace ZARI.Application.Features.Accounting.GlJournals.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.GlJournals.Shared;
using ZARI.Domain.Common;

public sealed class GetAllGlJournalsQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllGlJournalsQuery, Result<List<GlJournalResponse>>>
{
    public async Task<Result<List<GlJournalResponse>>> HandleAsync(GetAllGlJournalsQuery query, CancellationToken cancellationToken = default)
    {
        var journals = await dbContext.GlJournals
            .Include(j => j.Lines)
            .OrderByDescending(j => j.JournalDate)
            .ToListAsync(cancellationToken);

        return Result.Success(journals.Select(GlJournalMapper.ToResponse).ToList());
    }
}
