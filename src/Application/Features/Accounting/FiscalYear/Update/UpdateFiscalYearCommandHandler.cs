namespace ZARI.Application.Features.Accounting.FiscalYears.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateFiscalYearCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateFiscalYearCommand>
{
    public async Task<Result> HandleAsync(UpdateFiscalYearCommand command, CancellationToken cancellationToken = default)
    {
        var fiscalYear = await dbContext.FiscalYears.FindAsync([command.Id], cancellationToken);
        if (fiscalYear is null)
            return Result.Failure(Error.NotFound("FiscalYear.NotFound", $"Fiscal year with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("FISCAL_YEARS", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("FiscalYear.Forbidden", "You do not have permission to update fiscal years."));

        var duplicateName = await dbContext.FiscalYears
            .AnyAsync(f => f.Id != command.Id && f.YearName == command.YearName, cancellationToken);

        if (duplicateName)
            return Result.Failure(Error.Conflict("FiscalYear.DuplicateName", $"A fiscal year named '{command.YearName}' already exists."));

        if (command.EndDate < command.StartDate)
            return Result.Failure(Error.Validation("FiscalYear.InvalidDateRange", "End date must be on or after the start date."));

        fiscalYear.YearName = command.YearName;
        fiscalYear.StartDate = command.StartDate;
        fiscalYear.EndDate = command.EndDate;
        fiscalYear.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
