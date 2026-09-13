namespace ZARI.Application.UnitTests.Features.Hr.GovernmentContributionRate;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.GovernmentContributionRates.Create;
using ZARI.Application.Features.Hr.GovernmentContributionRates.Delete;
using ZARI.Application.Features.Hr.GovernmentContributionRates.Get;
using ZARI.Application.Features.Hr.GovernmentContributionRates.GetAll;
using ZARI.Application.Features.Hr.GovernmentContributionRates.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateGovernmentContributionRateCommandHandlerTests
{
    private static readonly DateTimeOffset Effective = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static CreateGovernmentContributionRateCommand Command(string contributionType = "PHILHEALTH") => new(contributionType, 0m, 100000m, 2m, 2m, null, Effective, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Rate()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateGovernmentContributionRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContributionType.Should().Be("PHILHEALTH");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("GOVT_CONTRIBUTION_RATES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateGovernmentContributionRateCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate()
    {
        await using var db = TestDbContextFactory.Create();
        db.GovernmentContributionRates.Add(HrTestFixtures.GovernmentContributionRate(contributionType: "PHILHEALTH", salaryFrom: 0m, effectiveDate: Effective));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateGovernmentContributionRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Allow_Same_Bracket_For_Different_ContributionType()
    {
        await using var db = TestDbContextFactory.Create();
        db.GovernmentContributionRates.Add(HrTestFixtures.GovernmentContributionRate(contributionType: "PHILHEALTH", salaryFrom: 0m, effectiveDate: Effective));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateGovernmentContributionRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command("PAGIBIG"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }
}

public sealed class UpdateGovernmentContributionRateCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Rate()
    {
        await using var db = TestDbContextFactory.Create();
        var rate = HrTestFixtures.GovernmentContributionRate();
        db.GovernmentContributionRates.Add(rate);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateGovernmentContributionRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateGovernmentContributionRateCommand(rate.Id, "PHILHEALTH", 0m, 100000m, 2.5m, 2.5m, null, rate.EffectiveDate, "active"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.GovernmentContributionRates.FindAsync([rate.Id], TestContext.Current.CancellationToken);
        updated!.EmployeeSharePct.Should().Be(2.5m);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateGovernmentContributionRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateGovernmentContributionRateCommand(Guid.NewGuid(), "PHILHEALTH", 0m, 100000m, 2m, 2m, null, DateTimeOffset.UtcNow, "active"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeleteGovernmentContributionRateCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Rate()
    {
        await using var db = TestDbContextFactory.Create();
        var rate = HrTestFixtures.GovernmentContributionRate();
        db.GovernmentContributionRates.Add(rate);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteGovernmentContributionRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteGovernmentContributionRateCommand(rate.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteGovernmentContributionRateCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteGovernmentContributionRateCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetGovernmentContributionRateQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Rate()
    {
        await using var db = TestDbContextFactory.Create();
        var rate = HrTestFixtures.GovernmentContributionRate();
        db.GovernmentContributionRates.Add(rate);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetGovernmentContributionRateQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGovernmentContributionRateQuery(rate.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContributionType.Should().Be(rate.ContributionType);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetGovernmentContributionRateQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetGovernmentContributionRateQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllGovernmentContributionRatesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Rates()
    {
        await using var db = TestDbContextFactory.Create();
        db.GovernmentContributionRates.Add(HrTestFixtures.GovernmentContributionRate(contributionType: "PHILHEALTH"));
        db.GovernmentContributionRates.Add(HrTestFixtures.GovernmentContributionRate(contributionType: "PAGIBIG"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllGovernmentContributionRatesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllGovernmentContributionRatesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
