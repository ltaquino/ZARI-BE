namespace ZARI.Application.UnitTests.Features.Loan.LoanApplication;

using ZARI.Application.Features.Loan.LoanApplications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class GetAllLoanApplicationsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Applications()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        db.LoanApplications.Add(new LoanApplication { ApplicationNo = "A1", BranchId = branch.Id, CustomerId = customer.Id, LoanProductId = product.Id, ApplicationDate = DateTimeOffset.UtcNow, RequestedPrincipal = 10000, RequestedTermMonths = 6, Status = "DRAFT" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllLoanApplicationsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllLoanApplicationsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LOAN_APPLICATIONS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllLoanApplicationsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllLoanApplicationsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
