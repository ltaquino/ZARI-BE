namespace ZARI.Application.UnitTests.Features.Todos;

using ZARI.Application.Features.Todos.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using ZARI.Infrastructure.Persistence.Repositories.Todo;

public sealed class UpdateTodoCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateTodoCommandHandler(db, new TodoItemRepository(db));

        var result = await handler.HandleAsync(new UpdateTodoCommand(Guid.NewGuid(), "New title", null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Existing_Todo()
    {
        await using var db = TestDbContextFactory.Create();
        var todo = new TodoItem { Title = "Old title", Description = "Old description" };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateTodoCommandHandler(db, new TodoItemRepository(db));

        var result = await handler.HandleAsync(new UpdateTodoCommand(todo.Id, "New title", "New description"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.Todos.FindAsync([todo.Id], TestContext.Current.CancellationToken);
        updated!.Title.Should().Be("New title");
        updated.Description.Should().Be("New description");
    }
}
