namespace ZARI.Application.Features.Customers.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string Type,
    string Email,
    string Phone,
    string BranchId,
    string Status,
    string Owner,
    string Address,
    string? Notes) : ICommand;
