namespace ZARI.Application.Features.Sales.StatutoryDiscountTypes.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateStatutoryDiscountTypeCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateStatutoryDiscountTypeCommand, Result<StatutoryDiscountTypeResponse>>
{
    public async Task<Result<StatutoryDiscountTypeResponse>> HandleAsync(CreateStatutoryDiscountTypeCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("STATUTORY_DISCOUNT_TYPES", FormAction.Create, cancellationToken))
            return Result.Failure<StatutoryDiscountTypeResponse>(Error.Forbidden("StatutoryDiscountType.Forbidden", "You do not have permission to create statutory discount types."));

        var codeExists = await dbContext.StatutoryDiscountTypes.AnyAsync(t => t.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<StatutoryDiscountTypeResponse>(Error.Conflict("StatutoryDiscountType.DuplicateCode", $"A statutory discount type with code '{command.Code}' already exists."));

        var type = new StatutoryDiscountType
        {
            Code = command.Code,
            Name = command.Name,
            DiscountPct = command.DiscountPct,
            IsVatExempt = command.IsVatExempt,
            RequiredIdLabel = command.RequiredIdLabel,
            Status = command.Status
        };

        dbContext.StatutoryDiscountTypes.Add(type);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new StatutoryDiscountTypeResponse(type.Id, type.Code, type.Name, type.DiscountPct, type.IsVatExempt, type.RequiredIdLabel, type.Status, type.CreatedAt);
        return Result.Success(response);
    }
}
