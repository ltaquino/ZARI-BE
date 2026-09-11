namespace ZARI.Application.Features.Customers.CreditRecords.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetCustomerCreditRecordQuery(Guid Id) : IQuery<Result<CustomerCreditRecordResponse>>;

public sealed record CustomerCreditRecordResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string RecordType,
    string Description,
    DateTimeOffset RecordDate,
    decimal? Amount,
    string? Remarks,
    DateTimeOffset CreatedAt,
    string? CreatedBy);
