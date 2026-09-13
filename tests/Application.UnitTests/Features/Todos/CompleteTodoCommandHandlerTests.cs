namespace ZARI.Application.UnitTests.Features.Todos;

using ZARI.Application.Features.Todos.Complete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CompleteTodoCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CompleteTodoCommandHandler(db);

        var result = await handler.HandleAsync(new CompleteTodoCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Mark_Todo_Completed()
    {
        await using var db = TestDbContextFactory.Create();
        var todo = new TodoItem { Title = "Finish this" };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CompleteTodoCommandHandler(db);

        var result = await handler.HandleAsync(new CompleteTodoCommand(todo.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.Todos.FindAsync([todo.Id], TestContext.Current.CancellationToken);
        updated!.IsCompleted.Should().BeTrue();
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_Should_Be_Idempotent_When_Already_Completed()
    {
        await using var db = TestDbContextFactory.Create();
        var todo = new TodoItem { Title = "Already done" };
        todo.MarkAsCompleted();
        var firstCompletedAt = todo.CompletedAt;
        db.Todos.Add(todo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CompleteTodoCommandHandler(db);

        var result = await handler.HandleAsync(new CompleteTodoCommand(todo.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.Todos.FindAsync([todo.Id], TestContext.Current.CancellationToken);
        updated!.CompletedAt.Should().Be(firstCompletedAt);
    }
}
