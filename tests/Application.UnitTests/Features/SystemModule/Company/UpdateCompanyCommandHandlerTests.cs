namespace ZARI.Application.UnitTests.Features.SystemModule.Company;

using Microsoft.EntityFrameworkCore;
using ZARI.Application.Features.SystemModule.Companies.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateCompanyCommandHandlerTests
{
    private static UpdateCompanyCommand Command(string code = "SIDC") =>
        new(code, "Updated Coop Name", null, "cur-php", null, null, null, null, false, false, false, false, false);

    [Fact]
    public async Task HandleAsync_Should_Update_Company()
    {
        await using var db = TestDbContextFactory.Create();
        db.Companies.Add(SystemModuleTestFixtures.Company());
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateCompanyCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.Companies.FirstAsync(TestContext.Current.CancellationToken)).Name.Should().Be("Updated Coop Name");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Configured()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateCompanyCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

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
        permissions.HasPermissionAsync("COMPANY", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateCompanyCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
