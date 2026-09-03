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
        await SeedCurrenciesAsync(context, logger);
        await SeedCompanyAsync(context, logger);
        await SeedFiscalYearsAsync(context, logger);
        await SeedExchangeRatesAsync(context, logger);
        await SeedBankAccountsAsync(context, logger);
        await SeedSuppliersAsync(context, logger);
        await SeedPurchaseOrdersAsync(context, logger);
        await SeedStatutoryDiscountTypesAsync(context, logger);
        await SeedPaymentMethodsAsync(context, logger);
        await SeedWalkInCustomersAsync(context, logger);
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
        // Diff-based like SeedGlAccountsAsync — the original version of this method only ever ran
        // once (on a fully empty table), so doc types added after go-live (e.g. Sales' "SO"/"DO"/
        // "SINV"/etc. and the BIR-OR series) never got their seed row on an already-seeded DB, and
        // silently fell back to GetNextDocumentNumberCommandHandler's timestamp-based numbering
        // instead. Diffing by (BranchId, DocType) lets new doc types keep getting seeded going
        // forward without re-touching branches/doc types that already have a row.
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
            ("br-hq", "PR", "HQ-PR-", 1),
            ("br-north", "PR", "NB-PR-", 1),
            ("br-hq", "GRPO", "HQ-GRPO-", 1),
            ("br-north", "GRPO", "NB-GRPO-", 1),
            ("br-hq", "GRTN", "HQ-GRTN-", 1),
            ("br-north", "GRTN", "NB-GRTN-", 1),
            ("br-hq", "APINV", "HQ-APINV-", 1),
            ("br-north", "APINV", "NB-APINV-", 1),
            ("br-hq", "OP", "HQ-OP-", 1),
            ("br-north", "OP", "NB-OP-", 1),
            ("br-hq", "MJE", "HQ-MJE-", 1),
            ("br-north", "MJE", "NB-MJE-", 1),
            // Sales/Order-to-Cash doc types. "SO" already had an "br-hq" row above from before this
            // method was diff-based; "br-north" is added here rather than duplicated above.
            ("br-north", "SO", "NB-SO-", 1),
            ("br-hq", "DO", "HQ-DO-", 1),
            ("br-north", "DO", "NB-DO-", 1),
            ("br-hq", "SINV", "HQ-SINV-", 1),
            ("br-north", "SINV", "NB-SINV-", 1),
            ("br-hq", "CRCPT", "HQ-CRCPT-", 1),
            ("br-north", "CRCPT", "NB-CRCPT-", 1),
            ("br-hq", "SRTN", "HQ-SRTN-", 1),
            ("br-north", "SRTN", "NB-SRTN-", 1),
            // BIR-compliant "OR/SI No." series (SalesModuleContext.md §3.7) — per-branch, active,
            // actually printed on the receipt. Format ("000-000000") is a print-template concern;
            // this just tracks the running number.
            ("br-hq", "BIR-OR", "000-", 1),
            ("br-north", "BIR-OR", "000-", 1),
            // Company-wide "overall" series — reserved for future use, modeled here (parked on the
            // head-office branch row since DocumentSequence requires a branch FK) but never surfaced
            // in the UI or on a printed receipt until asked for.
            ("br-hq", "BIR-OR-OVERALL", "000-", 1),
        ];

        var existingPairs = (await context.DocumentSequences.Select(s => new { s.BranchId, s.DocType }).ToListAsync())
            .Select(s => (s.BranchId, s.DocType))
            .ToHashSet();
        var missing = sequences.Where(s => !existingPairs.Contains((s.BranchId, s.DocType))).ToList();
        if (missing.Count == 0)
            return;

        context.DocumentSequences.AddRange(missing.Select(s => new DocumentSequence
        {
            BranchId = s.BranchId,
            DocType = s.DocType,
            Prefix = s.Prefix,
            NextNumber = s.NextNumber,
            PaddingLength = 6
        }));

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} document sequences", missing.Count);
    }

    /// <summary>
    /// Mirrors the FE's mock seed (ZARI-FE/src/data/accounting/glAccounts.ts) so the default chart
    /// of accounts other still-FE-mock modules fall back to (Item.inventoryAccountId,
    /// AdjustmentReason.GlAccountId, etc.) stays meaningful once GlAccount has a real backend —
    /// those fields are loose strings, not FKs, so they don't need updating for this to work.
    /// </summary>
    private static async Task SeedGlAccountsAsync(AppDbContext context, ILogger logger)
    {
        (string Code, string Name, string AccountType, string NormalBalance)[] accounts =
        [
            ("1000", "Cash on Hand", "Asset", "Debit"),
            // Clearing accounts a POS split-tender payment debits for non-cash tenders — cleared out
            // separately as the actual bank settlement/GC redemption happens, same "holds the value
            // until settled elsewhere" shape as "2100" GRNI on the Purchasing side.
            ("1010", "Card Clearing", "Asset", "Debit"),
            ("1020", "Gift Check Clearing", "Asset", "Debit"),
            ("1200", "Accounts Receivable", "Asset", "Debit"),
            ("1400", "Inventory Asset", "Asset", "Debit"),
            ("1450", "Inventory In-Transit", "Asset", "Debit"),
            ("2000", "Accounts Payable", "Liability", "Credit"),
            // Holds the liability for goods physically received but not yet formally billed by the
            // vendor — GRPO credits it (Dr Inventory), AP Invoice debits it (Cr Accounts Payable) to
            // convert the holding liability into a real payable. Goods Return reverses GRPO's side.
            ("2100", "Goods Received Not Invoiced", "Liability", "Credit"),
            // Output VAT liability — Sales Invoice credits this for the VAT portion of every VATable
            // line (extracted per SalesInvoiceLineCalculator); Sales Return debits its share back out
            // when a VATable sale is reversed. See SalesModuleContext.md §3.6.
            ("2200", "VAT Payable", "Liability", "Credit"),
            ("4000", "Sales Revenue", "Revenue", "Credit"),
            // Contra-revenue account for the credit-memo side of a Sales Return — debited alongside
            // the VAT Payable reversal, against a credit to Accounts Receivable.
            ("4100", "Sales Returns and Allowances", "Revenue", "Debit"),
            ("5000", "Cost of Goods Sold", "Cogs", "Debit"),
            ("5100", "Inventory Variance / Shrinkage", "Cogs", "Debit"),
            // Absorbs the difference when an AP Invoice's billed amount differs from the GRPO's
            // originally-received value, so GRNI always clears at the exact amount the GRPO posted.
            ("5200", "Purchase Price Variance", "Cogs", "Debit"),
            // Starter operating-expense accounts for EXPENSE-type AP Invoices (service/expense
            // billing with no GRPO — utilities, professional fees, manpower/salaries, etc.). The
            // expense line picker isn't restricted to just these — any active Expense/Cogs account
            // works — but these cover the common cases out of the box.
            ("6000", "Utilities Expense", "Expense", "Debit"),
            ("6010", "Professional Fees", "Expense", "Debit"),
            ("6020", "Salaries and Wages", "Expense", "Debit"),
            ("6030", "Rent Expense", "Expense", "Debit"),
            ("6900", "Other Operating Expense", "Expense", "Debit"),
        ];

        var existingCodes = await context.GlAccounts.Select(a => a.Code).ToListAsync();
        var missing = accounts.Where(a => !existingCodes.Contains(a.Code)).ToList();
        if (missing.Count == 0)
            return;

        context.GlAccounts.AddRange(missing.Select(a => new GlAccount
        {
            Code = a.Code,
            Name = a.Name,
            AccountType = a.AccountType,
            NormalBalance = a.NormalBalance,
            Status = "active"
        }));

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} GL accounts", missing.Count);
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
    /// Mirrors the FE's mock seed (ZARI-FE/src/data/system-module/currencies.ts). Ids are the
    /// exact "cur-php"/"cur-usd" strings the mock used, since Company.BaseCurrencyId already
    /// stores "cur-php" as a real FK value — matching it exactly needs no data migration.
    /// </summary>
    private static async Task SeedCurrenciesAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Currencies.AnyAsync())
            return;

        context.Currencies.AddRange(
            new Currency { Id = "cur-php", Code = "PHP", Name = "Philippine Peso", Status = "active", CreatedAt = DateTimeOffset.UtcNow },
            new Currency { Id = "cur-usd", Code = "USD", Name = "US Dollar", Status = "active", CreatedAt = DateTimeOffset.UtcNow }
        );

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default currencies");
    }

    /// <summary>Mirrors the FE's mock seed (ZARI-FE/src/data/accounting/fiscalYears.ts) — one open fiscal year.</summary>
    private static async Task SeedFiscalYearsAsync(AppDbContext context, ILogger logger)
    {
        if (await context.FiscalYears.AnyAsync())
            return;

        context.FiscalYears.Add(new FiscalYear
        {
            YearName = "FY 2025",
            StartDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EndDate = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero),
            Status = "OPEN"
        });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default fiscal year");
    }

    private static async Task SeedExchangeRatesAsync(AppDbContext context, ILogger logger)
    {
        if (await context.ExchangeRates.AnyAsync())
            return;

        // Looked up by Code rather than a hardcoded "cur-usd" id — the seeded Currency rows are
        // only guaranteed to exist by Code; their Id can drift if a currency is ever recreated.
        var usd = await context.Currencies.FirstOrDefaultAsync(c => c.Code == "USD");
        if (usd is null)
            return;

        context.ExchangeRates.Add(new ExchangeRate
        {
            CurrencyId = usd.Id,
            RateDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            RateToBase = 56.50m
        });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default exchange rate");
    }

    private static async Task SeedBankAccountsAsync(AppDbContext context, ILogger logger)
    {
        if (await context.BankAccounts.AnyAsync())
            return;

        var cashAccount = await context.GlAccounts.FirstOrDefaultAsync(a => a.Code == "1000");
        var php = await context.Currencies.FirstOrDefaultAsync(c => c.Code == "PHP");
        if (cashAccount is null || php is null)
            return;

        context.BankAccounts.Add(new BankAccount
        {
            BranchId = "br-hq",
            AccountName = "Main Operating Account",
            AccountNumber = "1234-5678-90",
            BankName = "BDO",
            GlAccountId = cashAccount.Id,
            CurrencyId = php.Id
        });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default bank account");
    }

    private static async Task SeedSuppliersAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Suppliers.AnyAsync())
            return;

        var php = await context.Currencies.FirstOrDefaultAsync(c => c.Code == "PHP");
        if (php is null)
            return;

        context.Suppliers.Add(new Supplier
        {
            Code = "SUP-001",
            Name = "Sample Supplier Co.",
            CurrencyId = php.Id,
            Status = "active"
        });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default supplier");
    }

    /// <summary>
    /// One DRAFT + one POSTED demo purchase order, skipped entirely if no items exist yet to put on
    /// the lines — this seeder never creates Items itself, so on a brand-new DB this is a no-op
    /// until at least one item has been created through the app.
    /// </summary>
    private static async Task SeedPurchaseOrdersAsync(AppDbContext context, ILogger logger)
    {
        if (await context.PurchaseOrders.AnyAsync())
            return;

        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Code == "SUP-001");
        var pcs = await context.Uoms.FirstOrDefaultAsync(u => u.Code == "PCS");
        var items = await context.Items.Take(2).ToListAsync();
        if (supplier is null || pcs is null || items.Count == 0)
            return;

        PurchaseOrderLine Line(Item item, decimal qty, decimal unitCost) => new()
        {
            ItemId = item.Id,
            Qty = qty,
            UomId = pcs.Id,
            UnitCost = unitCost
        };

        // Deliberately NOT using the "HQ-PO-" prefix that GetNextDocumentNumberCommandHandler
        // generates for real creates — this seeder can't safely bump the (br-hq, PO)
        // DocumentSequence counter (SeedDocumentSequencesAsync only runs once, on a fully empty
        // table), so a seeded "HQ-PO-000001" would collide with the first real PO ever created.
        context.PurchaseOrders.AddRange(
            new PurchaseOrder
            {
                PoNo = "DEMO-PO-0001",
                BranchId = "br-hq",
                SupplierId = supplier.Id,
                OrderDate = DateTimeOffset.UtcNow,
                Status = "DRAFT",
                Lines = [Line(items[0], 10, 100m)]
            },
            new PurchaseOrder
            {
                PoNo = "DEMO-PO-0002",
                BranchId = "br-hq",
                SupplierId = supplier.Id,
                OrderDate = DateTimeOffset.UtcNow.AddDays(-3),
                Status = "POSTED",
                Lines = items.Count > 1 ? [Line(items[0], 20, 100m), Line(items[1], 5, 250m)] : [Line(items[0], 20, 100m)]
            });

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded default purchase orders");
    }

    /// <summary>
    /// Philippine statutory/special-law discount catalog (DiscountSchemeContext.md §4.6) — a
    /// starting checklist of each category's general treatment, NOT a legal citation. Confirm the
    /// current rate/coverage/VAT treatment against each law's own IRR and the latest BIR Revenue
    /// Regulation before relying on these in production, especially Solo Parent's narrower coverage.
    /// </summary>
    private static async Task SeedStatutoryDiscountTypesAsync(AppDbContext context, ILogger logger)
    {
        (string Code, string Name, decimal DiscountPct, bool IsVatExempt, string RequiredIdLabel)[] types =
        [
            ("SENIOR_CITIZEN", "Senior Citizen (RA 9994)", 20m, true, "Senior Citizen ID No."),
            ("PWD", "Person with Disability (RA 10754)", 20m, true, "PWD ID No."),
            ("NATIONAL_ATHLETE", "National Athlete/Coach (RA 10699)", 20m, true, "Athlete/Coach Accreditation No."),
            ("SOLO_PARENT", "Solo Parent (RA 11861)", 10m, true, "Solo Parent ID No."),
        ];

        var existingCodes = await context.StatutoryDiscountTypes.Select(t => t.Code).ToListAsync();
        var missing = types.Where(t => !existingCodes.Contains(t.Code)).ToList();
        if (missing.Count == 0)
            return;

        context.StatutoryDiscountTypes.AddRange(missing.Select(t => new StatutoryDiscountType
        {
            Code = t.Code,
            Name = t.Name,
            DiscountPct = t.DiscountPct,
            IsVatExempt = t.IsVatExempt,
            RequiredIdLabel = t.RequiredIdLabel,
            Status = "active"
        }));

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} statutory discount types", missing.Count);
    }

    /// <summary>
    /// Starter payment-method catalog for POS Mode's split-tender payment modal — Cash/Card/Gift
    /// Check are just these 3 seeded rows, not hardcoded special cases (see PaymentMethod's own doc
    /// comment); the business can add more via the admin screen without a code change. Looked up by
    /// GL account Code, not a literal Id, same defensive pattern SeedBankAccountsAsync already uses.
    /// </summary>
    private static async Task SeedPaymentMethodsAsync(AppDbContext context, ILogger logger)
    {
        (string Code, string Name, string GlAccountCode, bool RequiresReferenceNo, string? ReferenceNoLabel, bool RequiresBankOrPartnerName, int DisplayOrder)[] methods =
        [
            ("CASH", "Cash", "1000", false, null, false, 1),
            ("CARD", "Card", "1010", true, "Card Number", true, 2),
            ("GIFT_CHECK", "Gift Check", "1020", true, "GC Number", true, 3),
        ];

        var existingCodes = await context.PaymentMethods.Select(m => m.Code).ToListAsync();
        var missing = methods.Where(m => !existingCodes.Contains(m.Code)).ToList();
        if (missing.Count == 0)
            return;

        var glAccountsByCode = await context.GlAccounts.ToDictionaryAsync(a => a.Code, a => a.Id);

        context.PaymentMethods.AddRange(missing.Select(m => new PaymentMethod
        {
            Code = m.Code,
            Name = m.Name,
            GlAccountId = glAccountsByCode[m.GlAccountCode],
            RequiresReferenceNo = m.RequiresReferenceNo,
            ReferenceNoLabel = m.ReferenceNoLabel,
            RequiresBankOrPartnerName = m.RequiresBankOrPartnerName,
            DisplayOrder = m.DisplayOrder,
            Status = "active"
        }));

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} payment methods", missing.Count);
    }

    /// <summary>
    /// One "Walk-in Customer" per branch — POS Mode's default selection when the cashier doesn't
    /// set a specific member. Customer.CustomerId stays required on SalesInvoice (unchanged), so a
    /// real seeded row is simpler than making that FK nullable just for this one flow.
    /// </summary>
    private static async Task SeedWalkInCustomersAsync(AppDbContext context, ILogger logger)
    {
        const string walkInName = "Walk-in Customer";
        var branches = await context.Branches.ToListAsync();
        var existingBranchIds = await context.Customers.Where(c => c.Name == walkInName).Select(c => c.BranchId).ToListAsync();
        var missingBranches = branches.Where(b => !existingBranchIds.Contains(b.Id)).ToList();
        if (missingBranches.Count == 0)
            return;

        context.Customers.AddRange(missingBranches.Select(b => new Customer
        {
            Name = walkInName,
            Type = "individual",
            Email = "walkin@zari.local",
            Phone = "N/A",
            BranchId = b.Id,
            Status = "active",
            Owner = "System",
            Address = "Walk-in"
        }));

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} walk-in customers", missingBranches.Count);
    }

    /// <summary>
    /// One row per admin/transactional page the app actually has today — the catalog Role
    /// templates and per-user overrides grant Form-level action flags against.
    /// </summary>
    private static async Task SeedFormsAsync(AppDbContext context, ILogger logger)
    {
        (string Code, string Name, string Module)[] forms =
        [
            ("DASHBOARD", "Dashboard", "Dashboard"),

            ("CUSTOMERS", "Customers", "CRM"),

            ("USERS", "Users", "System"),
            ("ROLES", "Roles", "System"),
            ("BRANCHES", "Branches", "System"),
            ("COMPANY", "Company Settings", "System"),
            ("DOCUMENT_SEQUENCES", "Document Sequences", "System"),
            ("CURRENCIES", "Currencies", "System"),
            ("EXCHANGE_RATES", "Exchange Rates", "System"),

            ("GL_ACCOUNTS", "GL Accounts", "Accounting"),
            ("COST_CENTERS", "Cost Centers", "Accounting"),
            ("GL_JOURNALS", "Journal Entries", "Accounting"),
            ("MANUAL_JOURNAL_ENTRIES", "Manual Journal Entries", "Accounting"),
            ("TAX_CODES", "Tax Codes", "Accounting"),
            ("FISCAL_YEARS", "Fiscal Years", "Accounting"),
            ("BANK_ACCOUNTS", "Bank Accounts", "Accounting"),

            ("SUPPLIERS", "Suppliers", "Purchasing"),
            ("PURCHASE_ORDERS", "Purchase Orders", "Purchasing"),
            ("PURCHASE_REQUESTS", "Purchase Requests", "Purchasing"),
            ("GOODS_RECEIPT_PO", "Goods Receipt (PO)", "Purchasing"),
            ("GOODS_RETURNS", "Goods Returns", "Purchasing"),
            ("AP_INVOICES", "AP Invoices", "Purchasing"),
            ("OUTGOING_PAYMENTS", "Outgoing Payments", "Purchasing"),
            ("PURCHASE_RETURN_REASONS", "Purchase Return Reasons", "Purchasing"),

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

            ("SALES_ORDERS", "Sales Orders", "Sales"),
            ("DELIVERIES", "Deliveries", "Sales"),
            ("SALES_INVOICES", "Sales Invoices", "Sales"),
            ("CUSTOMER_PAYMENTS", "Customer Payments", "Sales"),
            ("SALES_RETURNS", "Sales Returns", "Sales"),
            ("POS_CLOSING", "Daily POS Closing", "Sales"),
            ("DISCOUNT_RULES", "Discount Rules", "Sales"),
            ("STATUTORY_DISCOUNT_TYPES", "Statutory Discount Types", "Sales"),
            ("POS_MODE", "POS Mode", "Sales"),
            ("POS_TERMINALS", "POS Terminals", "Sales"),
            ("PAYMENT_METHODS", "Payment Methods", "Sales"),
            ("POS_PROMO_SLIDES", "POS Promo Slides", "Sales"),
        ];

        var existingCodes = await context.Forms.Select(f => f.Code).ToListAsync();
        var missing = forms.Where(f => !existingCodes.Contains(f.Code)).ToList();
        if (missing.Count == 0)
            return;

        context.Forms.AddRange(missing.Select(f => new Form { Code = f.Code, Name = f.Name, Module = f.Module }));

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} forms", missing.Count);
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
        var adminRole = await roleManager.FindByNameAsync("Admin");
        var managerRole = await roleManager.FindByNameAsync("Manager");
        var staffRole = await roleManager.FindByNameAsync("Staff");
        if (adminRole is null || managerRole is null || staffRole is null)
            return;

        var allFormCodes = await context.Forms.Select(f => f.Code).ToListAsync();
        var existingGrants = (await context.RolePermissions.Select(rp => new { rp.RoleId, rp.FormCode }).ToListAsync())
            .Select(rp => (rp.RoleId, rp.FormCode))
            .ToHashSet();

        (bool View, bool Create, bool Edit, bool Approve, bool Cancel, bool Delete) full = (true, true, true, true, true, true);
        (bool View, bool Create, bool Edit, bool Approve, bool Cancel, bool Delete) manage = (true, true, true, false, false, false);
        (bool View, bool Create, bool Edit, bool Approve, bool Cancel, bool Delete) operate = (true, true, true, true, true, false);
        (bool View, bool Create, bool Edit, bool Approve, bool Cancel, bool Delete) prepare = (true, true, true, false, false, false);
        (bool View, bool Create, bool Edit, bool Approve, bool Cancel, bool Delete) view = (true, false, false, false, false, false);

        var permissions = new List<RolePermission>();

        void Grant(string roleId, string formCode, (bool View, bool Create, bool Edit, bool Approve, bool Cancel, bool Delete) flags)
        {
            if (!allFormCodes.Contains(formCode) || existingGrants.Contains((roleId, formCode)))
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
            "STOCK_TRANSFER_REQUESTS", "STOCK_LOCATION_TRANSFERS", "APPROVAL_REQUESTS", "PURCHASE_ORDERS",
            "PURCHASE_REQUESTS", "GOODS_RECEIPT_PO", "GOODS_RETURNS", "AP_INVOICES", "OUTGOING_PAYMENTS",
            "MANUAL_JOURNAL_ENTRIES",
            "SALES_ORDERS", "DELIVERIES", "SALES_INVOICES", "CUSTOMER_PAYMENTS", "SALES_RETURNS", "POS_CLOSING", "POS_MODE"
        ];
        string[] managerMasterDataForms =
        [
            "UOMS", "ITEM_CATEGORIES", "WAREHOUSES", "STORAGE_LOCATIONS", "ITEMS",
            "ADJUSTMENT_REASONS", "ITEM_BRANCH_SETTINGS", "STOCK_RESERVATIONS", "SERIAL_NUMBERS",
            "PURCHASE_RETURN_REASONS", "DISCOUNT_RULES", "STATUTORY_DISCOUNT_TYPES",
            "POS_TERMINALS", "PAYMENT_METHODS", "POS_PROMO_SLIDES"
        ];
        string[] managerViewOnlyForms =
        [
            "DASHBOARD", "USERS", "BRANCHES", "DOCUMENT_SEQUENCES", "GL_ACCOUNTS", "COST_CENTERS", "GL_JOURNALS", "NOTIFICATIONS",
            "CURRENCIES", "EXCHANGE_RATES", "TAX_CODES", "FISCAL_YEARS", "BANK_ACCOUNTS"
        ];

        foreach (var formCode in managerTransactionalForms)
            Grant(managerRole.Id, formCode, operate);
        foreach (var formCode in managerMasterDataForms)
            Grant(managerRole.Id, formCode, manage);
        foreach (var formCode in managerViewOnlyForms)
            Grant(managerRole.Id, formCode, view);
        Grant(managerRole.Id, "CUSTOMERS", manage);
        Grant(managerRole.Id, "SUPPLIERS", manage);

        string[] staffTransactionalForms =
        [
            "GOODS_RECEIPTS", "GOODS_ISSUES", "STOCK_ADJUSTMENTS", "STOCK_OPNAMES",
            "STOCK_TRANSFER_REQUESTS", "STOCK_LOCATION_TRANSFERS", "PURCHASE_ORDERS",
            "PURCHASE_REQUESTS", "GOODS_RECEIPT_PO", "GOODS_RETURNS", "AP_INVOICES", "OUTGOING_PAYMENTS",
            "MANUAL_JOURNAL_ENTRIES",
            "SALES_ORDERS", "DELIVERIES", "SALES_INVOICES", "CUSTOMER_PAYMENTS", "SALES_RETURNS", "POS_CLOSING", "POS_MODE"
        ];
        string[] staffViewOnlyForms =
        [
            "DASHBOARD", "CUSTOMERS", "UOMS", "ITEM_CATEGORIES", "WAREHOUSES", "STORAGE_LOCATIONS",
            "ITEMS", "ADJUSTMENT_REASONS", "ITEM_BRANCH_SETTINGS", "STOCK_RESERVATIONS",
            "SERIAL_NUMBERS", "APPROVAL_REQUESTS", "NOTIFICATIONS", "SUPPLIERS", "PURCHASE_RETURN_REASONS",
            "DISCOUNT_RULES", "STATUTORY_DISCOUNT_TYPES",
            "POS_TERMINALS", "PAYMENT_METHODS", "POS_PROMO_SLIDES"
        ];

        foreach (var formCode in staffTransactionalForms)
            Grant(staffRole.Id, formCode, prepare);
        foreach (var formCode in staffViewOnlyForms)
            Grant(staffRole.Id, formCode, view);

        if (permissions.Count == 0)
            return;

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
