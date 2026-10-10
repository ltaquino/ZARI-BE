namespace ZARI.Application.Features.RouteSales.DeliveryRoutes.Create;

using FluentValidation;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Shared;

public sealed class CreateDeliveryRouteValidator : AbstractValidator<CreateDeliveryRouteCommand>
{
    public CreateDeliveryRouteValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        DeliveryRouteShared.ApplyRules<CreateDeliveryRouteCommand>(this, x => x.Code, x => x.Name, x => x.Description, x => x.Status, x => x.Stops);
    }
}
