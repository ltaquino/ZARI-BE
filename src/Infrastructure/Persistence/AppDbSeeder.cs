namespace ZARI.Infrastructure.Persistence;

using ZARI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public static class AppDbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager, logger);
        await SeedDemoUsersAsync(userManager, passwordHasher, logger);
        await SeedUomsAsync(context, logger);
        await SeedItemCategoriesAsync(context, logger);
        await SeedWarehousesAsync(context, logger);
        await SeedAdjustmentReasonsAsync(context, logger);
        await SeedDocumentSequencesAsync(context, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        string[] roles = ["Admin", "Manager", "Staff"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role: {Role}", role);
            }
        }
    }

    /// <summary>
    /// Mirrors the FE's mock system-module seed (ZARI-FE/src/data/system-module/users.ts) so the
    /// interbranch demo workflow advertised on the login page works against real backend auth.
    /// Branch/role-permission assignment still lives in that FE mock until Users/Roles/Branches
    /// get their own backend — this only makes the credential check itself real.
    /// </summary>
    private static async Task SeedDemoUsersAsync(
        UserManager<ApplicationUser> userManager,
        IPasswordHasher<ApplicationUser> passwordHasher,
        ILogger logger)
    {
        (string Email, string FirstName, string LastName, string Role)[] demoUsers =
        [
            ("admin@zari.coop", "Maria", "Santos", "Admin"),
            ("manager@zari.coop", "Carlo", "Reyes", "Manager"),
            ("ana.lopez@zari.coop", "Ana", "Lopez", "Staff"),
            ("rico.tan@zari.coop", "Rico", "Tan", "Staff"),
            ("staff.north@zari.coop", "Jenny", "Cruz", "Staff"),
            ("manager.hq@zari.coop", "Bea", "Santos", "Manager"),
            ("staff.hq@zari.coop", "Miguel", "Torres", "Staff"),
        ];

        // "zari123" fails the real signup password policy (no uppercase) — seeded directly via the
        // password hasher and the no-password CreateAsync overload (which skips policy validation)
        // so the demo credential advertised on the login page can stay as-is without weakening the
        // policy that applies to genuine self-service registrations.
        const string demoPassword = "zari123";

        foreach (var (email, firstName, lastName, role) in demoUsers)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                continue;

            var user = new ApplicationUser
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                UserName = email,
                EmailConfirmed = true
            };
            user.PasswordHash = passwordHasher.HashPassword(user, demoPassword);

            var result = await userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                logger.LogInformation("Seeded demo user: {Email}", email);
            }
        }
    }

    private static async Task SeedUomsAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Uoms.AnyAsync())
            return;

        context.Uoms.AddRange(
            new Uom { Code = "PCS", Name = "Piece" },
            new Uom { Code = "BOX", Name = "Box" },
            new Uom { Code = "KG", Name = "Kilogram" });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default UOMs");
    }

    private static async Task SeedItemCategoriesAsync(AppDbContext context, ILogger logger)
    {
        if (await context.ItemCategories.AnyAsync())
            return;

        context.ItemCategories.AddRange(
            new ItemCategory { Code = "GEN", Name = "General Merchandise" },
            new ItemCategory { Code = "HW", Name = "Hardware" });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default item categories");
    }

    private static async Task SeedWarehousesAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Warehouses.AnyAsync())
            return;

        // BranchId values match the FE's mock branch slugs (br-hq, br-north, ...) — see the
        // note on Warehouse.BranchId; there's no backend Branches module to reference yet.
        context.Warehouses.AddRange(
            new Warehouse { BranchId = "br-hq", Code = "HQ-MAIN", Name = "Head Office Main Warehouse", WarehouseType = "Main", Status = "active" },
            new Warehouse { BranchId = "br-north", Code = "NB-MAIN", Name = "North Branch Main Warehouse", WarehouseType = "Main", Status = "active" });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default warehouses");
    }

    private static async Task SeedAdjustmentReasonsAsync(AppDbContext context, ILogger logger)
    {
        if (await context.AdjustmentReasons.AnyAsync())
            return;

        context.AdjustmentReasons.AddRange(
            new AdjustmentReason { Code = "DAMAGE", Description = "Damaged or spoiled goods", Status = "active" },
            new AdjustmentReason { Code = "THEFT", Description = "Theft or pilferage", Status = "active" },
            new AdjustmentReason { Code = "COUNT_CORRECTION", Description = "Physical count correction", Status = "active" },
            new AdjustmentReason { Code = "EXPIRED", Description = "Expired stock write-off", Status = "active" },
            new AdjustmentReason { Code = "FOUND", Description = "Found stock not previously recorded", Status = "active" });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default adjustment reasons");
    }

    /// <summary>
    /// Mirrors the FE's mock seed (ZARI-FE/src/data/system-module/documentSequences.ts) so document
    /// numbering keeps working identically once the FE switches from its local read-then-write
    /// implementation to this backend's atomic compare-and-swap endpoint.
    /// </summary>
    private static async Task SeedDocumentSequencesAsync(AppDbContext context, ILogger logger)
    {
        if (await context.DocumentSequences.AnyAsync())
            return;

        (string BranchId, string DocType, string Prefix, int NextNumber)[] sequences =
        [
            ("br-hq", "PO", "HQ-PO-", 1),
            ("br-hq", "SO", "HQ-SO-", 1),
            ("br-hq", "GR", "HQ-GR-", 1),
            ("br-north", "GR", "NB-GR-", 1),
            // HQ's Goods Issue counter starts at 2 — the FE mock ships with HQ-GI-000001 pre-seeded.
            ("br-hq", "GI", "HQ-GI-", 2),
            ("br-north", "GI", "NB-GI-", 1),
            ("br-hq", "STR", "HQ-STR-", 1),
            ("br-north", "STR", "NB-STR-", 1),
            ("br-hq", "ADJ", "HQ-ADJ-", 1),
            ("br-north", "ADJ", "NB-ADJ-", 1),
            ("br-hq", "OPN", "HQ-OPN-", 1),
            ("br-north", "OPN", "NB-OPN-", 1),
            ("br-hq", "JV", "HQ-JV-", 1),
            ("br-north", "JV", "NB-JV-", 1),
            ("br-hq", "SLT", "HQ-SLT-", 1),
            ("br-north", "SLT", "NB-SLT-", 1),
        ];

        context.DocumentSequences.AddRange(sequences.Select(s => new DocumentSequence
        {
            BranchId = s.BranchId,
            DocType = s.DocType,
            Prefix = s.Prefix,
            NextNumber = s.NextNumber,
            PaddingLength = 6
        }));

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default document sequences");
    }

    //private static async Task SeedSampleTodosAsync(AppDbContext context, ILogger logger)
    //{
    //    if (await context.Todos.AnyAsync())
    //        return;

    //    var todos = new List<TodoItem>
    //    {
    //        new() { Title = "Explore the Clean Architecture template", Description = "Read through the layers: Domain → Application → Infrastructure → Api" },
    //        new() { Title = "Run the API with Aspire", Description = "Use 'dotnet run' in the AppHost project to start PostgreSQL, Redis, and the API" },
    //        new() { Title = "Try the Scalar API docs", Description = "Navigate to /scalar/v1 to explore and test the endpoints" },
    //        new() { Title = "Add your first feature", Description = "Create a new entity, command/query handlers, and endpoint following the Todos pattern" },
    //        new() { Title = "Check the architecture tests", Description = "Run 'dotnet test' to verify dependency rules are enforced" }
    //    };

    //    context.Todos.AddRange(todos);
    //    await context.SaveChangesAsync();
    //    logger.LogInformation("Seeded {Count} sample todos", todos.Count);
    //}
}
