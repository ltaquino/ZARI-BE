namespace ZARI.Application.Features.Sales.PosTerminals.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosTerminals.Shared;
using ZARI.Domain.Common;

public sealed class UpdatePosTerminalCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdatePosTerminalCommand>
{
    public async Task<Result> HandleAsync(UpdatePosTerminalCommand command, CancellationToken cancellationToken = default)
    {
        var terminal = await dbContext.PosTerminals.FindAsync([command.Id], cancellationToken);
        if (terminal is null)
            return Result.Failure(Error.NotFound("PosTerminal.NotFound", $"POS terminal with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("POS_TERMINALS", FormAction.Edit, terminal.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("PosTerminal.Forbidden", "You do not have permission to update POS terminals for this branch."));

        var duplicateCode = await dbContext.PosTerminals
            .AnyAsync(t => t.Id != command.Id && t.BranchId == terminal.BranchId && t.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("PosTerminal.DuplicateCode", $"A POS terminal with code '{command.Code}' already exists at this branch."));

        // A BIR series is fixed once it has issued a number: changing the prefix/next number/scope after
        // that would break the unbroken sequence an examiner expects. Before then (e.g. setting the
        // first number from the BIR permit) it's freely editable.
        var orNextNumber = command.OrNextNumber ?? terminal.OrNextNumber;
        var orPadding = command.OrPaddingLength ?? terminal.OrPaddingLength;
        var orPrefix = command.HasOwnSeries ? command.OrPrefix : null;
        var seriesChanged = command.HasOwnSeries != terminal.HasOwnSeries
            || (command.HasOwnSeries && (orPrefix != terminal.OrPrefix || orNextNumber != terminal.OrNextNumber || orPadding != terminal.OrPaddingLength));
        if (seriesChanged)
        {
            if (await dbContext.SalesInvoices.AnyAsync(i => i.PosTerminalId == terminal.Id && i.BirOrSeriesNumber != null, cancellationToken))
                return Result.Failure(Error.Conflict("PosTerminal.SeriesLocked",
                    "This terminal has already issued invoice numbers — its invoice series can no longer be changed."));
            if (!command.HasOwnSeries && await dbContext.Vehicles.AnyAsync(v => v.PosTerminalId == terminal.Id, cancellationToken))
                return Result.Failure(Error.Validation("PosTerminal.VehicleNeedsOwnSeries",
                    "This terminal belongs to a vehicle — a truck handheld must keep its own invoice series."));
            if (command.HasOwnSeries && await PosTerminalSeriesRules.CheckPrefixUniqueAsync(dbContext, terminal.Id, orPrefix!, cancellationToken) is { } prefixError)
                return Result.Failure(prefixError);
        }

        terminal.HasOwnSeries = command.HasOwnSeries;
        terminal.OrPrefix = orPrefix;
        terminal.OrNextNumber = orNextNumber;
        terminal.OrPaddingLength = orPadding;
        terminal.Code = command.Code;
        terminal.Name = command.Name;
        terminal.MachineIdentificationNumber = command.MachineIdentificationNumber;
        terminal.MachineSerialNumber = command.MachineSerialNumber;
        terminal.PosPermitNumber = command.PosPermitNumber;
        terminal.PosPermitDateIssued = command.PosPermitDateIssued;
        terminal.Status = command.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
