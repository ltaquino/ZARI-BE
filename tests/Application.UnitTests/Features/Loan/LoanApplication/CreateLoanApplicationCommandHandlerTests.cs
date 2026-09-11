namespace ZARI.Application.UnitTests.Features.Loan.LoanApplication;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanApplications.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateLoanApplicationCommandHandlerTests
{
    private static CreateLoanApplicationCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(
            dbContext,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("LOAN-APP-0001"))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_APPLICATION", "x", "br-1", "CREATED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid customerId, Guid productId)> Seed(bool requiresCollateral = false, bool requiresCoMaker = false)
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct(requiresCollateral: requiresCollateral, requiresCoMaker: requiresCoMaker);
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, customer.Id, product.Id);
    }

    private static CreateLoanApplicationCommand Command(string branchId, Guid customerId, Guid productId, decimal principal = 10000, int term = 6,
        List<LoanCollateralInput>? collaterals = null, List<LoanCoMakerInput>? coMakers = null) =>
        new(branchId, customerId, productId, DateTimeOffset.UtcNow, principal, term, "purpose", null, "tester",
            collaterals ?? [], coMakers ?? []);

    [Fact]
    public async Task HandleAsync_Should_Create_Application_When_Valid()
    {
        var (db, branchId, customerId, productId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, customerId, productId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("DRAFT");
        result.Value.ApplicationNo.Should().Be("LOAN-APP-0001");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Application_With_Collateral_And_CoMaker()
    {
        var (db, branchId, customerId, productId) = await Seed();
        var handler = BuildHandler(db);
        var collaterals = new List<LoanCollateralInput> { new("Motorcycle", "VEHICLE", 30000, "OR-1") };
        var coMakers = new List<LoanCoMakerInput> { new(null, "Maria Santos", "0917") };

        var result = await handler.HandleAsync(Command(branchId, customerId, productId, collaterals: collaterals, coMakers: coMakers), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Collaterals.Should().HaveCount(1);
        result.Value.CoMakers.Should().HaveCount(1);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, customerId, productId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_APPLICATIONS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(Command(branchId, customerId, productId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Branch_Not_Found()
    {
        var (db, _, customerId, productId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command("br-missing", customerId, productId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Branch.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Customer_Not_Found()
    {
        var (db, branchId, _, productId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, Guid.NewGuid(), productId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Customer.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Product_Not_Found()
    {
        var (db, branchId, customerId, _) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, customerId, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanProduct.NotFound");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Principal_Out_Of_Range()
    {
        var (db, branchId, customerId, productId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, customerId, productId, principal: 500), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanApplication.PrincipalOutOfRange");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Term_Out_Of_Range()
    {
        var (db, branchId, customerId, productId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, customerId, productId, term: 100), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanApplication.TermOutOfRange");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Product_Requires_Collateral_But_None_Given()
    {
        var (db, branchId, customerId, productId) = await Seed(requiresCollateral: true);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, customerId, productId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanApplication.CollateralRequired");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Product_Requires_CoMaker_But_None_Given()
    {
        var (db, branchId, customerId, productId) = await Seed(requiresCoMaker: true);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, customerId, productId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanApplication.CoMakerRequired");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_CoMaker_Customer_Not_Found()
    {
        var (db, branchId, customerId, productId) = await Seed();
        var handler = BuildHandler(db);
        var coMakers = new List<LoanCoMakerInput> { new(Guid.NewGuid(), "Ghost", null) };

        var result = await handler.HandleAsync(Command(branchId, customerId, productId, coMakers: coMakers), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Customer.NotFound");
        await db.DisposeAsync();
    }
}
