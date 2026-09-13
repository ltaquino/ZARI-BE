namespace ZARI.Application.UnitTests.Features.Accounting.FiscalYear;

using ZARI.Application.Features.Accounting.FiscalYears.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetFiscalYearQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_FiscalYear_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var fiscalYear = AccountingTestFixtures.FiscalYear();
        db.FiscalYears.Add(fiscalYear);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetFiscalYearQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetFiscalYearQuery(fiscalYear.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.YearName.Should().Be(fiscalYear.YearName);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetFiscalYearQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetFiscalYearQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("FISCAL_YEARS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetFiscalYearQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetFiscalYearQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
