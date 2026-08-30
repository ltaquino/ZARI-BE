namespace ZARI.Application.Features.Sales.CustomerPayments.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteCustomerPaymentCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteCustomerPaymentCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteCustomerPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.CustomerPayments.FindAsync([command.Id], cancellationToken);
        if (payment is null)
            return Result.Failure(Error.NotFound("CustomerPayment.NotFound", $"Customer payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMER_PAYMENTS", FormAction.Delete, payment.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("CustomerPayment.Forbidden", "You do not have permission to delete customer payments for this branch."));

        if (payment.Status != "DRAFT")
            return Result.Failure(Error.Validation("CustomerPayment.NotDraft", "Only draft customer payments can be deleted — cancel it instead."));

        dbContext.CustomerPayments.Remove(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
