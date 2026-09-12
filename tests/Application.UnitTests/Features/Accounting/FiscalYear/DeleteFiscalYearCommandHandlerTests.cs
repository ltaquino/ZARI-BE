namespace ZARI.Application.UnitTests.Features.Accounting.FiscalYear;

using ZARI.Application.Features.Accounting.FiscalYears.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeleteFiscalYearCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_FiscalYear()
    {
        await using var db = TestDbContextFactory.Create();
        var fiscalYear = AccountingTestFixtures.FiscalYear();
        db.FiscalYears.Add(fiscalYear);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteFiscalYearCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteFiscalYearCommand(fiscalYear.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.FiscalYears.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteFiscalYearCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteFiscalYearCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

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
        permissions.HasPermissionAsync("FISCAL_YEARS", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteFiscalYearCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteFiscalYearCommand(fiscalYear.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
