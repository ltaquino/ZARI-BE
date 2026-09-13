namespace ZARI.Application.UnitTests.Features.Purchasing.OutgoingPayment;

using ZARI.Application.Features.Purchasing.OutgoingPayments.Delete;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeleteOutgoingPaymentCommandHandlerTests
{
    private static DeleteOutgoingPaymentCommandHandler Handler(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Application.Abstractions.Identity.IPermissionService? permissions = null) =>
        new(db, permissions ?? LoanTestFixtures.AllowAllPermissionService());

    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, ZARI.Domain.Entities.OutgoingPayment payment)> Seed(string status = "DRAFT")
    {
        var db = TestDbContextFactory.Create();
        var branch = LoanTestFixtures.Branch();
        db.Branches.Add(branch);
        var supplier = PurchasingTestFixtures.Supplier();
        db.Suppliers.Add(supplier);
        var bankGlAccount = LoanTestFixtures.GlAccount(code: "1010");
        db.GlAccounts.Add(bankGlAccount);
        var uom = InventoryTestFixtures.Uom();
        db.Uoms.Add(uom);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var item = InventoryTestFixtures.Item(uom.Id);
        db.Items.Add(item);
        var bankAccount = AccountingTestFixtures.BankAccount(branch.Id, bankGlAccount.Id);
        db.BankAccounts.Add(bankAccount);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var invoice = PurchasingTestFixtures.ApInvoice(branch.Id, supplier.Id, item.Id, uom.Id);
        db.ApInvoices.Add(invoice);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var payment = PurchasingTestFixtures.OutgoingPayment(branch.Id, supplier.Id, bankAccount.Id, invoice.Id, status: status);
        db.OutgoingPayments.Add(payment);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, payment);
    }

    [Fact]
    public async Task HandleAsync_Should_Delete_Draft_Payment()
    {
        var (db, payment) = await Seed();

        var result = await Handler(db).HandleAsync(new DeleteOutgoingPaymentCommand(payment.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        db.OutgoingPayments.Should().BeEmpty();
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var result = await Handler(db).HandleAsync(new DeleteOutgoingPaymentCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        var (db, payment) = await Seed();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionOnBranchAsync("OUTGOING_PAYMENTS", FormAction.Delete, payment.BranchId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Handler(db, permissions).HandleAsync(new DeleteOutgoingPaymentCommand(payment.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Not_Draft()
    {
        var (db, payment) = await Seed(status: "POSTED");

        var result = await Handler(db).HandleAsync(new DeleteOutgoingPaymentCommand(payment.Id), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("OutgoingPayment.NotDraft");
        await db.DisposeAsync();
    }
}
