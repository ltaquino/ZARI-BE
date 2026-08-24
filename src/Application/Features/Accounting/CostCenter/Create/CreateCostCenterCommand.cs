namespace ZARI.Application.Features.Accounting.CostCenters.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.CostCenters.Get;
using ZARI.Domain.Common;

public sealed record CreateCostCenterCommand(string? BranchId, string Code, string Name, string Status) : ICommand<Result<CostCenterResponse>>;
