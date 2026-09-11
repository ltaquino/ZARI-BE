namespace ZARI.Application.UnitTests.Features.Loan.LoanAccount;

using ZARI.Application.Features.Loan.LoanAccounts.Create;
using ZARI.Application.Features.SystemModule.DocumentSequences.GetNext;
using ZARI.Application.Features.Workflow.Notifications.Create;
using ZARI.Application.Features.Workflow.Notifications.GetAll;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreateLoanAccountCommandHandlerTests
{
    private static CreateLoanAccountCommandHandler BuildHandler(ZARI.Infrastructure.Persistence.AppDbContext dbContext, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(dbContext,
            LoanTestFixtures.SuccessHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>>(Result.Success(new NextDocumentNumberResponse("LOAN-ACCT-0001"))),
            LoanTestFixtures.SuccessHandler<CreateNotificationCommand, Result<NotificationResponse>>(Result.Success(new NotificationResponse(Guid.NewGuid(), "LOAN_ACCOUNT", "x", "br-1", "CREATED", "ACTIVITY", "msg", null, [], DateTimeOffset.UtcNow))),
            permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, string branchId, Guid customerId, Guid productId)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        var customer = LoanTestFixtures.Customer(branch.Id);
        var product = LoanTestFixtures.LoanProduct();
        db.Branches.Add(branch);
        db.Customers.Add(customer);
        db.LoanProducts.Add(product);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, branch.Id, customer.Id, product.Id);
    }

    private static CreateLoanAccountCommand Command(string branchId, Guid customerId, Guid productId, Guid? applicationId = null, decimal principal = 12000, int term = 6) =>
        new(branchId, customerId, productId, applicationId, principal, term, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMonths(1), null, null, null, null, "tester");

    [Fact]
    public async Task HandleAsync_Should_Create_Account_With_Schedule()
    {
        var (db, branchId, customerId, productId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, customerId, productId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("PENDING_DISBURSEMENT");
        result.Value.ScheduleLines.Should().HaveCount(6);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, branchId, customerId, productId) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("LOAN_ACCOUNTS", FormAction.Create, branchId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = BuildHandler(db, permissions);

        var result = await handler.HandleAsync(Command(branchId, customerId, productId), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Principal_Out_Of_Range()
    {
        var (db, branchId, customerId, productId) = await Seed();
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, customerId, productId, principal: 999999), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanAccount.PrincipalOutOfRange");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Application_Not_Approved()
    {
        var (db, branchId, customerId, productId) = await Seed();
        var application = new LoanApplication { ApplicationNo = "A1", BranchId = branchId, CustomerId = customerId, LoanProductId = productId, ApplicationDate = DateTimeOffset.UtcNow, RequestedPrincipal = 12000, RequestedTermMonths = 6, Status = "DRAFT" };
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, customerId, productId, applicationId: application.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanAccount.ApplicationNotApproved");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Application_Already_Converted()
    {
        var (db, branchId, customerId, productId) = await Seed();
        var application = new LoanApplication { ApplicationNo = "A1", BranchId = branchId, CustomerId = customerId, LoanProductId = productId, ApplicationDate = DateTimeOffset.UtcNow, RequestedPrincipal = 12000, RequestedTermMonths = 6, Status = "APPROVED" };
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var existing = LoanTestFixtures.LoanAccount(branchId, customerId, productId, withSchedule: false);
        existing.LoanApplicationId = application.Id;
        existing.Status = "ACTIVE";
        db.LoanAccounts.Add(existing);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = BuildHandler(db);

        var result = await handler.HandleAsync(Command(branchId, customerId, productId, applicationId: application.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LoanAccount.ApplicationAlreadyConverted");
        await db.DisposeAsync();
    }
}
