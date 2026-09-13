namespace ZARI.Application.UnitTests.Features.Workflow.Notification;

using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.UnitTests.TestSupport;

public sealed class CreateNotificationCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Notification()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateNotificationCommandHandler(db);

        var result = await handler.HandleAsync(
            new CreateNotificationCommand("SALES_ORDER", "doc-1", "br-1", "CREATED", "ACTIVITY", "created this document", "encoder"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().Be("created this document");
        result.Value!.ReadBy.Should().BeEmpty();
        db.Notifications.Should().ContainSingle();
    }
}
