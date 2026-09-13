namespace ZARI.Application.UnitTests.TestSupport;

using NSubstitute;
using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;
using ZARI.Domain.Entities;

/// <summary>
/// Shared entity builders + fakes for every Loan/Customer handler test. Handlers are tested against
/// a real (InMemory-provider) AppDbContext via TestDbContextFactory — not a mocked IAppDbContext —
/// so their actual LINQ queries execute for real; only cross-cutting dependencies (permission
/// checks, other command handlers reached via DI) are faked with NSubstitute. One handler,
/// PostLoanLedgerEntryCommandHandler, uses raw MySQL `FOR UPDATE` SQL and a real execution-strategy
/// transaction that the InMemory provider can't run — its tests cover only the code paths that
/// execute before that point (not-found, idempotency), documented on that test file directly.
///
/// SEEDING GOTCHA: EF Core generates Guid PKs client-side the moment an entity is passed to
/// `DbSet.Add(...)`, not at SaveChangesAsync. So when building a child row that stores a parent's
/// Guid FK (e.g. `CustomerId = customer.Id`), always call `db.Customers.Add(customer)` (etc.)
/// *before* reading `customer.Id` — reading it earlier captures Guid.Empty. A stale/empty FK like
/// this won't fail the parent-row lookup itself, but any handler that does `.Include(a => a.Customer)`
/// on a *required* navigation will silently return zero rows (InMemory applies Include as an
/// inner-join-like filter), which is a confusing failure to debug from the assertion alone.
///
/// EXECUTEUPDATE GOTCHA: several Approve/ApproveCancellation handlers across LoanDisbursement,
/// LoanPayment, LoanRestructuring and LoanWriteOff use `ExecuteUpdateAsync` to flip status after a
/// PostLoanLedgerEntryCommand call clears the ChangeTracker — EF Core's InMemory provider does not
/// support ExecuteUpdate/ExecuteUpdateAsync at all (throws InvalidOperationException), so those
/// handlers' success paths can't be unit-tested here. Only test the guard clauses before that
/// point (not-found, forbidden, wrong status, missing GL account, missing approval request) and
/// document the gap on the test file, same as PostLoanLedgerEntryCommandHandlerTests.
/// </summary>
internal static class LoanTestFixtures
{
    public static Branch Branch(string id = "br-1", bool isHeadOffice = false) => new()
    {
        Id = id,
        Name = "Test Branch",
        Code = id.ToUpperInvariant(),
        City = "Test City",
        Address = "123 Test St",
        Phone = "000-0000",
        Status = "active",
        IsHeadOffice = isHeadOffice
    };

    public static GlAccount GlAccount(string code = "1300", string name = "Test GL Account", string accountType = "Asset", string normalBalance = "Debit") => new()
    {
        Code = code,
        Name = name,
        AccountType = accountType,
        NormalBalance = normalBalance,
        Status = "active"
    };

    public static PaymentMethod PaymentMethod(Guid glAccountId, string code = "CASH", string name = "Cash") => new()
    {
        Code = code,
        Name = name,
        GlAccountId = glAccountId,
        Status = "active",
        DisplayOrder = 1
    };

    public static CostCenter CostCenter(string? branchId = null, string code = "CC1") => new()
    {
        BranchId = branchId,
        Code = code,
        Name = "Test Cost Center",
        Status = "active"
    };

    public static Customer Customer(string branchId = "br-1", string name = "Juan Dela Cruz") => new()
    {
        Name = name,
        Type = "individual",
        Email = "customer@example.com",
        Phone = "0917-000-0000",
        BranchId = branchId,
        Status = "active",
        Owner = "tester",
        Address = "Test Address"
    };

    public static LoanProduct LoanProduct(
        Guid? loanReceivableAccountId = null,
        Guid? interestIncomeAccountId = null,
        Guid? penaltyIncomeAccountId = null,
        decimal annualInterestRatePct = 12,
        decimal minPrincipal = 1000,
        decimal maxPrincipal = 100000,
        int minTermMonths = 1,
        int maxTermMonths = 24,
        string repaymentFrequency = "MONTHLY",
        int gracePeriodDays = 5,
        decimal penaltyRatePct = 2,
        bool requiresCollateral = false,
        bool requiresCoMaker = false,
        string code = "PROD-1",
        string status = "active") => new()
    {
        Code = code,
        Name = "Test Loan Product",
        InterestMethod = "DIMINISHING",
        AnnualInterestRatePct = annualInterestRatePct,
        MinPrincipal = minPrincipal,
        MaxPrincipal = maxPrincipal,
        MinTermMonths = minTermMonths,
        MaxTermMonths = maxTermMonths,
        RepaymentFrequency = repaymentFrequency,
        GracePeriodDays = gracePeriodDays,
        PenaltyRatePct = penaltyRatePct,
        RequiresCollateral = requiresCollateral,
        RequiresCoMaker = requiresCoMaker,
        LoanReceivableAccountId = loanReceivableAccountId,
        InterestIncomeAccountId = interestIncomeAccountId,
        PenaltyIncomeAccountId = penaltyIncomeAccountId,
        Status = status
    };

