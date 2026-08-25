namespace ZARI.Application.Features.Accounting.FiscalYears.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Accounting.FiscalYears.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateFiscalYearCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateFiscalYearCommand, Result<FiscalYearResponse>>
{
    public async Task<Result<FiscalYearResponse>> HandleAsync(CreateFiscalYearCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionAsync("FISCAL_YEARS", FormAction.Create, cancellationToken))
            return Result.Failure<FiscalYearResponse>(Error.Forbidden("FiscalYear.Forbidden", "You do not have permission to create fiscal years."));

        var nameExists = await dbContext.FiscalYears.AnyAsync(f => f.YearName == command.YearName, cancellationToken);
        if (nameExists)
            return Result.Failure<FiscalYearResponse>(Error.Conflict("FiscalYear.DuplicateName", $"A fiscal year named '{command.YearName}' already exists."));

        if (command.EndDate < command.StartDate)
            return Result.Failure<FiscalYearResponse>(Error.Validation("FiscalYear.InvalidDateRange", "End date must be on or after the start date."));

        var fiscalYear = new FiscalYear
        {
            YearName = command.YearName,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            Status = command.Status
        };

        dbContext.FiscalYears.Add(fiscalYear);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new FiscalYearResponse(fiscalYear.Id, fiscalYear.YearName, fiscalYear.StartDate, fiscalYear.EndDate, fiscalYear.Status, fiscalYear.CreatedAt);
        return Result.Success(response);
    }
}
