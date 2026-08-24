namespace ZARI.Application.Features.Accounting.CostCenters.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.CostCenters.Get;
using ZARI.Domain.Common;

public sealed record GetAllCostCentersQuery : IQuery<Result<List<CostCenterResponse>>>;
