namespace ZARI.Application.Features.Inventory.StockOpnames.ApproveCancellation;

using FluentValidation;

public sealed class ApproveStockOpnameCancellationValidator : AbstractValidator<ApproveStockOpnameCancellationCommand>
{
    public ApproveStockOpnameCancellationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
