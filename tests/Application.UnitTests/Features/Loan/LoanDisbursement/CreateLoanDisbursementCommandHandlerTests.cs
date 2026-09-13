namespace ZARI.Application.UnitTests.Features.Loan.LoanDisbursement;

using ZARI.Application.Features.Loan.LoanDisbursements.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateLoanDisbursementCommandHandlerTests
{
    private static CreateLoanDisbursementCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("LOAN-DISB-0001"))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_DISBURSEMENT", "x", "br-1", "CREATED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, LoanAccount account, Guid paymentMethodId)> Seed(string accountStatus = "PENDING_DISBURSEMENT")
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
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, status: accountStatus, loanReceivableAccountId: glAccount.Id);
        db.LoanAccounts.Add(account);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, account, paymentMethod.Id);
    }

    private static CreateLoanDisbursementCommand Command(string branchId, Guid loanAccountId, Guid paymentMethodId, decimal amount) =>
        new(branchId, loanAccountId, DateTimeOffset.UtcNow, amount, paymentMethodId, null, null, null, "tester");

    [Fact]
    public async Task HandleAsync_Should_Create_Disbursement_At_Draft()
    {
        var (db, branchId, account, paymentMethodId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id, paymentMethodId, account.PrincipalAmount), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, account, paymentMethodId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_DISBURSEMENTS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(Command(branchId, account.Id, paymentMethodId, account.PrincipalAmount), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Account_Not_Pending_Disbursement()
    {
        var (db, branchId, account, paymentMethodId) = await Seed(accountStatus: "ACTIVE");
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id, paymentMethodId, account.PrincipalAmount), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.AccountNotPendingDisbursement");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Amount_Does_Not_Match_Principal()
    {
        var (db, branchId, account, paymentMethodId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id, paymentMethodId, account.PrincipalAmount - 1), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.AmountMustMatchPrincipal");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Already_Has_Active_Disbursement()
    {
        var (db, branchId, account, paymentMethodId) = await Seed();
        db.LoanDisbursements.Add(new LoanDisbursement
        {
            DisbursementNo = "LOAN-DISB-EXIST", BranchId = branchId, LoanAccountId = account.Id, DisbursementDate = DateTimeOffset.UtcNow,
            Amount = account.PrincipalAmount, PaymentMethodId = paymentMethodId, Status = "DRAFT"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, account.Id, paymentMethodId, account.PrincipalAmount), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanDisbursement.AlreadyExists");
        await db.DisposeAsync();
    }
}
