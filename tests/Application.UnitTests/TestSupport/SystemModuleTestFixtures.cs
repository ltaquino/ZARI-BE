namespace ZARI.Application.UnitTests.TestSupport;

using ZARI.Domain.Entities;

/// <summary>
/// Entity builders for the SystemModule feature area (Branch/Company/Currency/DocumentSequence/
/// Forms) not already covered by <see cref="LoanTestFixtures"/> — Branch and GlAccount are generic
/// master data shared across every module, so tests here reuse <see cref="LoanTestFixtures"/>'s own
/// builders for those instead of duplicating them. Same InMemory-provider caveats apply as
/// documented on <see cref="LoanTestFixtures"/> (Guid-FK seeding order; <c>ExecuteUpdateAsync</c>
/// unsupported) — <see cref="Branch"/>'s IsHeadOffice reassignment and
/// GetNextDocumentNumberCommandHandler's compare-and-swap allocation both hit the same
/// ExecuteUpdateAsync gap.
/// </summary>
internal static class SystemModuleTestFixtures
{
    public static Company Company(string baseCurrencyId = "cur-php", string code = "SIDC", string name = "Test Cooperative") => new()
    {
        Code = code,
        Name = name,
        BaseCurrencyId = baseCurrencyId,
        SalesOrderQuickPostEnabled = false,
        DeliveryQuickPostEnabled = false,
        SalesInvoiceQuickPostEnabled = false,
        CustomerPaymentQuickPostEnabled = false,
        SalesReturnQuickPostEnabled = false
    };

    public static Currency Currency(string id = "cur-php", string code = "PHP", string? name = "Philippine Peso", string status = "active") => new()
    {
        Id = id,
        Code = code,
        Name = name,
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow
    };

    public static DocumentSequence DocumentSequence(string branchId, string docType = "SO", string prefix = "SO-", int nextNumber = 1, int paddingLength = 5) => new()
    {
        BranchId = branchId,
        DocType = docType,
        Prefix = prefix,
        NextNumber = nextNumber,
        PaddingLength = paddingLength
    };

    public static Form Form(string code = "BRANCHES", string name = "Branches", string module = "System") => new()
    {
        Code = code,
        Name = name,
        Module = module
    };
}
