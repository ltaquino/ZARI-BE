namespace ZARI.Application.Features.Purchasing.GoodsReturns.Reject;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.GoodsReturns.GetAll;
using ZARI.Domain.Common;

public sealed record RejectGoodsReturnCommand(Guid Id, string ApproverUserId, string Comments) : ICommand<Result<GoodsReturnResponse>>;
