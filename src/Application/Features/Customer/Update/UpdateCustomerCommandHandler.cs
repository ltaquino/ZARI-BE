namespace ZARI.Application.Features.Customers.Update;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class UpdateCustomerCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<UpdateCustomerCommand>
{
    public async Task<Result> HandleAsync(UpdateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FindAsync([command.Id], cancellationToken);
        if (customer is null)
            return Result.Failure(Error.NotFound("Customer.NotFound", $"Customer with ID '{command.Id}' was not found."));

        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMERS", FormAction.Edit, customer.BranchId, cancellationToken))
            return Result.Failure(Error.Forbidden("Customer.Forbidden", "You do not have permission to update customers for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        if (command.ArAccountId is not null)
        {
            var glAccountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == command.ArAccountId, cancellationToken);
            if (!glAccountExists)
                return Result.Failure(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.ArAccountId}' was not found."));
        }

        customer.Name = command.Name;
        customer.Type = command.Type;
        customer.Email = command.Email;
        customer.Phone = command.Phone;
        customer.BranchId = command.BranchId;
        customer.Status = command.Status;
        customer.Owner = command.Owner;
        customer.Address = command.Address;
        customer.Notes = command.Notes;
        customer.ArAccountId = command.ArAccountId;
        customer.PaymentTermsDays = command.PaymentTermsDays;
        customer.StandingDiscountPct = command.StandingDiscountPct;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
