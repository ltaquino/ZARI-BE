namespace ZARI.Application.Features.Customers.CreditRecords.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteCustomerCreditRecordCommand(Guid Id) : ICommand;
