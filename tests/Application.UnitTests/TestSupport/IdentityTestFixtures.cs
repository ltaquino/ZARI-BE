namespace ZARI.Application.UnitTests.TestSupport;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ZARI.Domain.Entities;
using ZARI.Infrastructure.Persistence;

/// <summary>
/// AppDbContext extends IdentityDbContext&lt;ApplicationUser&gt;, so real ASP.NET Identity stores
/// (UserStore/RoleStore) can be built directly on top of the same InMemory-provider AppDbContext
/// every other handler test already uses — giving REAL UserManager/RoleManager behavior (password
/// hashing, FindByEmailAsync normalization, AddToRolesAsync, etc.) instead of hand-mocking a
/// concrete framework class. No IUserValidator/IPasswordValidator are wired in, so CreateAsync
/// never rejects a weak test password — that validation is ASP.NET Identity's own framework
/// concern, not application logic this sweep tests.
/// </summary>
internal static class IdentityTestFixtures
{
    public static UserManager<ApplicationUser> UserManager(AppDbContext db) => new(
        new UserStore<ApplicationUser>(db),
        null!,
        new PasswordHasher<ApplicationUser>(),
        [],
        [],
        new UpperInvariantLookupNormalizer(),
        new IdentityErrorDescriber(),
        null!,
        NullLogger<UserManager<ApplicationUser>>.Instance);

    public static RoleManager<IdentityRole> RoleManager(AppDbContext db) => new(
        new RoleStore<IdentityRole>(db),
        [],
        new UpperInvariantLookupNormalizer(),
        new IdentityErrorDescriber(),
        NullLogger<RoleManager<IdentityRole>>.Instance);
}
