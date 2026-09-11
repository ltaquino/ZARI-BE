namespace ZARI.Application.UnitTests.Features.Loan.LoanProduct;

using FluentAssertions;
using ZARI.Application.Features.Loan.LoanProducts.Get;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class GetLoanProductQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Product_When_Found()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var product = LoanTestFixtures.LoanProduct();
        dbContext.LoanProducts.Add(product);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetLoanProductQueryHandler(dbContext, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLoanProductQuery(product.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(product.Id);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var product = LoanTestFixtures.LoanProduct();
        dbContext.LoanProducts.Add(product);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LOAN_PRODUCTS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetLoanProductQueryHandler(dbContext, permissions);

        var result = await handler.HandleAsync(new GetLoanProductQuery(product.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new GetLoanProductQueryHandler(dbContext, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetLoanProductQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}
