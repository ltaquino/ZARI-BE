namespace ZARI.Application.UnitTests.Features.Sales.PosPromoSlide;

using ZARI.Application.Features.Sales.PosPromoSlides.Create;
using ZARI.Application.Features.Sales.PosPromoSlides.Delete;
using ZARI.Application.Features.Sales.PosPromoSlides.Get;
using ZARI.Application.Features.Sales.PosPromoSlides.GetAll;
using ZARI.Application.Features.Sales.PosPromoSlides.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreatePosPromoSlideCommandHandlerTests
{
    private static CreatePosPromoSlideCommand Command(string title = "Promo") => new(title, "Subtitle", null, 1, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Slide()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreatePosPromoSlideCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Promo");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("POS_PROMO_SLIDES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreatePosPromoSlideCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class UpdatePosPromoSlideCommandHandlerTests
{
    private static UpdatePosPromoSlideCommand Command(Guid id, string title = "Promo Updated") => new(id, title, "Subtitle", null, 2, "inactive");

    [Fact]
    public async Task HandleAsync_Should_Update_Slide()
    {
        await using var db = TestDbContextFactory.Create();
        var slide = SalesTestFixtures.PosPromoSlide();
        db.PosPromoSlides.Add(slide);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdatePosPromoSlideCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(slide.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.PosPromoSlides.FindAsync([slide.Id], TestContext.Current.CancellationToken))!.Status.Should().Be("inactive");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdatePosPromoSlideCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var slide = SalesTestFixtures.PosPromoSlide();
        db.PosPromoSlides.Add(slide);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("POS_PROMO_SLIDES", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdatePosPromoSlideCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(slide.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class DeletePosPromoSlideCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Slide()
    {
        await using var db = TestDbContextFactory.Create();
        var slide = SalesTestFixtures.PosPromoSlide();
        db.PosPromoSlides.Add(slide);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeletePosPromoSlideCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePosPromoSlideCommand(slide.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.PosPromoSlides.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeletePosPromoSlideCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePosPromoSlideCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var slide = SalesTestFixtures.PosPromoSlide();
        db.PosPromoSlides.Add(slide);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("POS_PROMO_SLIDES", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeletePosPromoSlideCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeletePosPromoSlideCommand(slide.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetPosPromoSlideQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Slide_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var slide = SalesTestFixtures.PosPromoSlide();
        db.PosPromoSlides.Add(slide);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPosPromoSlideQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPosPromoSlideQuery(slide.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be(slide.Title);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetPosPromoSlideQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPosPromoSlideQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("POS_PROMO_SLIDES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetPosPromoSlideQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetPosPromoSlideQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllPosPromoSlidesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Slides_Ordered_By_DisplayOrder()
    {
        await using var db = TestDbContextFactory.Create();
        db.PosPromoSlides.AddRange(SalesTestFixtures.PosPromoSlide(title: "Second", displayOrder: 2), SalesTestFixtures.PosPromoSlide(title: "First", displayOrder: 1));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllPosPromoSlidesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllPosPromoSlidesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Title.Should().Be("First");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("POS_PROMO_SLIDES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllPosPromoSlidesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllPosPromoSlidesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
