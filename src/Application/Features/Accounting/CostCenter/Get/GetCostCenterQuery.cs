namespace ZARI.Application.Features.Accounting.CostCenters.Get;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record GetCostCenterQuery(Guid Id) : IQuery<Result<CostCenterResponse>>;

public sealed record CostCenterResponse(Guid Id, string? BranchId, string Code, string Name, string Status, DateTimeOffset CreatedAt);
