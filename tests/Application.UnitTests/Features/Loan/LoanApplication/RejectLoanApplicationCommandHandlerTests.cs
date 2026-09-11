namespace ZARI.Application.UnitTests.Features.Loan.LoanApplication;

using ZARI.Application.Features.Loan.LoanApplications.Reject;
using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using Result = ZARI.Domain.Common.Result;

public sealed class RejectLoanApplicationCommandHandlerTests
{
    private static RejectLoanApplicationCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<DecideApprovalRequestCommand, Result<ApprovalRequestResponse>>(Result.Success(new ApprovalRequestResponse(Guid.NewGuid(), "LOAN_APPLICATION", "x", "br-1", "u1", DateTimeOffset.UtcNow, "REJECTED", "SUBMIT", null, [], DateTimeOffset.UtcNow))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_APPLICATION", "x", "br-1", "REJECTED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanApplication app)> Seed(string status = "PENDING_APPROVAL")
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
        db.ApprovalRequests.Add(new ApprovalRequest { EntityType = "LOAN_APPLICATION", EntityId = app.Id.ToString(), BranchId = branch.Id, RequestedBy = "u1", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, app);
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Back_To_Draft()
    {
        var (db, app) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RejectLoanApplicationCommand(app.Id, "approver1", "fix this"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_PendingApproval()
    {
        var (db, app) = await Seed(status: "APPROVED");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(new RejectLoanApplicationCommand(app.Id, "approver1", "fix"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanApplication.NotPendingApproval");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, app) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_APPLICATIONS", FormAction.Approve, app.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(new RejectLoanApplicationCommand(app.Id, "approver1", "fix"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
