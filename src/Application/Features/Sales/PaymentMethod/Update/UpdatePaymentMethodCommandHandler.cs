namespace ZARI.Application.Features.Sales.PaymentMethods.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdatePaymentMethodCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdatePaymentMethodCommand>
{
    public async Task<Result> HandleAsync(UpdatePaymentMethodCommand command, CancellationToken cancellationToken = default)
    {
        var method = await dbContext.PaymentMethods.FindAsync([command.Id], cancellationToken);
        if (method is null)
            return Result.Failure(Error.NotFound("PaymentMethod.NotFound", $"Payment method with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("PAYMENT_METHODS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("PaymentMethod.Forbidden", "You do not have permission to update payment methods."));

        var duplicateCode = await dbContext.PaymentMethods.AnyAsync(m => m.Id != command.Id && m.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("PaymentMethod.DuplicateCode", $"A payment method with code '{command.Code}' already exists."));

        var glAccountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == command.GlAccountId, cancellationToken);
        if (!glAccountExists)
            return Result.Failure(Error.NotFound("PaymentMethod.GlAccountNotFound", "The selected GL account was not found."));

        method.Code = command.Code;
        method.Name = command.Name;
        method.GlAccountId = command.GlAccountId;
        method.RequiresReferenceNo = command.RequiresReferenceNo;
        method.ReferenceNoLabel = command.RequiresReferenceNo ? command.ReferenceNoLabel : null;
        method.RequiresBankOrPartnerName = command.RequiresBankOrPartnerName;
        method.DisplayOrder = command.DisplayOrder;
        method.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
