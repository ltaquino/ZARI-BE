namespace ZARI.Application.Features.Purchasing.GoodsReturns.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteGoodsReturnCommand(Guid Id) : ICommand<Result>;
