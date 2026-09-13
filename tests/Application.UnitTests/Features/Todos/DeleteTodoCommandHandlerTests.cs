namespace ZARI.Application.UnitTests.Features.Todos;

using ZARI.Application.Features.Todos.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using ZARI.Infrastructure.Persistence.Repositories.Todo;

public sealed class DeleteTodoCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteTodoCommandHandler(db, new TodoItemRepository(db));

        var result = await handler.HandleAsync(new DeleteTodoCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Existing_Todo()
    {
        await using var db = TestDbContextFactory.Create();
        var todo = new TodoItem { Title = "To delete" };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteTodoCommandHandler(db, new TodoItemRepository(db));

        var result = await handler.HandleAsync(new DeleteTodoCommand(todo.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.Todos.Should().BeEmpty();
    }
}
