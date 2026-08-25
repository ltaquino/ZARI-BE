namespace ZARI.Application.Features.Inventory.Uoms.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Uoms.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateUomCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateUomCommand, Result<UomResponse>>
{
    public async Task<Result<UomResponse>> HandleAsync(CreateUomCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("UOMS", FormAction.Create, cancellationToken))
            return Result.Failure<UomResponse>(Error.Forbidden("Uom.Forbidden", "You do not have permission to create UOMs."));

        var codeExists = await dbContext.Uoms
            .AnyAsync(u => u.Code == command.Code, cancellationToken);

        if (codeExists)
            return Result.Failure<UomResponse>(Error.Conflict("Uom.DuplicateCode", $"A UOM with code '{command.Code}' already exists."));

        var uom = new Uom
        {
            Code = command.Code,
            Name = command.Name
        };

        dbContext.Uoms.Add(uom);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new UomResponse(uom.Id, uom.Code, uom.Name, uom.CreatedAt);
        return Result.Success(response);
    }
}
