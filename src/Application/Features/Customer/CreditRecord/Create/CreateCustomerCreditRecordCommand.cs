namespace ZARI.Application.Features.Customers.CreditRecords.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.CreditRecords.Get;
using ZARI.Domain.Common;

public sealed record CreateCustomerCreditRecordCommand(
    Guid CustomerId,
    string RecordType,
    string Description,
    DateTimeOffset RecordDate,
    decimal? Amount,
    string? Remarks,
    string? CreatedBy) : ICommand<Result<CustomerCreditRecordResponse>>;
