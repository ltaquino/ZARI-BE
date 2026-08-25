namespace ZARI.Application.Features.SystemModule.Forms.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetAllFormsQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllFormsQuery, Result<List<FormResponse>>>
{
    public async Task<Result<List<FormResponse>>> HandleAsync(GetAllFormsQuery query, CancellationToken cancellationToken = default)
    {
        var forms = await dbContext.Forms
            .OrderBy(f => f.Module).ThenBy(f => f.Name)
            .Select(f => new FormResponse(f.Code, f.Name, f.Module))
            .ToListAsync(cancellationToken);

        return Result.Success(forms);
    }
}
