namespace ZARI.Application.UnitTests.Features.Accounting.FiscalYear;

using ZARI.Application.Features.Accounting.FiscalYears.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateFiscalYearCommandHandlerTests
{
    private static UpdateFiscalYearCommand Command(Guid id, string yearName = "FY2026", DateTimeOffset? start = null, DateTimeOffset? end = null) =>
        new(id, yearName, start ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), end ?? new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero), "CLOSED");

    [Fact]
    public async Task HandleAsync_Should_Update_FiscalYear()
    {
        await using var db = TestDbContextFactory.Create();
        var fiscalYear = AccountingTestFixtures.FiscalYear();
        db.FiscalYears.Add(fiscalYear);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateFiscalYearCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(fiscalYear.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.FiscalYears.FindAsync([fiscalYear.Id], TestContext.Current.CancellationToken))!.Status.Should().Be("CLOSED");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateFiscalYearCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var fiscalYear = AccountingTestFixtures.FiscalYear();
        db.FiscalYears.Add(fiscalYear);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("FISCAL_YEARS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateFiscalYearCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(fiscalYear.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Name_Conflicts()
    {
        await using var db = TestDbContextFactory.Create();
        var fiscalYear = AccountingTestFixtures.FiscalYear(yearName: "FY2026");
        var other = AccountingTestFixtures.FiscalYear(yearName: "FY2027", startDate: new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));
        db.FiscalYears.AddRange(fiscalYear, other);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateFiscalYearCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(fiscalYear.Id, yearName: "FY2027"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_EndDate_Before_StartDate()
    {
        await using var db = TestDbContextFactory.Create();
        var fiscalYear = AccountingTestFixtures.FiscalYear();
        db.FiscalYears.Add(fiscalYear);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateFiscalYearCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(
            Command(fiscalYear.Id, start: new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero), end: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("FiscalYear.InvalidDateRange");
    }
}
