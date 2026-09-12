namespace ZARI.Application.UnitTests.Features.Workflow.ApprovalRequest;

using ZARI.Application.Features.Workflow.ApprovalRequests.Submit;
using ZARI.Application.UnitTests.TestSupport;

public sealed class SubmitForApprovalCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_A_Pending_Request()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new SubmitForApprovalCommandHandler(db);

        var result = await handler.HandleAsync(new SubmitForApprovalCommand("SALES_ORDER", "doc-1", "br-1", "encoder", null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING");
        result.Value!.RequestType.Should().Be("SUBMIT");
        db.ApprovalRequests.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_Use_Explicit_RequestType_When_Given()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new SubmitForApprovalCommandHandler(db);

        var result = await handler.HandleAsync(new SubmitForApprovalCommand("SALES_ORDER", "doc-1", "br-1", "encoder", "CANCEL", "reason"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RequestType.Should().Be("CANCEL");
        result.Value!.Reason.Should().Be("reason");
    }
}
