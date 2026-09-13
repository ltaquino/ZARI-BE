namespace ZARI.Application.UnitTests.Features.Loan.LoanProduct;

using FluentAssertions;
using ZARI.Application.Features.Loan.LoanProducts.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeleteLoanProductCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Product_When_Valid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var product = LoanTestFixtures.LoanProduct();
        dbContext.LoanProducts.Add(product);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteLoanProductCommandHandler(dbContext, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLoanProductCommand(product.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        dbContext.LoanProducts.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new DeleteLoanProductCommandHandler(dbContext, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLoanProductCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var product = LoanTestFixtures.LoanProduct();
        dbContext.LoanProducts.Add(product);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LOAN_PRODUCTS", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteLoanProductCommandHandler(dbContext, permissions);

        var result = await handler.HandleAsync(new DeleteLoanProductCommand(product.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        dbContext.LoanProducts.Should().HaveCount(1);
    }
}
