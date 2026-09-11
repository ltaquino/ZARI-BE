namespace ZARI.Application.UnitTests.Features.Loan.LoanProduct;

using FluentAssertions;
using ZARI.Application.Features.Loan.LoanProducts.Create;
using ZARI.Application.UnitTests.TestSupport;

public sealed class CreateLoanProductCommandHandlerTests
{
    private static CreateLoanProductCommand Command(Guid? loanReceivableAccountId = null, Guid? interestIncomeAccountId = null, Guid? penaltyIncomeAccountId = null, string code = "NEW-PROD") =>
        new(code, "New Product", "DIMINISHING", 12, 1000, 100000, 1, 24, "MONTHLY", 5, 2, false, false,
            loanReceivableAccountId, interestIncomeAccountId, penaltyIncomeAccountId, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Product_When_Valid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateLoanProductCommandHandler(dbContext, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("NEW-PROD");
        result.Value.Status.Should().Be("active");
        dbContext.LoanProducts.Should().HaveCount(1);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("LOAN_PRODUCTS", ZARI.Domain.Common.FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateLoanProductCommandHandler(dbContext, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ZARI.Domain.Common.ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        await using var dbContext = TestDbContextFactory.Create();
        dbContext.LoanProducts.Add(LoanTestFixtures.LoanProduct(code: "DUP-CODE"));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateLoanProductCommandHandler(dbContext, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(code: "DUP-CODE"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ZARI.Domain.Common.ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_LoanReceivableAccount_Not_Found()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var handler = new CreateLoanProductCommandHandler(dbContext, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(loanReceivableAccountId: Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ZARI.Domain.Common.ErrorType.NotFound);
    }
}
