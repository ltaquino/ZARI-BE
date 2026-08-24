namespace ZARI.Application.Features.Inventory.AdjustmentReasons.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.AdjustmentReasons.Get;
using ZARI.Domain.Common;

public sealed record GetAllAdjustmentReasonsQuery : IQuery<Result<List<AdjustmentReasonResponse>>>;
