namespace ZARI.Application.Features.Customers.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateCustomerCommandHandler(IAppDbContext dbContext) : ICommandHandler<UpdateCustomerCommand>
{
    public async Task<Result> HandleAsync(UpdateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FindAsync([command.Id], cancellationToken);
        if (customer is null)
            return Result.Failure(Error.NotFound("Customer.NotFound", $"Customer with ID '{command.Id}' was not found."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        customer.Name = command.Name;
        customer.Type = command.Type;
        customer.Email = command.Email;
        customer.Phone = command.Phone;
        customer.BranchId = command.BranchId;
        customer.Status = command.Status;
        customer.Owner = command.Owner;
        customer.Address = command.Address;
        customer.Notes = command.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
