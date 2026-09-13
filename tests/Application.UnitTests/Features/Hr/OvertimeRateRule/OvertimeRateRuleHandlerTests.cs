namespace ZARI.Application.UnitTests.Features.Hr.OvertimeRateRule;

using ZARI.Application.Abstractions.Identity;
using ZARI.Application.Features.Hr.OvertimeRateRules.Create;
using ZARI.Application.Features.Hr.OvertimeRateRules.Delete;
using ZARI.Application.Features.Hr.OvertimeRateRules.Get;
using ZARI.Application.Features.Hr.OvertimeRateRules.GetAll;
using ZARI.Application.Features.Hr.OvertimeRateRules.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class CreateOvertimeRateRuleCommandHandlerTests
{
    private static readonly DateTimeOffset Effective = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static CreateOvertimeRateRuleCommand Command(string otType = "ORDINARY") => new(otType, 1.25m, Effective, "active");

    [Fact]
    public async Task HandleAsync_Should_Create_Rule()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateOvertimeRateRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OtType.Should().Be("ORDINARY");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Forbidden()
    {
        await using var db = TestDbContextFactory.Create();
        var permissions = LoanTestFixtures.AllowAllPermissionService();
        permissions.HasPermissionAsync("OVERTIME_RATE_RULES", FormAction.Create, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateOvertimeRateRuleCommandHandler(db, permissions);

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_Duplicate()
    {
        await using var db = TestDbContextFactory.Create();
        db.OvertimeRateRules.Add(HrTestFixtures.OvertimeRateRule(otType: "ORDINARY", effectiveDate: Effective));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new CreateOvertimeRateRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(Command(), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }
}

public sealed class UpdateOvertimeRateRuleCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Update_Rule()
    {
        await using var db = TestDbContextFactory.Create();
        var rule = HrTestFixtures.OvertimeRateRule();
        db.OvertimeRateRules.Add(rule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new UpdateOvertimeRateRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateOvertimeRateRuleCommand(rule.Id, "HOLIDAY", 2m, rule.EffectiveDate, "active"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.OvertimeRateRules.FindAsync([rule.Id], TestContext.Current.CancellationToken);
        updated!.OtType.Should().Be("HOLIDAY");
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new UpdateOvertimeRateRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new UpdateOvertimeRateRuleCommand(Guid.NewGuid(), "ORDINARY", 1.25m, DateTimeOffset.UtcNow, "active"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class DeleteOvertimeRateRuleCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Delete_Rule()
    {
        await using var db = TestDbContextFactory.Create();
        var rule = HrTestFixtures.OvertimeRateRule();
        db.OvertimeRateRules.Add(rule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new DeleteOvertimeRateRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteOvertimeRateRuleCommand(rule.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new DeleteOvertimeRateRuleCommandHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new DeleteOvertimeRateRuleCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetOvertimeRateRuleQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Rule()
    {
        await using var db = TestDbContextFactory.Create();
        var rule = HrTestFixtures.OvertimeRateRule();
        db.OvertimeRateRules.Add(rule);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetOvertimeRateRuleQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetOvertimeRateRuleQuery(rule.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OtType.Should().Be(rule.OtType);
    }

    [Fact]
    public async Task HandleAsync_Should_Fail_When_NotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetOvertimeRateRuleQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetOvertimeRateRuleQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}

public sealed class GetAllOvertimeRateRulesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_All_Rules()
    {
        await using var db = TestDbContextFactory.Create();
        db.OvertimeRateRules.Add(HrTestFixtures.OvertimeRateRule(otType: "ORDINARY"));
        db.OvertimeRateRules.Add(HrTestFixtures.OvertimeRateRule(otType: "HOLIDAY"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new GetAllOvertimeRateRulesQueryHandler(db, LoanTestFixtures.AllowAllPermissionService());

        var result = await handler.HandleAsync(new GetAllOvertimeRateRulesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
