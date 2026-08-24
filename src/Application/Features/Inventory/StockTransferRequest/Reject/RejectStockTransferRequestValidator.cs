namespace ZARI.Application.Features.Inventory.StockTransferRequests.Reject;

using FluentValidation;

public sealed class RejectStockTransferRequestValidator : AbstractValidator<RejectStockTransferRequestCommand>
{
    public RejectStockTransferRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(300);
    }
}
