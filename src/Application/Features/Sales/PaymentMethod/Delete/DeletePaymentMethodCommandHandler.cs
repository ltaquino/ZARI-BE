namespace ZARI.Application.Features.Sales.PaymentMethods.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeletePaymentMethodCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeletePaymentMethodCommand>
{
    public async Task<Result> HandleAsync(DeletePaymentMethodCommand command, CancellationToken cancellationToken = default)
    {
        var method = await dbContext.PaymentMethods.FindAsync([command.Id], cancellationToken);
        if (method is null)
            return Result.Failure(Error.NotFound("PaymentMethod.NotFound", $"Payment method with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("PAYMENT_METHODS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("PaymentMethod.Forbidden", "You do not have permission to delete payment methods."));

        var inUse = await dbContext.CustomerPaymentTenders.AnyAsync(t => t.PaymentMethodId == command.Id, cancellationToken);
        if (inUse)
            return Result.Failure(Error.Conflict("PaymentMethod.InUse", "This payment method has been used on at least one payment and cannot be deleted — set it to inactive instead."));

        dbContext.PaymentMethods.Remove(method);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
