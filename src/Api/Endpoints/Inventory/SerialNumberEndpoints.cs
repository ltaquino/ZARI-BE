using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Inventory.SerialNumbers.GetAll;
using ZARI.Application.Features.Inventory.SerialNumbers.Issue;
using ZARI.Application.Features.Inventory.SerialNumbers.Receive;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseIssue;
using ZARI.Application.Features.Inventory.SerialNumbers.ReverseReceive;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class SerialNumberEndpoints
{
    public static void MapSerialNumberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/serial-numbers")
            .WithTags("SerialNumbers")
            .WithGroupName("Inventory")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllSerialNumbers")
            .WithSummary("Get all serial numbers");

        group.MapPost("/receive", Receive)
            .AddEndpointFilter<ValidationFilter<ReceiveSerialCommand>>()
            .WithName("ReceiveSerial")
            .WithSummary("Mark a serial as on-hand at a warehouse");

        group.MapPost("/issue", Issue)
            .AddEndpointFilter<ValidationFilter<IssueSerialCommand>>()
            .WithName("IssueSerial")
            .WithSummary("Mark a serial as no longer on-hand (IN_TRANSIT or DISPOSED)");

        group.MapPost("/reverse-issue", ReverseIssue)
            .WithName("ReverseIssueSerial")
            .WithSummary("Undo an issue, restoring a serial to IN_STOCK");

        group.MapPost("/reverse-receive", ReverseReceive)
            .AddEndpointFilter<ValidationFilter<ReverseReceiveSerialCommand>>()
            .WithName("ReverseReceiveSerial")
            .WithSummary("Undo a receipt, either reverting to IN_TRANSIT or removing the record");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllSerialNumbersQuery, Result<List<SerialNumberResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllSerialNumbersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Receive(
        ReceiveSerialCommand command,
        ICommandHandler<ReceiveSerialCommand, Result<SerialNumberResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Issue(
        IssueSerialCommand command,
        ICommandHandler<IssueSerialCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> ReverseIssue(
        ReverseIssueSerialCommand command,
        ICommandHandler<ReverseIssueSerialCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> ReverseReceive(
        ReverseReceiveSerialCommand command,
        ICommandHandler<ReverseReceiveSerialCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
