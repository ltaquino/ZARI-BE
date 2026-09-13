namespace ZARI.Application.UnitTests.Features.Hr.PagIbigContributionBracket;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.Create;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.Delete;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.Get;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.GetAll;
using ZARI.Application.Features.Hr.PagIbigContributionBrackets.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreatePagIbigContributionBracketCommandHandlerTests
{
    private static readonly DateTimeOffset Effective = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static CreatePagIbigContributionBracketCommand Command(decimal salaryFrom = 0m) => new(salaryFrom, 10000m, 200m, 400m, Effective, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Bracket()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreatePagIbigContributionBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeShare.Should().Be(200m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("PAGIBIG_BRACKETS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreatePagIbigContributionBracketCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate()
    {
        await using var db = TestDbContextFactory.Create();
        db.PagIbigContributionBrackets.Add(HrTestFixtures.PagIbigContributionBracket(salaryFrom: 0m, effectiveDate: Effective));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreatePagIbigContributionBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdatePagIbigContributionBracketCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Bracket()
    {
        await using var db = TestDbContextFactory.Create();
        var bracket = HrTestFixtures.PagIbigContributionBracket();
        db.PagIbigContributionBrackets.Add(bracket);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdatePagIbigContributionBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdatePagIbigContributionBracketCommand(bracket.Id, 0m, 10000m, 250m, 450m, bracket.EffectiveDate, "active"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.PagIbigContributionBrackets.FindAsync([bracket.Id], TestContext.Current.CancellationToken);
        updated!.EmployeeShare.Should().Be(250m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdatePagIbigContributionBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdatePagIbigContributionBracketCommand(Guid.NewGuid(), 0m, 10000m, 200m, 400m, DateTimeOffset.UtcNow, "active"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeletePagIbigContributionBracketCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Bracket()
    {
        await using var db = TestDbContextFactory.Create();
        var bracket = HrTestFixtures.PagIbigContributionBracket();
        db.PagIbigContributionBrackets.Add(bracket);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeletePagIbigContributionBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePagIbigContributionBracketCommand(bracket.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeletePagIbigContributionBracketCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePagIbigContributionBracketCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetPagIbigContributionBracketQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Bracket()
    {
        await using var db = TestDbContextFactory.Create();
        var bracket = HrTestFixtures.PagIbigContributionBracket();
        db.PagIbigContributionBrackets.Add(bracket);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPagIbigContributionBracketQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPagIbigContributionBracketQuery(bracket.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.EmployeeShare.Should().Be(bracket.EmployeeShare);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetPagIbigContributionBracketQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPagIbigContributionBracketQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllPagIbigContributionBracketsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Brackets()
    {
        await using var db = TestDbContextFactory.Create();
        db.PagIbigContributionBrackets.Add(HrTestFixtures.PagIbigContributionBracket(salaryFrom: 0m));
        db.PagIbigContributionBrackets.Add(HrTestFixtures.PagIbigContributionBracket(salaryFrom: 10000m));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllPagIbigContributionBracketsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllPagIbigContributionBracketsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
