namespace ZARI.Application.Features.Inventory.StockTransferRequests.Submit;

using FluentValidation;

public sealed class SubmitStockTransferRequestValidator : AbstractValidator<SubmitStockTransferRequestCommand>
{
    public SubmitStockTransferRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RequestedBy).NotEmpty().MaximumLength(150);
    }
}
