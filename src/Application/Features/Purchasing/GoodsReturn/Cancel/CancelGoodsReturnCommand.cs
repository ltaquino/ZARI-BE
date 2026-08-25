namespace ZARI.Application.Features.Purchasing.GoodsReturns.Cancel;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Domain.Common;

public sealed record CancelGoodsReturnCommand(Guid Id, string CancelledBy, string Reason) : ICommand<Result<GoodsReturnResponse>>;
