namespace ZARI.Application.UnitTests.Features.Sales.PaymentMethod;

using ZARI.Application.Features.Sales.PaymentMethods.Create;
using ZARI.Application.Features.Sales.PaymentMethods.Delete;
using ZARI.Application.Features.Sales.PaymentMethods.Get;
using ZARI.Application.Features.Sales.PaymentMethods.GetAll;
using ZARI.Application.Features.Sales.PaymentMethods.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

public sealed class CreatePaymentMethodCommandHandlerTests
{
    private static CreatePaymentMethodCommand Command(Guid glAccountId, string code = "CASH") =>
        new(code, "Cash", glAccountId, false, null, false, 1, "active");

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GlAccount glAccount)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var glAccount = LoanTestFixtures.GlAccount(code: "1000");
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, glAccount);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Payment_Method()
    {
        var (db, glAccount) = await Seed();
        var handler = new CreatePaymentMethodCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(glAccount.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("CASH");
        result.Value!.GlAccountCode.Should().Be("1000");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, glAccount) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("PAYMENT_METHODS", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreatePaymentMethodCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(glAccount.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Already_Exists()
    {
        var (db, glAccount) = await Seed();
        db.PaymentMethods.Add(LoanTestFixtures.PaymentMethod(glAccount.Id, code: "CASH"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreatePaymentMethodCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(glAccount.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Gl_Account_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreatePaymentMethodCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Clear_ReferenceNoLabel_When_Not_Required()
    {
        var (db, glAccount) = await Seed();
        var handler = new CreatePaymentMethodCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());
        var command = new CreatePaymentMethodCommand("GCASH", "GCash", glAccount.Id, false, "Should be dropped", false, 2, "active");

        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        result.Value!.ReferenceNoLabel.Should().BeNull();
        await db.DisposeAsync();
    }
}

public sealed class UpdatePaymentMethodCommandHandlerTests
{
    private static UpdatePaymentMethodCommand Command(Guid id, Guid glAccountId, string code = "CASH") =>
        new(id, code, "Cash Updated", glAccountId, false, null, false, 1, "inactive");

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, GlAccount glAccount, PaymentMethod method)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var glAccount = LoanTestFixtures.GlAccount(code: "1000");
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var method = LoanTestFixtures.PaymentMethod(glAccount.Id, code: "CASH");
        db.PaymentMethods.Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, glAccount, method);
    }

    [Fact]
    public async Task HandleAsync_Should_Update_Payment_Method()
    {
        var (db, glAccount, method) = await Seed();
        var handler = new UpdatePaymentMethodCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(method.Id, glAccount.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        (await db.PaymentMethods.FindAsync([method.Id], TestContext.Current.CancellationToken))!.Status.Should().Be("inactive");
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdatePaymentMethodCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(Guid.NewGuid(), Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, glAccount, method) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("PAYMENT_METHODS", FormAction.Edit, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UpdatePaymentMethodCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(method.Id, glAccount.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Code_Conflicts()
    {
        var (db, glAccount, method) = await Seed();
        db.PaymentMethods.Add(LoanTestFixtures.PaymentMethod(glAccount.Id, code: "GCASH"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdatePaymentMethodCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(method.Id, glAccount.Id, code: "GCASH"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Gl_Account_Not_Found()
    {
        var (db, _, method) = await Seed();
        var handler = new UpdatePaymentMethodCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(method.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }
}

public sealed class DeletePaymentMethodCommandHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, PaymentMethod method)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var glAccount = LoanTestFixtures.GlAccount(code: "1000");
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var method = LoanTestFixtures.PaymentMethod(glAccount.Id, code: "CASH");
        db.PaymentMethods.Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, method);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Payment_Method()
    {
        var (db, method) = await Seed();
        var handler = new DeletePaymentMethodCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePaymentMethodCommand(method.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.PaymentMethods.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeletePaymentMethodCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePaymentMethodCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, method) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("PAYMENT_METHODS", FormAction.Delete, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new DeletePaymentMethodCommandHandler(db, permissions);

        var result = await handler.HandleAsync(new DeletePaymentMethodCommand(method.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_In_Use_By_A_Customer_Payment_Tender()
    {
        var (db, method) = await Seed();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var customer = LoanTestFixtures.Customer(branch.Id);
        db.Customers.Add(customer);
        var cashAccount = LoanTestFixtures.GlAccount(code: "1001");
        db.GlAccounts.Add(cashAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = new CustomerPayment
        {
            PaymentNo = $"CRV-{Guid.NewGuid():N}", BranchId = branch.Id, CustomerId = customer.Id,
            PaymentMethod = "CASH", CashAccountId = cashAccount.Id, PaymentDate = DateTimeOffset.UtcNow, Status = "DRAFT",
            Tenders = [new CustomerPaymentTender { PaymentMethodId = method.Id, Amount = 100 }]
        };
        db.CustomerPayments.Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeletePaymentMethodCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeletePaymentMethodCommand(method.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
        await db.DisposeAsync();
    }
}

public sealed class GetPaymentMethodQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Method_When_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var glAccount = LoanTestFixtures.GlAccount(code: "1000");
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var method = LoanTestFixtures.PaymentMethod(glAccount.Id, code: "CASH");
        db.PaymentMethods.Add(method);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetPaymentMethodQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPaymentMethodQuery(method.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("CASH");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetPaymentMethodQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetPaymentMethodQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("PAYMENT_METHODS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetPaymentMethodQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetPaymentMethodQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}

public sealed class GetAllPaymentMethodsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Methods_Ordered_By_DisplayOrder()
    {
        await using var db = TestDbContextFactory.Create();
        var glAccount = LoanTestFixtures.GlAccount(code: "1000");
        db.GlAccounts.Add(glAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var second = LoanTestFixtures.PaymentMethod(glAccount.Id, code: "GCASH");
        second.DisplayOrder = 2;
        var first = LoanTestFixtures.PaymentMethod(glAccount.Id, code: "CASH");
        first.DisplayOrder = 1;
        db.PaymentMethods.AddRange(second, first);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllPaymentMethodsQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllPaymentMethodsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Code.Should().Be("CASH");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("PAYMENT_METHODS", FormAction.View, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new GetAllPaymentMethodsQueryHandler(db, permissions);

        var result = await handler.HandleAsync(new GetAllPaymentMethodsQuery(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }
}
