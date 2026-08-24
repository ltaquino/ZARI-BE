namespace ZARI.Application.Features.Accounting.CostCenters.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteCostCenterCommand(Guid Id) : ICommand;
