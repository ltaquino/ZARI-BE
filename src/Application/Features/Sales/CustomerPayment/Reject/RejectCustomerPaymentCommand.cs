namespace ZARI.Application.Features.Sales.CustomerPayments.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.CustomerPayments.GetAll;
using ZARI.Domain.Common;

public sealed record RejectCustomerPaymentCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<CustomerPaymentResponse>>;
