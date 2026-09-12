namespace ZARI.Application.UnitTests.Features.Workflow.Notification;

using ZARI.Application.Features.Workflow.Notifications.MarkRead;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

public sealed class MarkNotificationsReadCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Mark_Notifications_Read()
    {
        await using var db = TestDbContextFactory.Create();
        var n1 = new Notification { EntityType = "SALES_ORDER", EntityId = "doc-1", BranchId = "br-1", Type = "CREATED", Category = "ACTIVITY", Message = "x" };
        var n2 = new Notification { EntityType = "SALES_ORDER", EntityId = "doc-2", BranchId = "br-1", Type = "CREATED", Category = "ACTIVITY", Message = "y" };
        db.Notifications.AddRange(n1, n2);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new MarkNotificationsReadCommandHandler(db);

        var result = await handler.HandleAsync(new MarkNotificationsReadCommand([n1.Id, n2.Id], "manager"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.NotificationReads.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Skip_Already_Read_Notifications()
    {
        await using var db = TestDbContextFactory.Create();
        var n1 = new Notification { EntityType = "SALES_ORDER", EntityId = "doc-1", BranchId = "br-1", Type = "CREATED", Category = "ACTIVITY", Message = "x" };
        db.Notifications.Add(n1);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.NotificationReads.Add(new NotificationRead { NotificationId = n1.Id, UserId = "manager", ReadAt = DateTimeOffset.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new MarkNotificationsReadCommandHandler(db);

        var result = await handler.HandleAsync(new MarkNotificationsReadCommand([n1.Id], "manager"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.NotificationReads.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_When_No_Ids_Given()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new MarkNotificationsReadCommandHandler(db);

        var result = await handler.HandleAsync(new MarkNotificationsReadCommand([], "manager"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.NotificationReads.Should().BeEmpty();
    }
}
