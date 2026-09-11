namespace ZARI.Application.UnitTests.Features.Loan.LoanDisbursement;

using ZARI.Application.Features.Loan.LoanDisbursements.Update;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class UpdateLoanDisbursementCommandHandlerTests
{
    private static UpdateLoanDisbursementCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_DISBURSEMENT", "x", "br-1", "UPDATED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanDisbursement disbursement, string branchId, Guid paymentMethodId)> Seed(string status = "DRAFT")
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
        return (db, disbursement, branch.Id, paymentMethod.Id);
    }

    private static UpdateLoanDisbursementCommand Command(Guid id, string branchId, Guid paymentMethodId, decimal amount) =>
        new(id, branchId, DateTimeOffset.UtcNow, amount, paymentMethodId, null, null, null, "tester");

    [Fact]
    public async Task HandleAsync_Should_Update_When_Draft()
    {
        var (db, disbursement, branchId, paymentMethodId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(disbursement.Id, branchId, paymentMethodId, disbursement.Amount), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        var (db, _, branchId, paymentMethodId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), branchId, paymentMethodId, 1000), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, disbursement, branchId, paymentMethodId) = await Seed(status: "POSTED");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(disbursement.Id, branchId, paymentMethodId, disbursement.Amount), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Amount_Mismatch()
    {
        var (db, disbursement, branchId, paymentMethodId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(disbursement.Id, branchId, paymentMethodId, disbursement.Amount + 500), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.AmountMustMatchPrincipal");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, disbursement, branchId, paymentMethodId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Edit, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(Command(disbursement.Id, branchId, paymentMethodId, disbursement.Amount), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
