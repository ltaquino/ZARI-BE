namespace ZARI.Application.Features.Inventory.ItemBranchSettings.Update;

using FluentValidation;

public sealed class UpdateItemBranchSettingValidator : AbstractValidator<UpdateItemBranchSettingCommand>
{
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public UpdateItemBranchSettingValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0).When(x => x.SellingPrice.HasValue);
        RuleFor(x => x.MarkupPct).GreaterThanOrEqualTo(0).When(x => x.MarkupPct.HasValue);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
}
