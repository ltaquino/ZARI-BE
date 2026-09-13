namespace ZARI.Application.Features.Hr.SssContributionBrackets.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateSssContributionBracketCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdateSssContributionBracketCommand>
{
    public async Task<Result> HandleAsync(UpdateSssContributionBracketCommand command, CancellationToken cancellationToken = default)
    {
        var bracket = await dbContext.SssContributionBrackets.FindAsync([command.Id], cancellationToken);
        if (bracket is null)
            return Result.Failure(Error.NotFound("SssContributionBracket.NotFound", $"SSS contribution bracket with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("SSS_CONTRIBUTION_BRACKETS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("SssContributionBracket.Forbidden", "You do not have permission to update SSS contribution brackets."));

        var duplicateExists = await dbContext.SssContributionBrackets
            .AnyAsync(s => s.Id != command.Id && s.EffectiveDate == command.EffectiveDate && s.SalaryFrom == command.SalaryFrom, cancellationToken);
        if (duplicateExists)
            return Result.Failure(Error.Conflict("SssContributionBracket.Duplicate", $"A bracket starting at {command.SalaryFrom:N2} effective {command.EffectiveDate:d} already exists."));

        bracket.SalaryFrom = command.SalaryFrom;
        bracket.SalaryTo = command.SalaryTo;
        bracket.EmployeeShare = command.EmployeeShare;
        bracket.EmployerShare = command.EmployerShare;
        bracket.EffectiveDate = command.EffectiveDate;
        bracket.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
