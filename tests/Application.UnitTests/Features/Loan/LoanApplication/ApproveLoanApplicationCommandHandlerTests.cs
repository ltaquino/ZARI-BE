namespace ZARI.Application.UnitTests.Features.Loan.LoanApplication;

using ZARI.Application.Features.Loan.LoanApplications.Approve;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using Result = ZARI.Domain.Common.Result;

public sealed class ApproveLoanApplicationCommandHandlerTests
{
    private static ApproveLoanApplicationCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "LOAN_APPLICATION", "x", "br-1", "u1", DateTimeOffset.UtcNow, "APPROVED", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_APPLICATION", "x", "br-1", "APPROVED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanApplication app)> Seed(string status = "PENDING_APPROVAL", bool withApprovalRequest = true)
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
        if (withApprovalRequest)
        {
            db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "LOAN_APPLICATION", EntityId = app.Id.ToString(), BranchId = branch.Id, RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        return (db, app);
    }

    [Fact]
    public async Task HandleAsync_Should_Approve_When_PendingApproval()
    {
        var (db, app) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanApplicationCommand(app.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("APPROVED");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanApplicationCommand(Guid.NewGuid(), "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, app) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_APPLICATIONS", FormAction.Approve, app.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new ApproveLoanApplicationCommand(app.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_PendingApproval()
    {
        var (db, app) = await Seed(status: "DRAFT", withApprovalRequest: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanApplicationCommand(app.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanApplication.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_No_ApprovalRequest_Found()
    {
        var (db, app) = await Seed(withApprovalRequest: false);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new ApproveLoanApplicationCommand(app.Id, "approver1", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.NotFound");
        await db.DisposeAsync();
    }
}
