namespace ZARI.Application.Features.Customers.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.Get;
using ZARI.Domain.Common;

public sealed record GetAllCustomersQuery : IQuery<Result<List<CustomerResponse>>>;
