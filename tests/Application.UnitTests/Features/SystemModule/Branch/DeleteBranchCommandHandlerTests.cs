namespace ZARI.Application.UnitTests.Features.SystemModule.Branch;

using ZARI.Application.Features.SystemModule.Branches.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class DeleteBranchCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Branch()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteBranchCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteBranchCommand(branch.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.Branches.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteBranchCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteBranchCommand("br-missing"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(branch);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("BRANCHES", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeleteBranchCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeleteBranchCommand(branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Has_Warehouses()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(branch);
        db.Warehouses.Add(new Warehouse { BranchId = branch.Id, Code = "WH1", Name = "Main Warehouse", WarehouseType = "main", Status = "active" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteBranchCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteBranchCommand(branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.HasWarehouses");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Has_Customers()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch("br-1");
        db.Branches.Add(branch);
        db.Customers.Add(LoanTestFixtures.Customer(branch.Id));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteBranchCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteBranchCommand(branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.HasCustomers");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Has_BankAccounts()
    {
        await using var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch("br-1");
        var glAccount = LoanTestFixtures.GlAccount();
        db.Branches.Add(branch);
        db.GlAccounts.Add(glAccount);
        db.BankAccounts.Add(new BankAccount { BranchId = branch.Id, AccountName = "Main", AccountNumber = "123", BankName = "Test Bank", GlAccountId = glAccount.Id });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteBranchCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteBranchCommand(branch.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.HasBankAccounts");
    }
}
