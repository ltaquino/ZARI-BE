namespace ZARI.Application.UnitTests.Features.Loan.LoanApplication;

using ZARI.Application.Features.Loan.LoanApplications.Create;
using ZARI.Application.Features.Loan.LoanApplications.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using Result = ZARI.Domain.Common.Result;

public sealed class UpdateLoanApplicationCommandHandlerTests
{
    private static UpdateLoanApplicationCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_APPLICATION", "x", "br-1", "UPDATED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static UpdateLoanApplicationCommand Command(Guid id, string branchId, Guid customerId, Guid productId, decimal principal = 10000, int term = 6) =>
        new(id, branchId, customerId, productId, DateTimeOffset.UtcNow, principal, term, "purpose", null, "tester", [], []);

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanApplication app, string branchId, Guid customerId, Guid productId)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var app = new LoanApplication
        {
            ApplicationNo = "LOAN-APP-0001", BranchId = branch.Id, CustomerId = customer.Id, LoanProductId = product.Id,
            ApplicationDate = DateTimeOffset.UtcNow, RequestedPrincipal = 10000, RequestedTermMonths = 6, Status = status
        };
        db.LoanApplications.Add(app);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, app, branch.Id, customer.Id, product.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Application_When_Draft()
    {
        var (db, app, branchId, customerId, productId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(app.Id, branchId, customerId, productId, principal: 15000), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RequestedPrincipal.Should().Be(15000);
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
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, app, branchId, customerId, productId) = await Seed(status: "PENDING_APPROVAL");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(app.Id, branchId, customerId, productId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanApplication.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, app, branchId, customerId, productId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_APPLICATIONS", FormAction.Edit, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(Command(app.Id, branchId, customerId, productId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
