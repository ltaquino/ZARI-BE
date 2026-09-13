namespace ZARI.Application.Features.Customers.CreditRecords.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.CreditRecords.Get;
using ZARI.Domain.Common;

public sealed record GetAllCustomerCreditRecordsQuery(Guid? CustomerId) : IQuery<Result<List<CustomerCreditRecordResponse>>>;
