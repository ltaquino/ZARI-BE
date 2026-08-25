namespace ZARI.Application.Features.Purchasing.GoodsReturns.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitGoodsReturnCommand(Guid Id, string RequestedBy) : ICommand<Result<GoodsReturnResponse>>;
