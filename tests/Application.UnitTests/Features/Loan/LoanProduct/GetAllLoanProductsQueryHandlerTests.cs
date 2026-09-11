namespace ZARI.Application.UnitTests.Features.Loan.LoanProduct;

using FluentAssertions;
using ZARI.Application.Features.Loan.LoanProducts.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetAllLoanProductsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Products_Ordered_By_Code()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.LoanProducts.AddRange(LoanTestFixtures.LoanProduct(code: "B-PROD"), LoanTestFixtures.LoanProduct(code: "A-PROD"));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllLoanProductsQueryHandler(dbContext, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllLoanProductsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Select(p => p.Code).Should().ContainInOrder("A-PROD", "B-PROD");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LOAN_PRODUCTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllLoanProductsQueryHandler(dbContext, permissions);

        var result = await handler.HandleAsync(new GetAllLoanProductsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
