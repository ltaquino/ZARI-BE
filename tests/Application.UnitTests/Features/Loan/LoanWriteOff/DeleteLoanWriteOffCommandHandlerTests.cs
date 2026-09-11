namespace ZARI.Application.UnitTests.Features.Loan.LoanWriteOff;

using ZARI.Application.Features.Loan.LoanWriteOffs.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class DeleteLoanWriteOffCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, LoanWriteOff writeOff)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        var expenseAccount = LoanTestFixtures.GlAccount(code: "6910", name: "Loan Write-off Expense");
        db.GlAccounts.Add(expenseAccount);
        var account = LoanTestFixtures.LoanAccount(branch.Id, customer.Id, product.Id, withSchedule: false);
        db.LoanAccounts.Add(account);
        var writeOff = new LoanWriteOff
        {
            WriteOffNo = "LOAN-WO-0001", BranchId = branch.Id, LoanAccountId = account.Id, WriteOffDate = DateTimeOffset.UtcNow,
            Amount = 12000, WriteOffExpenseAccountId = expenseAccount.Id, Reason = "x", Status = status
        };
        db.LoanWriteOffs.Add(writeOff);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, writeOff);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_When_Draft()
    {
        var (db, writeOff) = await Seed();
        var handler = new DeleteLoanWriteOffCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLoanWriteOffCommand(writeOff.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.LoanWriteOffs.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, writeOff) = await Seed(status: "POSTED");
        var handler = new DeleteLoanWriteOffCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLoanWriteOffCommand(writeOff.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanWriteOff.NotDraft");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteLoanWriteOffCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteLoanWriteOffCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, writeOff) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_WRITE_OFFS", FormAction.Delete, writeOff.BranchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteLoanWriteOffCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteLoanWriteOffCommand(writeOff.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }
}
