namespace ZARI.Application.Features.Inventory.StockTransferRequests.Approve;

using FluentValidation;

public sealed class ApproveStockTransferRequestValidator : AbstractValidator<ApproveStockTransferRequestCommand>
{
    public ApproveStockTransferRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty().MaximumLength(150);
    }
}
