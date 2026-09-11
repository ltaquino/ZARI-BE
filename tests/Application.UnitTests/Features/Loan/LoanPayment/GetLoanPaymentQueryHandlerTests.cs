namespace ZARI.Application.UnitTests.Features.Loan.LoanPayment;

using ZARI.Application.Features.Loan.LoanPayments.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetLoanPaymentQueryHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanPayment payment)> Seed()
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
        var payment = new LoanPayment
        {
            PaymentNo = "LOAN-PMT-0001", BranchId = branch.Id, LoanAccountId = account.Id, PaymentDate = DateTimeOffset.UtcNow,
            Amount = 500, PaymentMethodId = paymentMethod.Id, Status = "POSTED"
        };
        db.LoanPayments.Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, payment);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Payment_When_Found()
    {
        var (db, payment) = await Seed();
        var handler = new GetLoanPaymentQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLoanPaymentQuery(payment.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PaymentNo.Should().Be("LOAN-PMT-0001");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetLoanPaymentQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLoanPaymentQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, payment) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_PAYMENTS", FormAction.View, payment.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetLoanPaymentQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetLoanPaymentQuery(payment.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
