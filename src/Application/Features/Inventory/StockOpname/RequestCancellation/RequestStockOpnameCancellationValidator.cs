namespace ZARI.Application.Features.Inventory.StockOpnames.RequestCancellation;

using FluentValidation;

public sealed class RequestStockOpnameCancellationValidator : AbstractValidator<RequestStockOpnameCancellationCommand>
{
    public RequestStockOpnameCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}
