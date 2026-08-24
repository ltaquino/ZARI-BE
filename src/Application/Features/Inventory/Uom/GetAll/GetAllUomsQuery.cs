namespace ZARI.Application.Features.Inventory.Uoms.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.Uoms.Get;
using ZARI.Domain.Common;

public sealed record GetAllUomsQuery : IQuery<Result<List<UomResponse>>>;
