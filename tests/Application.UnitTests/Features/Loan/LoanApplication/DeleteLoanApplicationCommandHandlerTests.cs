namespace ZARI.Application.UnitTests.Features.Loan.LoanApplication;

using ZARI.Application.Features.Loan.LoanApplications.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class DeleteLoanApplicationCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanApplication app)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var app = new LoanApplication { ApplicationNo = "A1", BranchId = branch.Id, CustomerId = customer.Id, LoanProductId = product.Id, ApplicationDate = DateTimeOffset.UtcNow, RequestedPrincipal = 10000, RequestedTermMonths = 6, Status = status };
        db.LoanApplications.Add(app);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, app);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_When_Draft()
    {
        var (db, app) = await Seed();
        var handler = new DeleteLoanApplicationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLoanApplicationCommand(app.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.LoanApplications.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, app) = await Seed(status: "APPROVED");
        var handler = new DeleteLoanApplicationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLoanApplicationCommand(app.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanApplication.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteLoanApplicationCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLoanApplicationCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}
