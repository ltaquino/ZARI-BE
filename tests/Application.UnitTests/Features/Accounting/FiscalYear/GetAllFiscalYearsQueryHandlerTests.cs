namespace ZARI.Application.UnitTests.Features.Accounting.FiscalYear;

using ZARI.Application.Features.Accounting.FiscalYears.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllFiscalYearsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_FiscalYears()
    {
        await using var db = TestDbContextFactory.Create();
        db.FiscalYears.Add(AccountingTestFixtures.FiscalYear());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllFiscalYearsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllFiscalYearsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("FISCAL_YEARS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllFiscalYearsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllFiscalYearsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
