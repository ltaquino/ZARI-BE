namespace ZARI.Application.Features.Hr.SssContributionBrackets.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.SssContributionBrackets.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateSssContributionBracketCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreateSssContributionBracketCommand, Result<SssContributionBracketResponse>>
{
    public async Task<Result<SssContributionBracketResponse>> HandleAsync(CreateSssContributionBracketCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("SSS_CONTRIBUTION_BRACKETS", FormAction.Create, cancellationToken))
            return Result.Failure<SssContributionBracketResponse>(Error.Forbidden("SssContributionBracket.Forbidden", "You do not have permission to create SSS contribution brackets."));

        var duplicateExists = await dbContext.SssContributionBrackets
            .AnyAsync(s => s.EffectiveDate == command.EffectiveDate && s.SalaryFrom == command.SalaryFrom, cancellationToken);
        if (duplicateExists)
            return Result.Failure<SssContributionBracketResponse>(Error.Conflict("SssContributionBracket.Duplicate", $"A bracket starting at {command.SalaryFrom:N2} effective {command.EffectiveDate:d} already exists."));

        var bracket = new SssContributionBracket
        {
            SalaryFrom = command.SalaryFrom,
            SalaryTo = command.SalaryTo,
            EmployeeShare = command.EmployeeShare,
            EmployerShare = command.EmployerShare,
            EffectiveDate = command.EffectiveDate,
            Status = command.Status
        };

        dbContext.SssContributionBrackets.Add(bracket);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new SssContributionBracketResponse(bracket.Id, bracket.SalaryFrom, bracket.SalaryTo, bracket.EmployeeShare, bracket.EmployerShare, bracket.EffectiveDate, bracket.Status, bracket.CreatedAt);
        return Result.Success(response);
    }
}
