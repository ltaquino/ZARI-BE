namespace ZARI.Application.UnitTests.Features.Identity;

using ZARI.Application.Features.Identity.Users.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

/// <summary>
/// The success path's own query — a Join(UserRoles, Roles) piped into a GroupBy that's never
/// composed into an aggregate/projection before ToDictionaryAsync — is a shape the EF Core
/// InMemory provider's query translator rejects outright ("A 'GroupBy' operation which is not
/// composed into aggregate or projection of elements is not supported"), confirmed directly:
/// it throws regardless of how many rows exist, not just at scale. Only the guard clause is
/// InMemory-testable here.
/// </summary>
public sealed class GetAllUsersQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("USERS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllUsersQueryHandler(IdentityTestFixtures.UserManager(db), IdentityTestFixtures.RoleManager(db), db, permissions);

        var result = await handler.HandleAsync(new GetAllUsersQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
