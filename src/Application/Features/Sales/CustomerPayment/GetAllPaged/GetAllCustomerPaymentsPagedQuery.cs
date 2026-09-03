namespace ZARI.Application.Features.Sales.CustomerPayments.GetAllPaged;

using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllCustomerPaymentsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<CustomerPaymentResponse>>>;
