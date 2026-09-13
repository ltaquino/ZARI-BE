namespace ZARI.Application.Features.Customers.CreditRecords.Get;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class GetCustomerCreditRecordQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetCustomerCreditRecordQuery, Result<CustomerCreditRecordResponse>>
{
    public async Task<Result<CustomerCreditRecordResponse>> HandleAsync(GetCustomerCreditRecordQuery query, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.CustomerCreditRecords
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == query.Id, cancellationToken);

        if (record is null)
            return Result.Failure<CustomerCreditRecordResponse>(Error.NotFound("CustomerCreditRecord.NotFound", $"Credit record with ID '{query.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMER_CREDIT_RECORDS", FormAction.View, record.Customer.BranchId, cancellationToken))
            return Result.Failure<CustomerCreditRecordResponse>(Error.Forbidden("CustomerCreditRecord.Forbidden", "You do not have permission to view credit records for this member's branch."));

        return Result.Success(new CustomerCreditRecordResponse(
            record.Id, record.CustomerId, record.Customer.Name, record.RecordType, record.Description,
            record.RecordDate, record.Amount, record.Remarks, record.CreatedAt, record.CreatedBy));
    }
}
