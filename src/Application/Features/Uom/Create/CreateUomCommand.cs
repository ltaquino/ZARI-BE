namespace ZARI.Application.Features.Uoms.Create;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Uoms.Get;
using ZARI.Domain.Common;

public sealed record CreateUomCommand(string Code, string Name) : ICommand<Result<UomResponse>>;
