namespace ZARI.Application.Features.Customers.Delete;

using ZARI.Application.Abstractions.Data;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed class DeleteCustomerCommandHandler(IAppDbContext dbContext) : ICommandHandler<DeleteCustomerCommand>
{
    public async Task<Result> HandleAsync(DeleteCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FindAsync([command.Id], cancellationToken);
        if (customer is null)
            return Result.Failure(Error.NotFound("Customer.NotFound", $"Customer with ID '{command.Id}' was not found."));

        dbContext.Customers.Remove(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
