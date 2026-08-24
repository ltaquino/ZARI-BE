namespace ZARI.Application.Features.Uoms.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Uoms.Get;
using ZARI.Domain.Common;

public sealed class GetAllUomsQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllUomsQuery, Result<List<UomResponse>>>
{
    public async Task<Result<List<UomResponse>>> HandleAsync(GetAllUomsQuery query, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.Uoms
            .OrderBy(u => u.Code)
            .Select(u => new UomResponse(u.Id, u.Code, u.Name, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
