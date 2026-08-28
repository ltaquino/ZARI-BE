namespace ZARI.Application.Features.Purchasing.OutgoingPayments.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteOutgoingPaymentCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteOutgoingPaymentCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteOutgoingPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var payment = await dbContext.OutgoingPayments.FindAsync([command.Id], cancellationToken);
        if (payment is null)
            return Result.Failure(Error.NotFound("OutgoingPayment.NotFound", $"Outgoing payment with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("OUTGOING_PAYMENTS", FormAction.Delete, payment.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("OutgoingPayment.Forbidden", "You do not have permission to delete outgoing payments for this branch."));

        if (payment.Status != "DRAFT")
            return Result.Failure(Error.Validation("OutgoingPayment.NotDraft", "Only draft outgoing payments can be deleted — cancel it instead."));

        dbContext.OutgoingPayments.Remove(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
