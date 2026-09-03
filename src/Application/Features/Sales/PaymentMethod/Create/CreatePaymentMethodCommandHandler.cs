namespace ZARI.Application.Features.Sales.PaymentMethods.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PaymentMethods.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreatePaymentMethodCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreatePaymentMethodCommand, Result<PaymentMethodResponse>>
{
    public async Task<Result<PaymentMethodResponse>> HandleAsync(CreatePaymentMethodCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PAYMENT_METHODS", FormAction.Create, cancellationToken))
            return Result.Failure<PaymentMethodResponse>(Error.Forbidden("PaymentMethod.Forbidden", "You do not have permission to create payment methods."));

        var codeExists = await dbContext.PaymentMethods.AnyAsync(m => m.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<PaymentMethodResponse>(Error.Conflict("PaymentMethod.DuplicateCode", $"A payment method with code '{command.Code}' already exists."));

        var glAccount = await dbContext.GlAccounts.FirstOrDefaultAsync(a => a.Id == command.GlAccountId, cancellationToken);
        if (glAccount is null)
            return Result.Failure<PaymentMethodResponse>(Error.NotFound("PaymentMethod.GlAccountNotFound", "The selected GL account was not found."));

        var method = new PaymentMethod
        {
            Code = command.Code,
            Name = command.Name,
            GlAccountId = command.GlAccountId,
            RequiresReferenceNo = command.RequiresReferenceNo,
            ReferenceNoLabel = command.RequiresReferenceNo ? command.ReferenceNoLabel : null,
            RequiresBankOrPartnerName = command.RequiresBankOrPartnerName,
            DisplayOrder = command.DisplayOrder,
            Status = command.Status
        };

        dbContext.PaymentMethods.Add(method);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new PaymentMethodResponse(method.Id, method.Code, method.Name, method.GlAccountId, glAccount.Code, glAccount.Name, method.RequiresReferenceNo, method.ReferenceNoLabel, method.RequiresBankOrPartnerName, method.DisplayOrder, method.Status, method.CreatedAt);
        return Result.Success(response);
    }
}
