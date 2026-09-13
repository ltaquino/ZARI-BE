namespace ZARI.Application.UnitTests.Features.Workflow.ApprovalRequest;

using ZARI.Application.Features.Workflow.ApprovalRequests.Decide;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// The actual decide (compare-and-swap PENDING -> APPROVED/REJECTED) and already-decided-conflict
/// paths both go through `ExecuteUpdateAsync`, unsupported by EF Core's InMemory provider — same gap
/// documented throughout this suite. Only the two guard clauses that run before that point
/// (not-found, self-approval) are covered here; every caller of this handler fakes it entirely in
/// its own tests for exactly this reason.
/// </summary>
public sealed class DecideApprovalRequestCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DecideApprovalRequestCommandHandler(db);

        var result = await handler.HandleAsync(new DecideApprovalRequestCommand(Guid.NewGuid(), "manager", "Approve", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Approver_Is_The_Requester()
    {
        await using var db = TestDbContextFactory.Create();
        var request = new ApprovalRequest
        {
            EntityType = "SALES_ORDER", EntityId = "doc-1", BranchId = "br-1",
            RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "PENDING", RequestType = "SUBMIT"
        };
        db.ApprovalRequests.Add(request);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DecideApprovalRequestCommandHandler(db);

        var result = await handler.HandleAsync(new DecideApprovalRequestCommand(request.Id, "encoder", "Approve", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("ApprovalRequest.SelfApproval");
    }
}
