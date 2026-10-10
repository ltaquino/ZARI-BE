namespace ZARI.Application.UnitTests.Features.RouteSales;

using ZARI.Application.Features.RouteSales.DeliveryRoutes.Create;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Get;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Shared;
using ZARI.Application.Features.RouteSales.DeliveryRoutes.Update;
using ZARI.Application.UnitTests.TestSupport;
using ZARI.Domain.Common;

public sealed class DeliveryRouteHandlerTests
{
    private static async Task<(ZARI.Infrastructure.Persistence.AppDbContext db, Guid c1, Guid c2, Guid c3)> Seed()
    {
        var db = TestDbContextFactory.Create();
        db.Branches.Add(LoanTestFixtures.Branch());
        var c1 = LoanTestFixtures.Customer(name: "Alpha Store");
        var c2 = LoanTestFixtures.Customer(name: "Bravo Mart");
        var c3 = LoanTestFixtures.Customer(name: "Charlie Sari-sari");
        db.Customers.AddRange(c1, c2, c3);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (db, c1.Id, c2.Id, c3.Id);
    }

    [Fact]
    public async Task Create_Should_Save_Stops_In_The_Given_Visiting_Order()
    {
        var (db, c1, c2, c3) = await Seed();

        var result = await new CreateDeliveryRouteCommandHandler(db, LoanTestFixtures.AllowAllPermissionService()).HandleAsync(
            new CreateDeliveryRouteCommand("br-1", "R-NORTH", "North loop", null, null, "active",
                [new(c3, null), new(c1, "Back gate"), new(c2, null)]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Stops.Select(s => s.CustomerName).Should().Equal("Charlie Sari-sari", "Alpha Store", "Bravo Mart");
        result.Value.Stops.Select(s => s.Sequence).Should().Equal(1, 2, 3);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Create_Should_Fail_When_A_Customer_Does_Not_Exist()
    {
        var (db, c1, _, _) = await Seed();

        var result = await new CreateDeliveryRouteCommandHandler(db, LoanTestFixtures.AllowAllPermissionService()).HandleAsync(
            new CreateDeliveryRouteCommand("br-1", "R1", "Route", null, null, "active", [new(c1, null), new(Guid.NewGuid(), null)]),
            TestContext.Current.CancellationToken);

        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await db.DisposeAsync();
    }

    [Fact]
    public void Validator_Should_Reject_The_Same_Customer_Twice()
    {
        var id = Guid.NewGuid();
        var result = new CreateDeliveryRouteValidator().Validate(
            new CreateDeliveryRouteCommand("br-1", "R1", "Route", null, null, "active", [new(id, null), new(id, null)]));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Update_Should_Replace_The_Stops()
    {
        var (db, c1, c2, c3) = await Seed();
        var created = await new CreateDeliveryRouteCommandHandler(db, LoanTestFixtures.AllowAllPermissionService()).HandleAsync(
            new CreateDeliveryRouteCommand("br-1", "R1", "Route", null, null, "active", [new(c1, null), new(c2, null)]),
            TestContext.Current.CancellationToken);

        var result = await new UpdateDeliveryRouteCommandHandler(db, LoanTestFixtures.AllowAllPermissionService()).HandleAsync(
            new UpdateDeliveryRouteCommand(created.Value!.Id, "R1", "Route", null, null, "active", [new(c2, null), new(c3, null)]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await new GetDeliveryRouteQueryHandler(db, LoanTestFixtures.AllowAllPermissionService())
            .HandleAsync(new GetDeliveryRouteQuery(created.Value.Id), TestContext.Current.CancellationToken);
        reloaded.Value!.Stops.Select(s => s.CustomerId).Should().Equal(c2, c3);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Create_Should_Reject_A_Default_Vehicle_From_Another_Branch()
    {
        var (db, c1, _, _) = await Seed();

        var result = await new CreateDeliveryRouteCommandHandler(db, LoanTestFixtures.AllowAllPermissionService()).HandleAsync(
            new CreateDeliveryRouteCommand("br-1", "R1", "Route", null, Guid.NewGuid(), "active", [new DeliveryRouteStopInput(c1, null)]),
            TestContext.Current.CancellationToken);

        result.Error!.Code.Should().Be("DeliveryRoute.VehicleNotFound");
        await db.DisposeAsync();
    }
}
