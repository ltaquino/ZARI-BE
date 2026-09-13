namespace ZARI.Application.Features.Customers.CreditRecords.Delete;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteCustomerCreditRecordCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteCustomerCreditRecordCommand>
{
    public async Task<Result> HandleAsync(DeleteCustomerCreditRecordCommand command, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.CustomerCreditRecords.Include(r => r.Customer).FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (record is null)
            return Result.Failure(Error.NotFound("CustomerCreditRecord.NotFound", $"Credit record with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMER_CREDIT_RECORDS", FormAction.Delete, record.Customer.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("CustomerCreditRecord.Forbidden", "You do not have permission to delete credit records for this member's branch."));

        dbContext.CustomerCreditRecords.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
