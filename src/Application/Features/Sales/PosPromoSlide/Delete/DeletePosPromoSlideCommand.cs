namespace ZARI.Application.Features.Sales.PosPromoSlides.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeletePosPromoSlideCommand(Guid Id) : ICommand;
