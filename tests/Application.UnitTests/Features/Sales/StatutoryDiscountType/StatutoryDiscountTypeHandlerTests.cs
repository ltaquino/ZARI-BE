namespace ZARI.Application.UnitTests.Features.Sales.StatutoryDiscountType;

using ZARI.Application.Features.Sales.StatutoryDiscountTypes.Create;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.Delete;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.Get;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.GetAll;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateStatutoryDiscountTypeCommandHandlerTests
{
    private static CreateStatutoryDiscountTypeCommand Command(string code = "SENIOR") => new(code, "Senior Citizen", 20, true, "Senior Citizen ID", "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Type()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateStatutoryDiscountTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("SENIOR");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STATUTORY_DISCOUNT_TYPES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateStatutoryDiscountTypeCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.StatutoryDiscountTypes.Add(SalesTestFixtures.StatutoryDiscountType(code: "SENIOR"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateStatutoryDiscountTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdateStatutoryDiscountTypeCommandHandlerTests
{
    private static UpdateStatutoryDiscountTypeCommand Command(Guid id, string code = "SENIOR") => new(id, code, "Senior Citizen Updated", 25, true, "Senior Citizen ID", "inactive");

    [Fact]
    public async Task HandleAsync_Should_Update_Type()
    {
        await using var db = TestDbContextFactory.Create();
        var type = SalesTestFixtures.StatutoryDiscountType();
        db.StatutoryDiscountTypes.Add(type);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateStatutoryDiscountTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(type.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.StatutoryDiscountTypes.FindAsync([type.Id], TestContext.Current.CancellationToken))!.Status.Should().Be("inactive");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateStatutoryDiscountTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var type = SalesTestFixtures.StatutoryDiscountType();
        db.StatutoryDiscountTypes.Add(type);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STATUTORY_DISCOUNT_TYPES", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateStatutoryDiscountTypeCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(type.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts()
    {
        await using var db = TestDbContextFactory.Create();
        var type = SalesTestFixtures.StatutoryDiscountType(code: "SENIOR");
        var other = SalesTestFixtures.StatutoryDiscountType(code: "PWD");
        db.StatutoryDiscountTypes.AddRange(type, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateStatutoryDiscountTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(type.Id, code: "PWD"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class DeleteStatutoryDiscountTypeCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Type()
    {
        await using var db = TestDbContextFactory.Create();
        var type = SalesTestFixtures.StatutoryDiscountType();
        db.StatutoryDiscountTypes.Add(type);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteStatutoryDiscountTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteStatutoryDiscountTypeCommand(type.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.StatutoryDiscountTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteStatutoryDiscountTypeCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteStatutoryDiscountTypeCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var type = SalesTestFixtures.StatutoryDiscountType();
        db.StatutoryDiscountTypes.Add(type);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STATUTORY_DISCOUNT_TYPES", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteStatutoryDiscountTypeCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteStatutoryDiscountTypeCommand(type.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetStatutoryDiscountTypeQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Type_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var type = SalesTestFixtures.StatutoryDiscountType();
        db.StatutoryDiscountTypes.Add(type);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetStatutoryDiscountTypeQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetStatutoryDiscountTypeQuery(type.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be(type.Code);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetStatutoryDiscountTypeQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetStatutoryDiscountTypeQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STATUTORY_DISCOUNT_TYPES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetStatutoryDiscountTypeQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetStatutoryDiscountTypeQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllStatutoryDiscountTypesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Types()
    {
        await using var db = TestDbContextFactory.Create();
        db.StatutoryDiscountTypes.AddRange(SalesTestFixtures.StatutoryDiscountType(code: "SENIOR"), SalesTestFixtures.StatutoryDiscountType(code: "PWD"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllStatutoryDiscountTypesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllStatutoryDiscountTypesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("STATUTORY_DISCOUNT_TYPES", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllStatutoryDiscountTypesQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllStatutoryDiscountTypesQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
