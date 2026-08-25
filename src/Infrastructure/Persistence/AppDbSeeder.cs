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
        await SeedBranchesAsync(context, logger);
        await SeedDemoUsersAsync(userManager, passwordHasher, context, logger);
        await SeedUomsAsync(context, logger);
        await SeedItemCategoriesAsync(context, logger);
        await SeedWarehousesAsync(context, logger);
        await SeedAdjustmentReasonsAsync(context, logger);
        await SeedDocumentSequencesAsync(context, logger);
        await SeedGlAccountsAsync(context, logger);
        await SeedCostCentersAsync(context, logger);
        await SeedCompanyAsync(context, logger);
        await SeedFormsAsync(context, logger);
        await SeedRolePermissionsAsync(context, roleManager, logger);
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
    /// Mirrors the FE's mock system-module seed (ZARI-FE/src/data/system-module/users.ts) — same
    /// emails, names, and branch assignments — so the interbranch demo workflow advertised on the
    /// login page works against real backend auth, roles, and branch scoping.
    /// </summary>
    private static async Task SeedDemoUsersAsync(
        UserManager<ApplicationUser> userManager,
        IPasswordHasher<ApplicationUser> passwordHasher,
        AppDbContext context,
        ILogger logger)
    {
        (string Email, string FirstName, string LastName, string Phone, string Role, string[] BranchIds)[] demoUsers =
        [
            ("admin@zari.coop", "Maria", "Santos", "+63 917 111 2222", "Admin", ["br-hq", "br-north", "br-south", "br-east"]),
            ("manager@zari.coop", "Carlo", "Reyes", "+63 918 222 3333", "Manager", ["br-north"]),
            ("ana.lopez@zari.coop", "Ana", "Lopez", "+63 919 333 4444", "Staff", ["br-south"]),
            ("rico.tan@zari.coop", "Rico", "Tan", "+63 920 444 5555", "Staff", ["br-east"]),
            ("staff.north@zari.coop", "Jenny", "Cruz", "+63 921 555 6666", "Staff", ["br-north"]),
            ("manager.hq@zari.coop", "Bea", "Santos", "+63 922 666 7777", "Manager", ["br-hq"]),
            ("staff.hq@zari.coop", "Miguel", "Torres", "+63 923 777 8888", "Staff", ["br-hq"]),
        ];

        // "zari123" fails the real signup password policy (no uppercase) — seeded directly via the
        // password hasher and the no-password CreateAsync overload (which skips policy validation)
        // so the demo credential advertised on the login page can stay as-is without weakening the
        // policy that applies to genuine self-service registrations.
        const string demoPassword = "zari123";

        foreach (var (email, firstName, lastName, phone, role, branchIds) in demoUsers)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                continue;

            var user = new ApplicationUser
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                UserName = email,
                Phone = phone,
                Status = "active",
                EmailConfirmed = true
            };
            user.PasswordHash = passwordHasher.HashPassword(user, demoPassword);

            var result = await userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                context.UserBranches.AddRange(branchIds.Select(branchId => new UserBranch { UserId = user.Id, BranchId = branchId }));
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded demo user: {Email}", email);
            }
        }
    }

    /// <summary>
    /// Mirrors the FE's mock seed (ZARI-FE/src/data/system-module/branches.ts) exactly — same ids,
    /// so every already-seeded/created row elsewhere that stores one of these as a plain BranchId
    /// string (Warehouse, DocumentSequence, ...) resolves against a real row here. Runs before
    /// SeedWarehousesAsync/SeedDocumentSequencesAsync since those now carry a real FK to this table.
    /// </summary>
    private static async Task SeedBranchesAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Branches.AnyAsync())
            return;

        context.Branches.AddRange(
            new Branch { Id = "br-hq", Name = "Head Office", Code = "HQ", City = "Cebu City", Address = "Osmena Blvd, Cebu City", Phone = "+63 32 111 2222", Status = "active", IsHeadOffice = true },
            new Branch { Id = "br-north", Name = "North Branch", Code = "NB", City = "Mandaue City", Address = "A.S. Fortuna St, Mandaue City", Phone = "+63 32 222 3333", Status = "active", IsHeadOffice = false },
            new Branch { Id = "br-south", Name = "South Branch", Code = "SB", City = "Talisay City", Address = "Tabunok, Talisay City", Phone = "+63 32 333 4444", Status = "active", IsHeadOffice = false },
            new Branch { Id = "br-east", Name = "East Branch", Code = "EB", City = "Lapu-Lapu City", Address = "Pusok, Lapu-Lapu City", Phone = "+63 32 444 5555", Status = "active", IsHeadOffice = false });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default branches");
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

    /// <summary>
    /// Mirrors the FE's mock seed (ZARI-FE/src/data/accounting/glAccounts.ts) so the default chart
    /// of accounts other still-FE-mock modules fall back to (Item.inventoryAccountId,
    /// AdjustmentReason.GlAccountId, etc.) stays meaningful once GlAccount has a real backend —
    /// those fields are loose strings, not FKs, so they don't need updating for this to work.
    /// </summary>
    private static async Task SeedGlAccountsAsync(AppDbContext context, ILogger logger)
    {
        if (await context.GlAccounts.AnyAsync())
            return;

        context.GlAccounts.AddRange(
            new GlAccount { Code = "1000", Name = "Cash on Hand", AccountType = "Asset", NormalBalance = "Debit", Status = "active" },
            new GlAccount { Code = "1200", Name = "Accounts Receivable", AccountType = "Asset", NormalBalance = "Debit", Status = "active" },
            new GlAccount { Code = "1400", Name = "Inventory Asset", AccountType = "Asset", NormalBalance = "Debit", Status = "active" },
            new GlAccount { Code = "1450", Name = "Inventory In-Transit", AccountType = "Asset", NormalBalance = "Debit", Status = "active" },
            new GlAccount { Code = "2000", Name = "Accounts Payable", AccountType = "Liability", NormalBalance = "Credit", Status = "active" },
            new GlAccount { Code = "4000", Name = "Sales Revenue", AccountType = "Revenue", NormalBalance = "Credit", Status = "active" },
            new GlAccount { Code = "5000", Name = "Cost of Goods Sold", AccountType = "Cogs", NormalBalance = "Debit", Status = "active" },
            new GlAccount { Code = "5100", Name = "Inventory Variance / Shrinkage", AccountType = "Cogs", NormalBalance = "Debit", Status = "active" });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default GL accounts");
    }

    private static async Task SeedCostCentersAsync(AppDbContext context, ILogger logger)
    {
        if (await context.CostCenters.AnyAsync())
            return;

        context.CostCenters.AddRange(
            new CostCenter { Code = "ADMIN", Name = "Administration", Status = "active" },
            new CostCenter { Code = "DELIVERY", Name = "Delivery Fleet", Status = "active" });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default cost centers");
    }

    /// <summary>
    /// Mirrors the FE's mock seed (ZARI-FE/src/data/system-module/company.ts) — a single row,
    /// never created or deleted through the API, only ever updated.
    /// </summary>
    private static async Task SeedCompanyAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Companies.AnyAsync())
            return;

        context.Companies.Add(new Company
        {
            Code = "ZARI",
            Name = "Zari Distribution Corp.",
            TaxId = "000-000-000-000",
            BaseCurrencyId = "cur-php"
        });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default company record");
    }

    /// <summary>
    /// One row per admin/transactional page the app actually has today — the catalog Role
    /// templates and per-user overrides grant Form-level action flags against.
    /// </summary>
    private static async Task SeedFormsAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Forms.AnyAsync())
            return;

        (string Code, string Name, string Module)[] forms =
        [
            ("DASHBOARD", "Dashboard", "Dashboard"),

            ("CUSTOMERS", "Customers", "CRM"),

            ("USERS", "Users", "System"),
            ("ROLES", "Roles", "System"),
            ("BRANCHES", "Branches", "System"),
            ("COMPANY", "Company Settings", "System"),
            ("DOCUMENT_SEQUENCES", "Document Sequences", "System"),

            ("GL_ACCOUNTS", "GL Accounts", "Accounting"),
            ("COST_CENTERS", "Cost Centers", "Accounting"),
            ("GL_JOURNALS", "Journal Entries", "Accounting"),

            ("UOMS", "Units of Measure", "Inventory"),
            ("ITEM_CATEGORIES", "Item Categories", "Inventory"),
            ("WAREHOUSES", "Warehouses", "Inventory"),
            ("STORAGE_LOCATIONS", "Storage Locations", "Inventory"),
            ("ITEMS", "Items", "Inventory"),
            ("ADJUSTMENT_REASONS", "Adjustment Reasons", "Inventory"),
            ("ITEM_BRANCH_SETTINGS", "Item Branch Settings", "Inventory"),
            ("STOCK_RESERVATIONS", "Stock Reservations", "Inventory"),
            ("SERIAL_NUMBERS", "Serial Numbers", "Inventory"),

            ("GOODS_RECEIPTS", "Goods Receipts", "Inventory Transactions"),
            ("GOODS_ISSUES", "Goods Issues", "Inventory Transactions"),
            ("STOCK_ADJUSTMENTS", "Stock Adjustments", "Inventory Transactions"),
            ("STOCK_OPNAMES", "Stock Opnames", "Inventory Transactions"),
            ("STOCK_TRANSFER_REQUESTS", "Stock Transfer Requests", "Inventory Transactions"),
            ("STOCK_LOCATION_TRANSFERS", "Stock Location Transfers", "Inventory Transactions"),

            ("APPROVAL_REQUESTS", "Approval Requests", "Workflow"),
            ("NOTIFICATIONS", "Notifications", "Workflow"),
        ];

        context.Forms.AddRange(forms.Select(f => new Form { Code = f.Code, Name = f.Name, Module = f.Module }));

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} forms", forms.Length);
    }

    /// <summary>
    /// Default Form grants for the three seeded roles — a starting point editable later through the
    /// Roles admin screen, not a fixed policy. Admin gets every flag on every form. Manager gets
    /// full operational rights (view/create/edit/approve/cancel, no delete) on the transactional
    /// inventory documents plus manage rights on master data, but only view (or nothing) on
    /// system-admin forms. Staff can prepare (view/create/edit) transactional documents but not
    /// approve/cancel/delete them, and is view-only everywhere else it has access at all.
    /// </summary>
    private static async Task SeedRolePermissionsAsync(AppDbContext context, RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        if (await context.RolePermissions.AnyAsync())
            return;

        var adminRole = await roleManager.FindByNameAsync("Admin");
        var managerRole = await roleManager.FindByNameAsync("Manager");
        var staffRole = await roleManager.FindByNameAsync("Staff");
        if (adminRole is null || managerRole is null || staffRole is null)
            return;

        var allFormCodes = await context.Forms.Select(f => f.Code).ToListAsync();

        (bool View, bool Create, bool Edit, bool Approve, bool Cancel, bool Delete) full = (true, true, true, true, true, true);
        (bool View, bool Create, bool Edit, bool Approve, bool Cancel, bool Delete) manage = (true, true, true, false, false, false);
        (bool View, bool Create, bool Edit, bool Approve, bool Cancel, bool Delete) operate = (true, true, true, true, true, false);
        (bool View, bool Create, bool Edit, bool Approve, bool Cancel, bool Delete) prepare = (true, true, true, false, false, false);
        (bool View, bool Create, bool Edit, bool Approve, bool Cancel, bool Delete) view = (true, false, false, false, false, false);

        var permissions = new List<RolePermission>();

        void Grant(string roleId, string formCode, (bool View, bool Create, bool Edit, bool Approve, bool Cancel, bool Delete) flags)
        {
            if (!allFormCodes.Contains(formCode))
                return;

            permissions.Add(new RolePermission
            {
                RoleId = roleId,
                FormCode = formCode,
                CanView = flags.View,
                CanCreate = flags.Create,
                CanEdit = flags.Edit,
                CanApprove = flags.Approve,
                CanCancel = flags.Cancel,
                CanDelete = flags.Delete
            });
        }

        foreach (var formCode in allFormCodes)
            Grant(adminRole.Id, formCode, full);

        string[] managerTransactionalForms =
        [
            "GOODS_RECEIPTS", "GOODS_ISSUES", "STOCK_ADJUSTMENTS", "STOCK_OPNAMES",
            "STOCK_TRANSFER_REQUESTS", "STOCK_LOCATION_TRANSFERS", "APPROVAL_REQUESTS"
        ];
        string[] managerMasterDataForms =
        [
            "UOMS", "ITEM_CATEGORIES", "WAREHOUSES", "STORAGE_LOCATIONS", "ITEMS",
            "ADJUSTMENT_REASONS", "ITEM_BRANCH_SETTINGS", "STOCK_RESERVATIONS", "SERIAL_NUMBERS"
        ];
        string[] managerViewOnlyForms =
        [
            "DASHBOARD", "USERS", "BRANCHES", "DOCUMENT_SEQUENCES", "GL_ACCOUNTS", "COST_CENTERS", "GL_JOURNALS", "NOTIFICATIONS"
        ];

        foreach (var formCode in managerTransactionalForms)
            Grant(managerRole.Id, formCode, operate);
        foreach (var formCode in managerMasterDataForms)
            Grant(managerRole.Id, formCode, manage);
        foreach (var formCode in managerViewOnlyForms)
            Grant(managerRole.Id, formCode, view);
        Grant(managerRole.Id, "CUSTOMERS", manage);

        string[] staffTransactionalForms =
        [
            "GOODS_RECEIPTS", "GOODS_ISSUES", "STOCK_ADJUSTMENTS", "STOCK_OPNAMES",
            "STOCK_TRANSFER_REQUESTS", "STOCK_LOCATION_TRANSFERS"
        ];
        string[] staffViewOnlyForms =
        [
            "DASHBOARD", "CUSTOMERS", "UOMS", "ITEM_CATEGORIES", "WAREHOUSES", "STORAGE_LOCATIONS",
            "ITEMS", "ADJUSTMENT_REASONS", "ITEM_BRANCH_SETTINGS", "STOCK_RESERVATIONS",
            "SERIAL_NUMBERS", "APPROVAL_REQUESTS", "NOTIFICATIONS"
        ];

        foreach (var formCode in staffTransactionalForms)
            Grant(staffRole.Id, formCode, prepare);
        foreach (var formCode in staffViewOnlyForms)
            Grant(staffRole.Id, formCode, view);

        context.RolePermissions.AddRange(permissions);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} default role permissions", permissions.Count);
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
