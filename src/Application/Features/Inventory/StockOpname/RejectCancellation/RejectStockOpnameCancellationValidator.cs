namespace ZARI.Application.Features.Inventory.StockOpnames.RejectCancellation;

using FluentValidation;

public sealed class RejectStockOpnameCancellationValidator : AbstractValidator<RejectStockOpnameCancellationCommand>
{
    public RejectStockOpnameCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
