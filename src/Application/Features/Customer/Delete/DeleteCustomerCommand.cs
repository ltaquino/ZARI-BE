namespace ZARI.Application.Features.Customers.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteCustomerCommand(Guid Id) : ICommand;
