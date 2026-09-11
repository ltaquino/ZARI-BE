using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Loan.LoanProducts.Create;
using ZARI.Application.Features.Loan.LoanProducts.Delete;
using ZARI.Application.Features.Loan.LoanProducts.Get;
using ZARI.Application.Features.Loan.LoanProducts.GetAll;
using ZARI.Application.Features.Loan.LoanProducts.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class LoanProductEndpoints
{
    public static void MapLoanProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/loan-products")
            .WithTags("LoanProducts")
            .WithGroupName("Loan")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllLoanProducts")
            .WithSummary("Get all loan products");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetLoanProductById")
            .WithSummary("Get a loan product by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateLoanProductCommand>>()
            .WithName("CreateLoanProduct")
            .WithSummary("Create a new loan product");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateLoanProduct")
            .WithSummary("Update an existing loan product");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteLoanProduct")
            .WithSummary("Delete a loan product");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllLoanProductsQuery, Result<List<LoanProductResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllLoanProductsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetLoanProductQuery, Result<LoanProductResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLoanProductQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateLoanProductCommand command,
        ICommandHandler<CreateLoanProductCommand, Result<LoanProductResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetLoanProductById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateLoanProductRequest request,
        IValidator<UpdateLoanProductCommand> validator,
        ICommandHandler<UpdateLoanProductCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLoanProductCommand(
            id, request.Code, request.Name, request.InterestMethod, request.AnnualInterestRatePct, request.MinPrincipal, request.MaxPrincipal,
            request.MinTermMonths, request.MaxTermMonths, request.RepaymentFrequency, request.GracePeriodDays, request.PenaltyRatePct,
            request.RequiresCollateral, request.RequiresCoMaker, request.LoanReceivableAccountId, request.InterestIncomeAccountId,
            request.PenaltyIncomeAccountId, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteLoanProductCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteLoanProductCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateLoanProductRequest(
    string Code,
    string Name,
    string InterestMethod,
    decimal AnnualInterestRatePct,
    decimal MinPrincipal,
    decimal MaxPrincipal,
    int MinTermMonths,
    int MaxTermMonths,
    string RepaymentFrequency,
    int GracePeriodDays,
    decimal PenaltyRatePct,
    bool RequiresCollateral,
    bool RequiresCoMaker,
    Guid? LoanReceivableAccountId,
    Guid? InterestIncomeAccountId,
    Guid? PenaltyIncomeAccountId,
    string Status);
