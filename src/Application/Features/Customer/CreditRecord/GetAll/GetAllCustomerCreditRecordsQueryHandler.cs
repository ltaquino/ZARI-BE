namespace ZARI.Application.Features.Customers.CreditRecords.GetAll;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.CreditRecords.Get;
using ZARI.Domain.Common;

public sealed class GetAllCustomerCreditRecordsQueryHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : IQueryHandler<GetAllCustomerCreditRecordsQuery, Result<List<CustomerCreditRecordResponse>>>
{
    public async Task<Result<List<CustomerCreditRecordResponse>>> HandleAsync(GetAllCustomerCreditRecordsQuery query, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("CUSTOMER_CREDIT_RECORDS", FormAction.View, cancellationToken))
            return Result.Failure<List<CustomerCreditRecordResponse>>(Error.Forbidden("CustomerCreditRecord.Forbidden", "You do not have permission to view member credit records."));

        var recordsQuery = dbContext.CustomerCreditRecords.AsNoTracking().Include(r => r.Customer).AsQueryable();
        if (query.CustomerId is { } customerId) recordsQuery = recordsQuery.Where(r => r.CustomerId == customerId);

        var records = await recordsQuery
            .OrderByDescending(r => r.RecordDate)
            .Select(r => new CustomerCreditRecordResponse(
                r.Id, r.CustomerId, r.Customer.Name, r.RecordType, r.Description, r.RecordDate, r.Amount, r.Remarks, r.CreatedAt, r.CreatedBy))
            .ToListAsync(cancellationToken);

        return Result.Success(records);
    }
}
