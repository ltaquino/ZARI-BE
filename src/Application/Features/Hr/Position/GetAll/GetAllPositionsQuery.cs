namespace ZARI.Application.Features.Hr.Positions.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.Positions.Get;
using ZARI.Domain.Common;

public sealed record GetAllPositionsQuery : IQuery<Result<List<PositionResponse>>>;
