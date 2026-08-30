namespace ZARI.Application.Features.Sales.SalesReturns.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteSalesReturnCommand(Guid Id) : ICommand<Result>;
