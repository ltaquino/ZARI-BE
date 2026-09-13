namespace ZARI.Application.Features.Hr.PagIbigContributionBrackets.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreatePagIbigContributionBracketCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<CreatePagIbigContributionBracketCommand, Result<PagIbigContributionBracketResponse>>
{
    public async Task<Result<PagIbigContributionBracketResponse>> HandleAsync(CreatePagIbigContributionBracketCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("PAGIBIG_BRACKETS", FormAction.Create, cancellationToken))
            return Result.Failure<PagIbigContributionBracketResponse>(Error.Forbidden("PagIbigContributionBracket.Forbidden", "You do not have permission to create Pag-IBIG contribution brackets."));

        var duplicateExists = await dbContext.PagIbigContributionBrackets
            .AnyAsync(s => s.EffectiveDate == command.EffectiveDate && s.SalaryFrom == command.SalaryFrom, cancellationToken);
        if (duplicateExists)
            return Result.Failure<PagIbigContributionBracketResponse>(Error.Conflict("PagIbigContributionBracket.Duplicate", $"A bracket starting at {command.SalaryFrom:N2} effective {command.EffectiveDate:d} already exists."));

        var bracket = new PagIbigContributionBracket
        {
            SalaryFrom = command.SalaryFrom,
            SalaryTo = command.SalaryTo,
            EmployeeShare = command.EmployeeShare,
            EmployerShare = command.EmployerShare,
            EffectiveDate = command.EffectiveDate,
            Status = command.Status
        };

        dbContext.PagIbigContributionBrackets.Add(bracket);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new PagIbigContributionBracketResponse(bracket.Id, bracket.SalaryFrom, bracket.SalaryTo, bracket.EmployeeShare, bracket.EmployerShare, bracket.EffectiveDate, bracket.Status, bracket.CreatedAt);
        return Result.Success(response);
    }
}
