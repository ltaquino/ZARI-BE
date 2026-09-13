namespace ZARI.Application.UnitTests.Features.Todos;

using ZARI.Application.Features.Todos.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Entities;

public sealed class GetAllTodosQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Empty_Page_When_No_Todos_Exist()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetAllTodosQueryHandler(db);

        var result = await handler.HandleAsync(new GetAllTodosQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Page_Results_Ordered_By_Created_Descending()
    {
        await using var db = TestDbContextFactory.Create();
        var older = new TodoItem { Title = "Older" };
        db.Todos.Add(older);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var newer = new TodoItem { Title = "Newer" };
        db.Todos.Add(newer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllTodosQueryHandler(db);

        var result = await handler.HandleAsync(new GetAllTodosQuery(Page: 1, PageSize: 1), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].Title.Should().Be("Newer");
    }
}
