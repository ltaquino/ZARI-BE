namespace ZARI.Application.Features.Uoms.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Uoms.Get;
using ZARI.Domain.Common;

public sealed record GetAllUomsQuery : IQuery<Result<List<UomResponse>>>;