    /// <summary>Builds a LoanAccount with a simple flat-principal schedule (not a real diminishing-balance amortization — good enough for handler tests that don't assert exact schedule math; use AmortizationScheduleGenerator directly for that).</summary>
    public static LoanAccount LoanAccount(
        string branchId,
        Guid customerId,
        Guid loanProductId,
        decimal principalAmount = 12000,
        int termMonths = 6,
        string repaymentFrequency = "MONTHLY",
        decimal annualInterestRatePct = 12,
        decimal penaltyRatePct = 2,
        int gracePeriodDays = 5,
        string status = "ACTIVE",
        Guid? loanReceivableAccountId = null,
        Guid? interestIncomeAccountId = null,
        Guid? penaltyIncomeAccountId = null,
        DateTimeOffset? grantDate = null,
        DateTimeOffset? firstDueDate = null,
        bool withSchedule = true)
    {
        var grant = grantDate ?? DateTimeOffset.UtcNow;
        var firstDue = firstDueDate ?? grant.AddMonths(1);

        var account = new LoanAccount
        {
            LoanAcctNo = $"LOAN-ACCT-{Guid.NewGuid():N}",
            BranchId = branchId,
            CustomerId = customerId,
            LoanProductId = loanProductId,
            PrincipalAmount = principalAmount,
            AnnualInterestRatePct = annualInterestRatePct,
            TermMonths = termMonths,
            RepaymentFrequency = repaymentFrequency,
            GracePeriodDays = gracePeriodDays,
            PenaltyRatePct = penaltyRatePct,
            GrantDate = grant,
            FirstDueDate = firstDue,
            Status = status,
            LoanReceivableAccountId = loanReceivableAccountId,
            InterestIncomeAccountId = interestIncomeAccountId,
            PenaltyIncomeAccountId = penaltyIncomeAccountId
        };

        if (withSchedule)
        {
            var installmentPrincipal = Math.Round(principalAmount / termMonths, 2);
            var balance = principalAmount;
            for (var i = 1; i <= termMonths; i++)
            {
                var principalDue = i == termMonths ? balance : installmentPrincipal;
                var interestDue = Math.Round(balance * (annualInterestRatePct / 100m / 12m), 2);
                balance = Math.Round(balance - principalDue, 2);
                account.ScheduleLines.Add(new LoanAmortizationScheduleLine
                {
                    InstallmentNo = i,
                    DueDate = firstDue.AddMonths(i - 1),
                    PrincipalDue = principalDue,
                    InterestDue = interestDue,
                    TotalDue = principalDue + interestDue,
                    OutstandingPrincipalAfter = balance,
                    PrincipalPaid = 0,
                    InterestPaid = 0,
                    PenaltyPaid = 0,
                    Status = "DUE"
                });
            }
        }

        return account;
    }

    /// <summary>A permission service that allows every check — tests override individual calls with `.Returns(false)` to exercise the forbidden path.</summary>
    public static IPermissionService AllowAllPermissionService()
    {
        var service = Substitute.For<IPermissionService>();
        service.HasPermissionAsync(Arg.Any<string>(), Arg.Any<FormAction>(), Arg.Any<CancellationToken>()).Returns(true);
        service.HasPermissionOnBranchAsync(Arg.Any<string>(), Arg.Any<FormAction>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        service.HasCancellationAuthorityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        service.HasHqApprovalAuthorityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        return service;
    }

    /// <summary>A fake for an injected `ICommandHandler&lt;TCommand, TResponse&gt;` dependency that always succeeds with the given response.</summary>
    public static ICommandHandler<TCommand, TResponse> SuccessHandler<TCommand, TResponse>(TResponse response)
        where TCommand : ICommand<TResponse>
    {
        var handler = Substitute.For<ICommandHandler<TCommand, TResponse>>();
        handler.HandleAsync(Arg.Any<TCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(response));
        return handler;
    }

    /// <summary>A fake for an injected `ICommandHandler&lt;TCommand, TResponse&gt;` dependency that always fails with the given error.</summary>
    public static ICommandHandler<TCommand, TResponse> FailingHandler<TCommand, TResponse>(TResponse failureResponse)
        where TCommand : ICommand<TResponse>
    {
        var handler = Substitute.For<ICommandHandler<TCommand, TResponse>>();
        handler.HandleAsync(Arg.Any<TCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(failureResponse));
        return handler;
    }
}
