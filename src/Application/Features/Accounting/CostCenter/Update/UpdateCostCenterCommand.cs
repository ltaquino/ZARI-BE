namespace ZARI.Application.Features.Accounting.CostCenters.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateCostCenterCommand(Guid Id, string? BranchId, string Code, string Name, string Status) : ICommand;
