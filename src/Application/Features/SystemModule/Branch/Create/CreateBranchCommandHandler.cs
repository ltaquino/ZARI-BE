namespace ZARI.Application.Features.SystemModule.Branches.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.SystemModule.Branches.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateBranchCommandHandler(IAppDbContext dbContext) : ICommandHandler<CreateBranchCommand, Result<BranchResponse>>
{
    public async Task<Result<BranchResponse>> HandleAsync(CreateBranchCommand command, CancellationToken cancellationToken = default)
    {
        var codeExists = await dbContext.Branches.AnyAsync(b => b.Code == command.Code, cancellationToken);
        if (codeExists)
            return Result.Failure<BranchResponse>(Error.Conflict("Branch.DuplicateCode", $"A branch with code '{command.Code}' already exists."));

        var branch = new Branch
        {
            Id = $"br-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            Name = command.Name,
            Code = command.Code,
            City = command.City,
            Address = command.Address,
            Phone = command.Phone,
            Status = command.Status,
            IsHeadOffice = command.IsHeadOffice
        };

        if (command.IsHeadOffice)
        {
            await dbContext.Branches.Where(b => b.IsHeadOffice)
                .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.IsHeadOffice, false), cancellationToken);
        }

        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new BranchResponse(branch.Id, branch.Name, branch.Code, branch.City, branch.Address, branch.Phone, branch.Status, branch.IsHeadOffice);
        return Result.Success(response);
    }
}
