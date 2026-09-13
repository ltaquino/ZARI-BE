namespace ZARI.Application.UnitTests.Features.Todos;

using ZARI.Application.Features.Todos.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Infrastructure.Persistence.Repositories.Todo;

public sealed class CreateTodoCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Create_Todo()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateTodoCommandHandler(db, new TodoItemRepository(db));

        var result = await handler.HandleAsync(new CreateTodoCommand("Follow up with supplier", "Call before Friday"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Follow up with supplier");
        result.Value.Description.Should().Be("Call before Friday");
        db.Todos.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Todo_With_Null_Description()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateTodoCommandHandler(db, new TodoItemRepository(db));

        var result = await handler.HandleAsync(new CreateTodoCommand("Simple task", null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Description.Should().BeNull();
    }
}
