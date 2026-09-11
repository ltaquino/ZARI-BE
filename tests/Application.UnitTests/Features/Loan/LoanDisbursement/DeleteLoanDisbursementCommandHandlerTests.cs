namespace ZARI.Application.UnitTests.Features.Loan.LoanDisbursement;

using ZARI.Application.Features.Loan.LoanDisbursements.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class DeleteLoanDisbursementCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanDisbursement disbursement)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var glAccount = LoanTestFixtures.GlAccount();
        db.GlAccounts.Add(glAccount);
        var paymentMethod = LoanTestFixtures.PaymentMethod(glAccount.Id);
        db.PaymentMethods.Add(paymentMethod);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, loanReceivableAccountId: glAccount.Id);
        db.LoanAccounts.Add(account);
        var disbursement = new LoanDisbursement
        {
            DisbursementNo = "LOAN-DISB-0001", BranchId = branch.Id, LoanAccountId = account.Id, DisbursementDate = DateTimeOffset.UtcNow,
            Amount = account.PrincipalAmount, PaymentMethodId = paymentMethod.Id, Status = status
        };
        db.LoanDisbursements.Add(disbursement);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, disbursement);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_When_Draft()
    {
        var (db, disbursement) = await Seed();
        var handler = new DeleteLoanDisbursementCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLoanDisbursementCommand(disbursement.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.LoanDisbursements.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, disbursement) = await Seed(status: "POSTED");
        var handler = new DeleteLoanDisbursementCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLoanDisbursementCommand(disbursement.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteLoanDisbursementCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLoanDisbursementCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, disbursement) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Delete, disbursement.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteLoanDisbursementCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteLoanDisbursementCommand(disbursement.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
