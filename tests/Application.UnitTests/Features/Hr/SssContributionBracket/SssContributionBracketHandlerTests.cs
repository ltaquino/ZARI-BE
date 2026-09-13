namespace ZARI.Application.UnitTests.Features.Hr.SssContributionBracket;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.SssContributionBrackets.Create;
using ZARI.Application.Features.Hr.SssContributionBrackets.Delete;
using ZARI.Application.Features.Hr.SssContributionBrackets.Get;
using ZARI.Application.Features.Hr.SssContributionBrackets.GetAll;
using ZARI.Application.Features.Hr.SssContributionBrackets.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateSssContributionBracketCommandHandlerTests
{
    private static readonly DateTimeOffset Effective = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static CreateSssContributionBracketCommand Command(decimal salaryFrom = 0m) => new(salaryFrom, 10000m, 200m, 400m, Effective, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Bracket()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateSssContributionBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeShare.Should().Be(200m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("SSS_CONTRIBUTION_BRACKETS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateSssContributionBracketCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate()
    {
        await using var db = TestDbContextFactory.Create();
        db.SssContributionBrackets.Add(HrTestFixtures.SssContributionBracket(salaryFrom: 0m, effectiveDate: Effective));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateSssContributionBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdateSssContributionBracketCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Bracket()
    {
        await using var db = TestDbContextFactory.Create();
        var bracket = HrTestFixtures.SssContributionBracket();
        db.SssContributionBrackets.Add(bracket);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateSssContributionBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateSssContributionBracketCommand(bracket.Id, 0m, 10000m, 250m, 450m, bracket.EffectiveDate, "active"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.SssContributionBrackets.FindAsync([bracket.Id], TestContext.Current.CancellationToken);
        updated!.EmployeeShare.Should().Be(250m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateSssContributionBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateSssContributionBracketCommand(Guid.NewGuid(), 0m, 10000m, 200m, 400m, DateTimeOffset.UtcNow, "active"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeleteSssContributionBracketCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Bracket()
    {
        await using var db = TestDbContextFactory.Create();
        var bracket = HrTestFixtures.SssContributionBracket();
        db.SssContributionBrackets.Add(bracket);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteSssContributionBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteSssContributionBracketCommand(bracket.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteSssContributionBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteSssContributionBracketCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetSssContributionBracketQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Bracket()
    {
        await using var db = TestDbContextFactory.Create();
        var bracket = HrTestFixtures.SssContributionBracket();
        db.SssContributionBrackets.Add(bracket);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetSssContributionBracketQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSssContributionBracketQuery(bracket.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeShare.Should().Be(bracket.EmployeeShare);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetSssContributionBracketQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetSssContributionBracketQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllSssContributionBracketsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Brackets()
    {
        await using var db = TestDbContextFactory.Create();
        db.SssContributionBrackets.Add(HrTestFixtures.SssContributionBracket(salaryFrom: 0m));
        db.SssContributionBrackets.Add(HrTestFixtures.SssContributionBracket(salaryFrom: 10000m));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllSssContributionBracketsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllSssContributionBracketsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
