namespace ZARI.Application.Features.Accounting.FiscalYears.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteFiscalYearCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<DeleteFiscalYearCommand>
{
    public async Task<Result> HandleAsync(DeleteFiscalYearCommand command, CancellationToken cancellationToken = default)
    {
        var fiscalYear = await dbContext.FiscalYears.FindAsync([command.Id], cancellationToken);
        if (fiscalYear is null)
            return Result.Failure(Error.NotFound("FiscalYear.NotFound", $"Fiscal year with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("FISCAL_YEARS", FormAction.Delete, cancellationToken))
            return Result.Failure(Error.Forbidden("FiscalYear.Forbidden", "You do not have permission to delete fiscal years."));

        dbContext.FiscalYears.Remove(fiscalYear);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
