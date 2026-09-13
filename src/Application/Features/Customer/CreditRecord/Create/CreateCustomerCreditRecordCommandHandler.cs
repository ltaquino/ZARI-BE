namespace ZARI.Application.Features.Customers.CreditRecords.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.CreditRecords.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateCustomerCreditRecordCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateCustomerCreditRecordCommand, Result<CustomerCreditRecordResponse>>
{
    public async Task<Result<CustomerCreditRecordResponse>> HandleAsync(CreateCustomerCreditRecordCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(c => c.Id == command.CustomerId, cancellationToken);
        if (customer is null)
            return Result.Failure<CustomerCreditRecordResponse>(Error.NotFound("Customer.NotFound", $"Customer with ID '{command.CustomerId}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMER_CREDIT_RECORDS", FormAction.Create, customer.BranchId, cancellationToken))
            return Result.Failure<CustomerCreditRecordResponse>(Error.Forbidden("CustomerCreditRecord.Forbidden", "You do not have permission to add credit records for this member's branch."));

        var record = new CustomerCreditRecord
        {
            CustomerId = command.CustomerId,
            RecordType = command.RecordType,
            Description = command.Description,
            RecordDate = command.RecordDate,
            Amount = command.Amount,
            Remarks = command.Remarks,
            CreatedBy = command.CreatedBy
        };

        dbContext.CustomerCreditRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new CustomerCreditRecordResponse(
            record.Id, record.CustomerId, customer.Name, record.RecordType, record.Description,
            record.RecordDate, record.Amount, record.Remarks, record.CreatedAt, record.CreatedBy));
    }
}
