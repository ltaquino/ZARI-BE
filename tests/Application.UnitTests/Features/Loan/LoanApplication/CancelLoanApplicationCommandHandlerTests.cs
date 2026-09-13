namespace ZARI.Application.UnitTests.Features.Loan.LoanApplication;

using ZARI.Application.Features.Loan.LoanApplications.Cancel;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using Result = ZARI.Domain.Common.Result;

public sealed class CancelLoanApplicationCommandHandlerTests
{
    private static CancelLoanApplicationCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<CancelPendingApprovalRequestCommand, Result>(Result.Success()),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_APPLICATION", "x", "br-1", "CANCELLED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanApplication app)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var app = new LoanApplication { ApplicationNo = "A1", BranchId = branch.Id, CustomerId = customer.Id, LoanProductId = product.Id, ApplicationDate = DateTimeOffset.UtcNow, RequestedPrincipal = 10000, RequestedTermMonths = 6, Status = status };
        db.LoanApplications.Add(app);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, app);
    }

    [Fact]
    public async Task HandleAsync_Should_Cancel_When_Draft()
    {
        var (db, app) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new CancelLoanApplicationCommand(app.Id, "u1", "no longer needed"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("CANCELLED");
        result.Value.CancelReason.Should().Be("no longer needed");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Already_Cancelled()
    {
        var (db, app) = await Seed(status: "CANCELLED");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new CancelLoanApplicationCommand(app.Id, "u1", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanApplication.AlreadyCancelled");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, app) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_APPLICATIONS", FormAction.Cancel, app.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new CancelLoanApplicationCommand(app.Id, "u1", "reason"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
