namespace ZARI.Application.Features.Customers.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateCustomerCommandHandler(IAppDbContext dbContext, IPermissionService permissionService) : ICommandHandler<CreateCustomerCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> HandleAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.HasPermissionOnBranchAsync("CUSTOMERS", FormAction.Create, command.BranchId, cancellationToken))
            return Result.Failure<CustomerResponse>(Error.Forbidden("Customer.Forbidden", "You do not have permission to create customers for this branch."));

        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<CustomerResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

        if (command.ArAccountId is not null)
        {
            var glAccountExists = await dbContext.GlAccounts.AnyAsync(a => a.Id == command.ArAccountId, cancellationToken);
            if (!glAccountExists)
                return Result.Failure<CustomerResponse>(Error.NotFound("GlAccount.NotFound", $"GL account with ID '{command.ArAccountId}' was not found."));
        }

        var customer = new Customer
        {
            Name = command.Name,
            Type = command.Type,
            Email = command.Email,
            Phone = command.Phone,
            BranchId = command.BranchId,
            Status = command.Status,
            Owner = command.Owner,
            Address = command.Address,
            Notes = command.Notes,
            ArAccountId = command.ArAccountId,
            PaymentTermsDays = command.PaymentTermsDays,
            StandingDiscountPct = command.StandingDiscountPct
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CustomerResponse(customer.Id, customer.Name, customer.Type, customer.Email, customer.Phone,
            customer.BranchId, customer.Status, customer.Owner, customer.Address, customer.Notes,
            customer.ArAccountId, customer.PaymentTermsDays, customer.StandingDiscountPct, customer.CreatedAt);
        return Result.Success(response);
    }
}
