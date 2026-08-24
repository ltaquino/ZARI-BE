namespace ZARI.Application.Features.Inventory.SerialNumbers.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetAllSerialNumbersQueryHandler(IAppDbContext dbContext) : IQueryHandler<GetAllSerialNumbersQuery, Result<List<SerialNumberResponse>>>
{
    public async Task<Result<List<SerialNumberResponse>>> HandleAsync(GetAllSerialNumbersQuery query, CancellationToken cancellationToken = default)
    {
        var items = await dbContext.SerialNumbers
            .OrderBy(s => s.SerialNo)
            .Select(s => new SerialNumberResponse(s.Id, s.ItemId, s.SerialNo, s.WarehouseId, s.Status, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
