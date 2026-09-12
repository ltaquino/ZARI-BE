namespace ZARI.Application.UnitTests.Features.Loan.Reports.CicCsdf;

using ZARI.Application.Features.Loan.Reports.CicCsdf;
using ZARI.Application.Features.Loan.Reports.CisaCreditData;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetCicCsdfExportQueryHandlerTests
{
    private static GetCicCsdfExportQueryHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null)
    {
        permissions ??= LoanTestFixtures.AllowAllPermissionService();
        var cisaHandler = new GetCisaCreditDataExportQueryHandler(db, permissions);
        return new GetCicCsdfExportQueryHandler(db, permissions, cisaHandler);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Empty_When_No_Accounts()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await Handler(db).HandleAsync(new GetCicCsdfExportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IdRecords.Should().BeEmpty();
        result.Value!.CiRecords.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LOAN_ACCOUNTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new GetCicCsdfExportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Default_ProviderCode_When_Not_Supplied()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        db.LoanAccounts.Add(LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, withSchedule: false));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new GetCicCsdfExportQuery(null, null, null, null), TestContext.Current.CancellationToken);

        result.Value!.ProviderCode.Should().Be("UNSET000");
    }

    [Fact]
    public async Task HandleAsync_Should_Produce_One_Id_And_One_Ci_Record_For_A_Single_Account()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id);
        db.LoanAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new GetCicCsdfExportQuery(null, null, null, "CO123456"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IdRecords.Should().ContainSingle();
        result.Value!.CiRecords.Should().ContainSingle();
        result.Value!.IdRecords[0][4].Should().Be(customer.Id.ToString());
        result.Value!.CiRecords[0][6].Should().Be(account.LoanAcctNo);
        result.Value!.CiRecords[0][5].Should().Be("B");
    }

    [Fact]
    public async Task HandleAsync_Should_Add_A_Second_Id_Record_For_A_CoMaker_Linked_To_A_Customer()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var borrower = LoanTestFixtures.Customer(branch.Id, "Borrower One");
        var guarantor = LoanTestFixtures.Customer(branch.Id, "Guarantor Two");
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(borrower);
        db.Customers.Add(guarantor);
        db.LoanProducts.Add(product);
        var application = new LoanApplication
        {
            ApplicationNo = "LOAN-APP-0001", BranchId = branch.Id, CustomerId = borrower.Id, LoanProductId = product.Id,
            RequestedPrincipal = 12000, RequestedTermMonths = 6, Purpose = "test", ApplicationDate = DateTimeOffset.UtcNow, Status = "POSTED"
        };
        db.LoanApplications.Add(application);
        var account = LoanTestFixtures.LoanAccount(branch.Id, borrower.Id, product.Id, withSchedule: false);
        account.LoanApplicationId = application.Id;
        db.LoanAccounts.Add(account);
        db.LoanCoMakers.Add(new LoanCoMaker { LoanApplicationId = application.Id, CoMakerCustomerId = guarantor.Id, Name = guarantor.Name });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await Handler(db).HandleAsync(new GetCicCsdfExportQuery(null, null, null), TestContext.Current.CancellationToken);

        result.Value!.IdRecords.Should().HaveCount(2);
        result.Value!.IdRecords.Select(r => r[4]).Should().Contain([borrower.Id.ToString(), guarantor.Id.ToString()]);
        result.Value!.CiRecords[0][42].Should().Be(guarantor.Id.ToString());
        result.Value!.CiRecords[0][43].Should().Be(guarantor.Name);
    }
}
