namespace ZARI.Application.Features.Purchasing.GoodsReturns.RequestCancellation;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Domain.Common;

public sealed record RequestGoodsReturnCancellationCommand(Guid Id, string RequestedBy, string Reason) : ICommand<Result<GoodsReturnResponse>>;
