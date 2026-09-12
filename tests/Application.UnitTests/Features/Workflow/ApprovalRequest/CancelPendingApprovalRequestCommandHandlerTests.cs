namespace ZARI.Application.UnitTests.Features.Workflow.ApprovalRequest;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.Workflow.ApprovalRequests.CancelPending;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

/// <summary>
/// The actual cancel (compare-and-swap PENDING -> CANCELLED) uses `ExecuteUpdateAsync`, unsupported
/// by the InMemory provider — same gap as DecideApprovalRequestCommandHandler. Only the two
/// no-op-by-design paths (nothing to cancel; already-decided) are covered here.
/// </summary>
public sealed class CancelPendingApprovalRequestCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_NoOp_When_No_Request_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CancelPendingApprovalRequestCommandHandler(db);

        var result = await handler.HandleAsync(new CancelPendingApprovalRequestCommand("SALES_ORDER", "doc-missing"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_NoOp_When_Latest_Request_Is_Not_Pending()
    {
        await using var db = TestDbContextFactory.Create();
        db.ApprovalRequests.Add(new ApprovalRequest
        {
            EntityType = "SALES_ORDER", EntityId = "doc-1", BranchId = "br-1",
            RequestedBy = "encoder", RequestedAt = DateTimeOffset.UtcNow, Status = "APPROVED", RequestType = "SUBMIT"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CancelPendingApprovalRequestCommandHandler(db);

        var result = await handler.HandleAsync(new CancelPendingApprovalRequestCommand("SALES_ORDER", "doc-1"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.ApprovalRequests.FirstAsync(TestContext.Current.CancellationToken)).Status.Should().Be("APPROVED");
    }
}
