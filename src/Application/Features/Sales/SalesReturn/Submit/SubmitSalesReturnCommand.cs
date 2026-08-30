namespace ZARI.Application.Features.Sales.SalesReturns.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.SalesReturns.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitSalesReturnCommand(Guid Id, string RequestedBy) : ICommand<Result<SalesReturnResponse>>;
