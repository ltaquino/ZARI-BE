namespace ZARI.Application.Features.Inventory.StockLocationTransfers.Post;

using FluentValidation;

public sealed class PostStockLocationTransferValidator : AbstractValidator<PostStockLocationTransferCommand>
{
    public PostStockLocationTransferValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PostedBy).NotEmpty().MaximumLength(150);
    }
}
