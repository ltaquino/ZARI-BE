using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.OvertimeRateRules.Create;
using ZARI.Application.Features.Hr.OvertimeRateRules.Delete;
using ZARI.Application.Features.Hr.OvertimeRateRules.Get;
using ZARI.Application.Features.Hr.OvertimeRateRules.GetAll;
using ZARI.Application.Features.Hr.OvertimeRateRules.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class OvertimeRateRuleEndpoints
{
    public static void MapOvertimeRateRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/overtime-rate-rules")
            .WithTags("OvertimeRateRules")
            .WithGroupName("HR")
            .RequireAuthorization();

        group.MapGet("/", GetAll).WithName("GetAllOvertimeRateRules").WithSummary("Get all overtime rate rules");
        group.MapGet("/{id:guid}", GetById).WithName("GetOvertimeRateRuleById").WithSummary("Get an overtime rate rule by ID");
        group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateOvertimeRateRuleCommand>>().WithName("CreateOvertimeRateRule").WithSummary("Create a new overtime rate rule");
        group.MapPut("/{id:guid}", Update).WithName("UpdateOvertimeRateRule").WithSummary("Update an existing overtime rate rule");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteOvertimeRateRule").WithSummary("Delete an overtime rate rule");
    }

    private static async Task<IResult> GetAll(IQueryHandler<GetAllOvertimeRateRulesQuery, Result<List<OvertimeRateRuleResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllOvertimeRateRulesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(Guid id, IQueryHandler<GetOvertimeRateRuleQuery, Result<OvertimeRateRuleResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetOvertimeRateRuleQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(CreateOvertimeRateRuleCommand command, ICommandHandler<CreateOvertimeRateRuleCommand, Result<OvertimeRateRuleResponse>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.CreatedAtRoute(result.Value, "GetOvertimeRateRuleById", new { id = result.Value!.Id }) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(Guid id, UpdateOvertimeRateRuleRequest request, IValidator<UpdateOvertimeRateRuleCommand> validator, ICommandHandler<UpdateOvertimeRateRuleCommand, Result> handler, CancellationToken cancellationToken)
    {
        var command = new UpdateOvertimeRateRuleCommand(id, request.OtType, request.Multiplier, request.EffectiveDate, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(Guid id, ICommandHandler<DeleteOvertimeRateRuleCommand, Result> handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteOvertimeRateRuleCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record UpdateOvertimeRateRuleRequest(string OtType, decimal Multiplier, DateTimeOffset EffectiveDate, string Status);
