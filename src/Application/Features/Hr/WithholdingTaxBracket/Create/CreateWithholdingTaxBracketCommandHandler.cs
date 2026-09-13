namespace ZARI.Application.Features.Hr.WithholdingTaxBrackets.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateWithholdingTaxBracketCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateWithholdingTaxBracketCommand, Result<WithholdingTaxBracketResponse>>
{
    public async Task<Result<WithholdingTaxBracketResponse>> HandleAsync(CreateWithholdingTaxBracketCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("WITHHOLDING_TAX_BRACKETS", FormAction.Create, cancellationToken))
            return Result.Failure<WithholdingTaxBracketResponse>(Error.Forbidden("WithholdingTaxBracket.Forbidden", "You do not have permission to create withholding tax brackets."));

        var duplicateExists = await dbContext.WithholdingTaxBrackets
            .AnyAsync(w => w.EffectiveDate == command.EffectiveDate && w.SalaryFrom == command.SalaryFrom, cancellationToken);
        if (duplicateExists)
            return Result.Failure<WithholdingTaxBracketResponse>(Error.Conflict("WithholdingTaxBracket.Duplicate", $"A bracket starting at {command.SalaryFrom:N2} effective {command.EffectiveDate:d} already exists."));

        var bracket = new WithholdingTaxBracket
        {
            SalaryFrom = command.SalaryFrom,
            SalaryTo = command.SalaryTo,
            BaseTax = command.BaseTax,
            RateOverExcess = command.RateOverExcess,
            EffectiveDate = command.EffectiveDate,
            Status = command.Status
        };

        dbContext.WithholdingTaxBrackets.Add(bracket);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new WithholdingTaxBracketResponse(bracket.Id, bracket.SalaryFrom, bracket.SalaryTo, bracket.BaseTax, bracket.RateOverExcess, bracket.EffectiveDate, bracket.Status, bracket.CreatedAt);
        return Result.Success(response);
    }
}
