namespace ZARI.Application.Features.Sales.DeliveryOrders.Update;

using FluentValidation;

public sealed class UpdateDeliveryOrderValidator : AbstractValidator<UpdateDeliveryOrderCommand>
{
    public UpdateDeliveryOrderValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.DeliveryDate).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(300);

        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one item line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ItemId).NotEmpty();
            line.RuleFor(l => l.UomId).NotEmpty();
            line.RuleFor(l => l.QtyShipped).GreaterThan(0);
        });
    }
}
