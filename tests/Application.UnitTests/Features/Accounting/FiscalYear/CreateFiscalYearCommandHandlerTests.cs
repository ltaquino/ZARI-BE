namespace ZARI.Application.UnitTests.Features.Accounting.FiscalYear;

using ZARI.Application.Features.Accounting.FiscalYears.Create;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateFiscalYearCommandHandlerTests
{
    private static CreateFiscalYearCommand Command(string yearName = "FY2026", DateTimeOffset? start = null, DateTimeOffset? end = null) =>
        new(yearName, start ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), end ?? new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero), "OPEN");

    [Fact]
    public async Task HandleAsync_Should_Create_FiscalYear()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateFiscalYearCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.YearName.Should().Be("FY2026");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("FISCAL_YEARS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateFiscalYearCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Name_Already_Exists()
    {
        await using var db = TestDbContextFactory.Create();
        db.FiscalYears.Add(AccountingTestFixtures.FiscalYear(yearName: "FY2026"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateFiscalYearCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_EndDate_Before_StartDate()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateFiscalYearCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(
            Command(start: new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero), end: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("FiscalYear.InvalidDateRange");
    }
}
