namespace ZARI.Application.UnitTests.Features.Loan.LoanProduct;

using FluentAssertions;
using ZARI.Application.Features.Loan.LoanProducts.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class UpdateLoanProductCommandHandlerTests
{
    private static UpdateLoanProductCommand Command(Guid id, string code = "PROD-1") =>
        new(id, code, "Updated Name", "DIMINISHING", 15, 2000, 50000, 2, 12, "WEEKLY", 3, 5, true, true, null, null, null, "inactive");

    [Fact]
    public async Task HandleAsync_Should_Update_Product_When_Valid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var product = LoanTestFixtures.LoanProduct();
        dbContext.LoanProducts.Add(product);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateLoanProductCommandHandler(dbContext, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(product.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var saved = await dbContext.LoanProducts.FindAsync([product.Id], TestContext.Current.CancellationToken);
        saved!.Name.Should().Be("Updated Name");
        saved.PenaltyRatePct.Should().Be(5);
        saved.Status.Should().Be("inactive");
    }

    [Fact]
    public async Task HandleAsync_Should_Persist_Cic_Contract_Type_Code()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var product = LoanTestFixtures.LoanProduct();
        dbContext.LoanProducts.Add(product);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateLoanProductCommandHandler(dbContext, LoanTestFixtures.AllowAllPermissionService());
        var command = Command(product.Id) with { CicContractTypeCode = "15" };

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var saved = await dbContext.LoanProducts.FindAsync([product.Id], TestContext.Current.CancellationToken);
        saved!.CicContractTypeCode.Should().Be("15");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new UpdateLoanProductCommandHandler(dbContext, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

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
        permissions.HasPermissionAsync("LOAN_PRODUCTS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdateLoanProductCommandHandler(dbContext, permissions);

        var result = await handler.HandleAsync(Command(product.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts_With_Another_Product()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var product = LoanTestFixtures.LoanProduct(code: "PROD-A");
        var other = LoanTestFixtures.LoanProduct(code: "PROD-B");
        dbContext.LoanProducts.AddRange(product, other);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateLoanProductCommandHandler(dbContext, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(product.Id, code: "PROD-B"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}
