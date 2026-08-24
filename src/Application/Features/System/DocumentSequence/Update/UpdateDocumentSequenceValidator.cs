namespace ZARI.Application.Features.System.DocumentSequences.Update;

using FluentValidation;

public sealed class UpdateDocumentSequenceValidator : AbstractValidator<UpdateDocumentSequenceCommand>
{
    public UpdateDocumentSequenceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty().MaximumLength(25);
        RuleFor(x => x.DocType).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Prefix).NotEmpty().MaximumLength(25);
        RuleFor(x => x.NextNumber).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaddingLength).InclusiveBetween(1, 10);
    }
}
