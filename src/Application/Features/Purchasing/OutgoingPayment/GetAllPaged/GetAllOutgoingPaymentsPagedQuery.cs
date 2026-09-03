namespace ZARI.Application.Features.Purchasing.OutgoingPayments.GetAllPaged;

using ZARI.Application.Features.Purchasing.OutgoingPayments.GetAll;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetAllOutgoingPaymentsPagedQuery(int Page = 1, int PageSize = 20, string? Search = null) : IQuery<Result<PagedResult<OutgoingPaymentResponse>>>;
