namespace ZARI.Application.Features.Sales.CustomerPayments.Approve;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Domain.Common;

public sealed record ApproveCustomerPaymentCommand(Guid Id, string ApproverUserId, string? Comments) : ICommand<Result<CustomerPaymentResponse>>;
