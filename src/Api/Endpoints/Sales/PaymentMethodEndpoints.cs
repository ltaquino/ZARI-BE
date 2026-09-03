using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PaymentMethods.Create;
using ZARI.Application.Features.Sales.PaymentMethods.Delete;
using ZARI.Application.Features.Sales.PaymentMethods.Get;
using ZARI.Application.Features.Sales.PaymentMethods.GetAll;
using ZARI.Application.Features.Sales.PaymentMethods.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class PaymentMethodEndpoints
{
    public static void MapPaymentMethodEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payment-methods")
            .WithTags("PaymentMethods")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllPaymentMethods")
            .WithSummary("Get all payment methods");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetPaymentMethodById")
            .WithSummary("Get a payment method by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreatePaymentMethodCommand>>()
            .WithName("CreatePaymentMethod")
            .WithSummary("Create a new payment method");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdatePaymentMethod")
            .WithSummary("Update an existing payment method");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeletePaymentMethod")
            .WithSummary("Delete a payment method");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllPaymentMethodsQuery, Result<List<PaymentMethodResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllPaymentMethodsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetPaymentMethodQuery, Result<PaymentMethodResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPaymentMethodQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreatePaymentMethodCommand command,
        ICommandHandler<CreatePaymentMethodCommand, Result<PaymentMethodResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetPaymentMethodById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdatePaymentMethodRequest request,
        IValidator<UpdatePaymentMethodCommand> validator,
        ICommandHandler<UpdatePaymentMethodCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePaymentMethodCommand(id, request.Code, request.Name, request.GlAccountId, request.RequiresReferenceNo, request.ReferenceNoLabel, request.RequiresBankOrPartnerName, request.DisplayOrder, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeletePaymentMethodCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeletePaymentMethodCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdatePaymentMethodRequest(string Code, string Name, Guid GlAccountId, bool RequiresReferenceNo, string? ReferenceNoLabel, bool RequiresBankOrPartnerName, int DisplayOrder, string Status);
