namespace ZARI.Application.UnitTests.Features.SystemModule.Company;

using ZARI.Application.Features.SystemModule.Companies.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetCompanyQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Company_When_Configured()
    {
        await using var db = TestDbContextFactory.Create();
        db.Companies.Add(SystemModuleTestFixtures.Company());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetCompanyQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCompanyQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("SIDC");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Configured()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetCompanyQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetCompanyQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        db.Companies.Add(SystemModuleTestFixtures.Company());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("COMPANY", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetCompanyQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetCompanyQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
