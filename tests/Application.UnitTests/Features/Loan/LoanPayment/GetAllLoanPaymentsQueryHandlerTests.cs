namespace ZARI.Application.UnitTests.Features.Loan.LoanPayment;

using ZARI.Application.Features.Loan.LoanPayments.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllLoanPaymentsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Payments()
    {
        await using var db = TestDbContextFactory.Create();
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
        db.LoanPayments.Add(new LoanPayment
        {
            PaymentNo = "LOAN-PMT-0001", BranchId = branch.Id, LoanAccountId = account.Id, PaymentDate = DateTimeOffset.UtcNow,
            Amount = 500, PaymentMethodId = paymentMethod.Id, Status = "POSTED"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllLoanPaymentsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllLoanPaymentsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LOAN_PAYMENTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllLoanPaymentsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllLoanPaymentsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
