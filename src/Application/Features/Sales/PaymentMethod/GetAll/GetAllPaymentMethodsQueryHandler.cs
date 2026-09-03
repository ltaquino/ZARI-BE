namespace ZARI.Application.Features.Sales.PaymentMethods.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PaymentMethods.Get;
using ZARI.Domain.Common;

public sealed class GetAllPaymentMethodsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService) : IQueryHandler<GetAllPaymentMethodsQuery, Result<List<PaymentMethodResponse>>>
{
    public async Task<Result<List<PaymentMethodResponse>>> HandleAsync(GetAllPaymentMethodsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PAYMENT_METHODS", FormAction.View, cancellationToken))
            return Result.Failure<List<PaymentMethodResponse>>(Error.Forbidden("PaymentMethod.Forbidden", "You do not have permission to view payment methods."));

        var items = await dbContext.PaymentMethods
            .Include(m => m.GlAccount)
            .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Code)
            .Select(m => new PaymentMethodResponse(m.Id, m.Code, m.Name, m.GlAccountId, m.GlAccount.Code, m.GlAccount.Name, m.RequiresReferenceNo, m.ReferenceNoLabel, m.RequiresBankOrPartnerName, m.DisplayOrder, m.Status, m.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
