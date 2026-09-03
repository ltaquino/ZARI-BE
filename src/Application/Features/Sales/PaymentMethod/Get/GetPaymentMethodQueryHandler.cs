namespace ZARI.Application.Features.Sales.PaymentMethods.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetPaymentMethodQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetPaymentMethodQuery, Result<PaymentMethodResponse>>
{
    public async Task<Result<PaymentMethodResponse>> HandleAsync(GetPaymentMethodQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PAYMENT_METHODS", FormAction.View, cancellationToken))
            return Result.Failure<PaymentMethodResponse>(Error.Forbidden("PaymentMethod.Forbidden", "You do not have permission to view payment methods."));

        var method = await dbContext.PaymentMethods
            .Include(m => m.GlAccount)
            .Where(m => m.Id == query.Id)
            .Select(m => new PaymentMethodResponse(m.Id, m.Code, m.Name, m.GlAccountId, m.GlAccount.Code, m.GlAccount.Name, m.RequiresReferenceNo, m.ReferenceNoLabel, m.RequiresBankOrPartnerName, m.DisplayOrder, m.Status, m.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (method is null)
            return Result.Failure<PaymentMethodResponse>(Error.NotFound("PaymentMethod.NotFound", $"Payment method with ID '{query.Id}' was not found."));

        return Result.Success(method);
    }
}
