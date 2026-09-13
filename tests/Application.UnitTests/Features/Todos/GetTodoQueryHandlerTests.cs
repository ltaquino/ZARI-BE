namespace ZARI.Application.UnitTests.Features.Todos;

using ZARI.Application.Features.Todos.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using ZARI.Infrastructure.Persistence.Repositories.Todo;

public sealed class GetTodoQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetTodoQueryHandler(new TodoItemRepository(db));

        var result = await handler.HandleAsync(new GetTodoQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Todo()
    {
        await using var db = TestDbContextFactory.Create();
        var todo = new TodoItem { Title = "Read this", Description = "Details" };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetTodoQueryHandler(new TodoItemRepository(db));

        var result = await handler.HandleAsync(new GetTodoQuery(todo.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Read this");
        result.Value.IsCompleted.Should().BeFalse();
    }
}
