namespace ZARI.Application.Features.SystemModule.Branches.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateBranchCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateBranchCommand>
{
    public async Task<Result> HandleAsync(UpdateBranchCommand command, CancellationToken cancellationToken = default)
    {
        var branch = await dbContext.Branches.FindAsync([command.Id], cancellationToken);
        if (branch is null)
            return Result.Failure(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionAsync("BRANCHES", FormAction.Edit, cancellationToken))
            return Result.Failure(Error.Forbidden("Branch.Forbidden", "You do not have permission to update branches."));

        var duplicateCode = await dbContext.Branches.AnyAsync(b => b.Id != command.Id && b.Code == command.Code, cancellationToken);
        if (duplicateCode)
            return Result.Failure(Error.Conflict("Branch.DuplicateCode", $"A branch with code '{command.Code}' already exists."));

        if (command.IsHeadOffice)
        {
            await dbContext.Branches.Where(b => b.Id != command.Id && b.IsHeadOffice)
                .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.IsHeadOffice, false), cancellationToken);
        }

        branch.Name = command.Name;
        branch.Code = command.Code;
        branch.City = command.City;
        branch.Address = command.Address;
        branch.Phone = command.Phone;
        branch.Status = command.Status;
        branch.IsHeadOffice = command.IsHeadOffice;
        branch.BirBranchCode = command.BirBranchCode;
        branch.PosPermitNumber = command.PosPermitNumber;
        branch.PosPermitDateIssued = command.PosPermitDateIssued;
        branch.MachineIdentificationNumber = command.MachineIdentificationNumber;
        branch.MachineSerialNumber = command.MachineSerialNumber;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
