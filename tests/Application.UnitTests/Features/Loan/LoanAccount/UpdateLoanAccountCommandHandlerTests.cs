namespace ZARI.Application.UnitTests.Features.Loan.LoanAccount;

using ZARI.Application.Features.Loan.LoanAccounts.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateLoanAccountCommandHandlerTests
{
    private static UpdateLoanAccountCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_ACCOUNT", "x", "br-1", "UPDATED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanAccount account, string branchId, Guid customerId, Guid productId)> Seed(string status = "PENDING_DISBURSEMENT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: status);
        db.LoanAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, account, branch.Id, customer.Id, product.Id);
    }

    private static UpdateLoanAccountCommand Command(Guid id, string branchId, Guid customerId, Guid productId, decimal principal = 15000, int term = 6) =>
        new(id, branchId, customerId, productId, principal, term, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMonths(1), null, null, null, null, "tester");

    [Fact]
    public async Task HandleAsync_Should_Update_And_Regenerate_Schedule()
    {
        var (db, account, branchId, customerId, productId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(account.Id, branchId, customerId, productId, principal: 18000), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PrincipalAmount.Should().Be(18000);
        result.Value.ScheduleLines.Should().HaveCount(6);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, _, branchId, customerId, productId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), branchId, customerId, productId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Editable()
    {
        var (db, account, branchId, customerId, productId) = await Seed(status: "ACTIVE");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(account.Id, branchId, customerId, productId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanAccount.NotEditable");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, account, branchId, customerId, productId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_ACCOUNTS", FormAction.Edit, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(Command(account.Id, branchId, customerId, productId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
