namespace ZARI.Application.Features.Sales.PosTerminals.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosTerminals.Get;
using ZARI.Application.Features.Sales.PosTerminals.Shared;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreatePosTerminalCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreatePosTerminalCommand, Result<PosTerminalResponse>>
{
    public async Task<Result<PosTerminalResponse>> HandleAsync(CreatePosTerminalCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("POS_TERMINALS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<PosTerminalResponse>(Error.Forbidden("PosTerminal.Forbidden", "You do not have permission to create POS terminals for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<PosTerminalResponse>(Error.NotFound("PosTerminal.BranchNotFound", $"Branch '{command.BranchId}' was not found."));

        var codeExists = await dbContext.PosTerminals.AnyAsync(t => t.BranchId == command.BranchId && t.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<PosTerminalResponse>(Error.Conflict("PosTerminal.DuplicateCode", $"A POS terminal with code '{command.Code}' already exists at this branch."));

        if (command.HasOwnSeries && await PosTerminalSeriesRules.CheckPrefixUniqueAsync(dbContext, null, command.OrPrefix!, cancellationToken) is { } prefixError)
            return Result.Failure<PosTerminalResponse>(prefixError);

        var terminal = new PosTerminal
        {
            BranchId = command.BranchId,
            Code = command.Code,
            Name = command.Name,
            MachineIdentificationNumber = command.MachineIdentificationNumber,
            MachineSerialNumber = command.MachineSerialNumber,
            PosPermitNumber = command.PosPermitNumber,
            PosPermitDateIssued = command.PosPermitDateIssued,
            Status = command.Status,
            HasOwnSeries = command.HasOwnSeries,
            OrPrefix = command.HasOwnSeries ? command.OrPrefix : null,
            OrNextNumber = command.OrNextNumber ?? 1,
            OrPaddingLength = command.OrPaddingLength ?? 6,
        };

        dbContext.PosTerminals.Add(terminal);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = PosTerminalMapper.ToResponse(terminal);
        return Result.Success(response);
    }
}
