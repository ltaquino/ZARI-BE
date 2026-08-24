namespace ZARI.Application.Features.Inventory.StockOpnames.Post;

using FluentValidation;

public sealed class PostStockOpnameValidator : AbstractValidator<PostStockOpnameCommand>
{
    public PostStockOpnameValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PostedBy).NotEmpty().MaximumLength(150);
    }
}
