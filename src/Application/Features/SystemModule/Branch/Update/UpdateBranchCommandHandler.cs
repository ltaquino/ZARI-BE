namespace ZARI.Application.Features.SystemModule.Branches.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateBranchCommandHandler(IAppDbContext dbContext) : ICommandHandler<UpdateBranchCommand>
{
    public async Task<Result> HandleAsync(UpdateBranchCommand command, CancellationToken cancellationToken = default)
    {
        var branch = await dbContext.Branches.FindAsync([command.Id], cancellationToken);
        if (branch is null)
            return Result.Failure(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.Id}' was not found."));

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

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
