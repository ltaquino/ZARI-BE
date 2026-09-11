namespace ZARI.Application.Features.Customers.CreditRecords.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateCustomerCreditRecordCommand(
    Guid Id,
    string RecordType,
    string Description,
    DateTimeOffset RecordDate,
    decimal? Amount,
    string? Remarks) : ICommand;
