namespace ZARI.Application.Features.Hr.PagIbigContributionBrackets.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdatePagIbigContributionBracketCommandHandler(IAppDbContext dbContext, IPermissionService permissionService)
    : ICommandHandler<UpdatePagIbigContributionBracketCommand>
{
    public async Task<Result> HandleAsync(UpdatePagIbigContributionBracketCommand command, CancellationToken cancellationToken = default)
    {
        var bracket = await dbContext.PagIbigContributionBrackets.FindAsync([command.Id], cancellationToken);
        if (bracket is null)
            return Result.Failure(Error.NotFound("PagIbigContributionBracket.NotFound", $"Pag-IBIG contribution bracket with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("PAGIBIG_BRACKETS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("PagIbigContributionBracket.Forbidden", "You do not have permission to update Pag-IBIG contribution brackets."));

        var duplicateExists = await dbContext.PagIbigContributionBrackets
            .AnyAsync(s => s.Id != command.Id && s.EffectiveDate == command.EffectiveDate && s.SalaryFrom == command.SalaryFrom, cancellationToken);
        if (duplicateExists)
            return Result.Failure(Error.Conflict("PagIbigContributionBracket.Duplicate", $"A bracket starting at {command.SalaryFrom:N2} effective {command.EffectiveDate:d} already exists."));

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
