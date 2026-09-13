namespace ZARI.Application.UnitTests.Features.Workflow.ApprovalRequest;

using ZARI.Application.Features.Workflow.ApprovalRequests.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllApprovalRequestsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Requests_With_Actions_Ordered_By_Requested_At_Descending()
    {
        await using var db = TestDbContextFactory.Create();
        var older = new ApprovalRequest { EntityType = "SALES_ORDER", EntityId = "doc-1", BranchId = "br-1", RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow.AddDays(-1), Status = "PENDING", RequestType = "SUBMIT" };
        db.ApprovalRequests.Add(older);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var newer = new ApprovalRequest { EntityType = "SALES_ORDER", EntityId = "doc-2", BranchId = "br-1", RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "APPROVED", RequestType = "SUBMIT" };
        db.ApprovalRequests.Add(newer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ApprovalActions.Add(new ApprovalAction { ApprovalRequestId = newer.Id, ApproverUserId = "manager", Action = "Approve", ActionAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllApprovalRequestsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllApprovalRequestsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(r => r.EntityId).Should().ContainInOrder("doc-2", "doc-1");
        result.Value![0].Actions.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("APPROVAL_REQUESTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllApprovalRequestsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllApprovalRequestsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
