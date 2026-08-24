namespace ZARI.Application.Features.Customers.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.Get;
using ZARI.Domain.Common;

public sealed record CreateCustomerCommand(
    string Name,
    string Type,
    string Email,
    string Phone,
    string BranchId,
    string Status,
    string Owner,
    string Address,
    string? Notes) : ICommand<Result<CustomerResponse>>;
