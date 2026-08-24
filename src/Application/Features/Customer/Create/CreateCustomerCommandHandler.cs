namespace ZARI.Application.Features.Customers.Create;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Customers.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateCustomerCommandHandler(IAppDbContext dbContext) : ICommandHandler<CreateCustomerCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> HandleAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var branchExists = await dbContext.Branches.AnyAsync(b => b.Id == command.BranchId, cancellationToken);
        if (!branchExists)
            return Result.Failure<CustomerResponse>(Error.NotFound("Branch.NotFound", $"Branch with ID '{command.BranchId}' was not found."));

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
            Notes = command.Notes
        };

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new CustomerResponse(customer.Id, customer.Name, customer.Type, customer.Email, customer.Phone,
            customer.BranchId, customer.Status, customer.Owner, customer.Address, customer.Notes, customer.CreatedAt);
        return Result.Success(response);
    }
}
