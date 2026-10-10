namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.Update;

using FluentValidation;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Shared;

public sealed class UpdateDeliveryRouteValidator : AbstractValidator<UpdateDeliveryRouteCommand>
{
    public UpdateDeliveryRouteValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        DeliveryRouteShared.ApplyRules<UpdateDeliveryRouteCommand>(this, x => x.Code, x => x.Name, x => x.Description, x => x.Status, x => x.Stops);
    }
}
