using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosSale;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class PosSaleEndpoints
{
    public static void MapPosSaleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pos")
            .WithTags("PosSale")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapPost("/checkout", Checkout)
            .AddEndpointFilter<ValidationFilter<CreatePosSaleCommand>>()
            .WithName("CheckoutPosSale")
            .WithSummary("Complete a POS sale — creates and immediately posts a Sales Invoice, then a fully-settling Customer Payment");
    }

    private static async Task<IResult> Checkout(
        CreatePosSaleCommand command,
        ICommandHandler<CreatePosSaleCommand, Result<PosSaleResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
