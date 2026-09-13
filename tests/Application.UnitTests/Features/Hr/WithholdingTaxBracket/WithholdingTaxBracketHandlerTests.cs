namespace ZARI.Application.UnitTests.Features.Hr.WithholdingTaxBracket;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.Create;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.Delete;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.Get;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.GetAll;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateWithholdingTaxBracketCommandHandlerTests
{
    private static readonly DateTimeOffset Effective = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static CreateWithholdingTaxBracketCommand Command(decimal salaryFrom = 0m) => new(salaryFrom, 20000m, 0m, 0m, Effective, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Bracket()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateWithholdingTaxBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("WITHHOLDING_TAX_BRACKETS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateWithholdingTaxBracketCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate()
    {
        await using var db = TestDbContextFactory.Create();
        db.WithholdingTaxBrackets.Add(HrTestFixtures.WithholdingTaxBracket(salaryFrom: 0m, effectiveDate: Effective));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateWithholdingTaxBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdateWithholdingTaxBracketCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Bracket()
    {
        await using var db = TestDbContextFactory.Create();
        var bracket = HrTestFixtures.WithholdingTaxBracket();
        db.WithholdingTaxBrackets.Add(bracket);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateWithholdingTaxBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateWithholdingTaxBracketCommand(bracket.Id, 0m, 20000m, 100m, 0.15m, bracket.EffectiveDate, "active"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.WithholdingTaxBrackets.FindAsync([bracket.Id], TestContext.Current.CancellationToken);
        updated!.BaseTax.Should().Be(100m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateWithholdingTaxBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateWithholdingTaxBracketCommand(Guid.NewGuid(), 0m, 20000m, 0m, 0m, DateTimeOffset.UtcNow, "active"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeleteWithholdingTaxBracketCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Bracket()
    {
        await using var db = TestDbContextFactory.Create();
        var bracket = HrTestFixtures.WithholdingTaxBracket();
        db.WithholdingTaxBrackets.Add(bracket);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteWithholdingTaxBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteWithholdingTaxBracketCommand(bracket.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteWithholdingTaxBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteWithholdingTaxBracketCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetWithholdingTaxBracketQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Bracket()
    {
        await using var db = TestDbContextFactory.Create();
        var bracket = HrTestFixtures.WithholdingTaxBracket();
        db.WithholdingTaxBrackets.Add(bracket);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetWithholdingTaxBracketQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetWithholdingTaxBracketQuery(bracket.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BaseTax.Should().Be(bracket.BaseTax);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetWithholdingTaxBracketQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetWithholdingTaxBracketQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllWithholdingTaxBracketsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Brackets()
    {
        await using var db = TestDbContextFactory.Create();
        db.WithholdingTaxBrackets.Add(HrTestFixtures.WithholdingTaxBracket(salaryFrom: 0m));
        db.WithholdingTaxBrackets.Add(HrTestFixtures.WithholdingTaxBracket(salaryFrom: 20000m));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllWithholdingTaxBracketsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllWithholdingTaxBracketsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
