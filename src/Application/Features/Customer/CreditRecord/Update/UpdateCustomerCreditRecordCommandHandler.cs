namespace ZARI.Application.Features.Customers.CreditRecords.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateCustomerCreditRecordCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateCustomerCreditRecordCommand>
{
    public async Task<Result> HandleAsync(UpdateCustomerCreditRecordCommand command, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.CustomerCreditRecords.Include(r => r.Customer).FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken);
        if (record is null)
            return Result.Failure(Error.NotFound("CustomerCreditRecord.NotFound", $"Credit record with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMER_CREDIT_RECORDS", FormAction.Edit, record.Customer.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("CustomerCreditRecord.Forbidden", "You do not have permission to edit credit records for this member's branch."));

        record.RecordType = command.RecordType;
        record.Description = command.Description;
        record.RecordDate = command.RecordDate;
        record.Amount = command.Amount;
        record.Remarks = command.Remarks;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
