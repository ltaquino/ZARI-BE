namespace ZARI.Application.UnitTests.Features.Todos;

//using ZARI.Application.Features.Todos.Get;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;
using FluentAssertions;

public sealed class GetTodoQueryHandlerTests
{
    //[Fact]
    //public async Task HandleAsync_Should_Return_Todo_When_Found()
    //{
    //    // Arrange
    //    await using var dbContext = TestDbContextFactory.Create();
    //    var todo = new TodoItem { Title = "Test", Description = "Desc" };
    //    dbContext.Todos.Add(todo);
    //    await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

    //    var handler = new GetTodoQueryHandler(dbContext);

    //    // Act
    //    var result = await handler.HandleAsync(new GetTodoQuery(todo.Id), TestContext.Current.CancellationToken);

    //    // Assert
    //    result.IsSuccess.Should().BeTrue();
    //    result.Value!.Title.Should().Be("Test");
    //}

    //[Fact]
    //public async Task HandleAsync_Should_Return_NotFound_When_Missing()
    //{
    //    // Arrange
    //    await using var dbContext = TestDbContextFactory.Create();
    //    var handler = new GetTodoQueryHandler(dbContext);

    //    // Act
    //    var result = await handler.HandleAsync(new GetTodoQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

    //    // Assert
    //    result.IsFailure.Should().BeTrue();
    //    result.Error!.Type.Should().Be(ErrorType.NotFound);
    //}
}
