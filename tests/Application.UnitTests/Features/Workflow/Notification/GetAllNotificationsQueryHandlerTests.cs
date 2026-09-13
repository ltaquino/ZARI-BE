namespace ZARI.Application.UnitTests.Features.Workflow.Notification;

using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllNotificationsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Notifications_With_ReadBy()
    {
        await using var db = TestDbContextFactory.Create();
        var notification = new Notification { EntityType = "SALES_ORDER", EntityId = "doc-1", BranchId = "br-1", Type = "CREATED", Category = "ACTIVITY", Message = "created this document" };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.NotificationReads.Add(new NotificationRead { NotificationId = notification.Id, UserId = "manager", ReadAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllNotificationsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllNotificationsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value![0].ReadBy.Should().Contain("manager");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("NOTIFICATIONS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllNotificationsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllNotificationsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
