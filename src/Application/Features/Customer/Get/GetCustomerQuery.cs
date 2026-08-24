namespace ZARI.Application.Features.Customers.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetCustomerQuery(Guid Id) : IQuery<Result<CustomerResponse>>;

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Type,
    string Email,
    string Phone,
    string BranchId,
    string Status,
    string Owner,
    string Address,
    string? Notes,
    DateTimeOffset CreatedAt);
