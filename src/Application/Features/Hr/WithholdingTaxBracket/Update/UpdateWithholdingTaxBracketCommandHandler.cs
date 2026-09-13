namespace ZARI.Application.Features.Hr.WithholdingTaxBrackets.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateWithholdingTaxBracketCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateWithholdingTaxBracketCommand>
{
    public async Task<Result> HandleAsync(UpdateWithholdingTaxBracketCommand command, CancellationToken cancellationToken = default)
    {
        var bracket = await dbContext.WithholdingTaxBrackets.FindAsync([command.Id], cancellationToken);
        if (bracket is null)
            return Result.Failure(Error.NotFound("WithholdingTaxBracket.NotFound", $"Withholding tax bracket with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("WITHHOLDING_TAX_BRACKETS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("WithholdingTaxBracket.Forbidden", "You do not have permission to update withholding tax brackets."));

        var duplicateExists = await dbContext.WithholdingTaxBrackets
            .AnyAsync(w => w.Id != command.Id && w.EffectiveDate == command.EffectiveDate && w.SalaryFrom == command.SalaryFrom, cancellationToken);
        if (duplicateExists)
            return Result.Failure(Error.Conflict("WithholdingTaxBracket.Duplicate", $"A bracket starting at {command.SalaryFrom:N2} effective {command.EffectiveDate:d} already exists."));

        bracket.SalaryFrom = command.SalaryFrom;
        bracket.SalaryTo = command.SalaryTo;
        bracket.BaseTax = command.BaseTax;
        bracket.RateOverExcess = command.RateOverExcess;
        bracket.EffectiveDate = command.EffectiveDate;
        bracket.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
