using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.System.DocumentSequences.Create;
using ZARI.Application.Features.System.DocumentSequences.Delete;
using ZARI.Application.Features.System.DocumentSequences.Get;
using ZARI.Application.Features.System.DocumentSequences.GetAll;
using ZARI.Application.Features.System.DocumentSequences.GetNext;
using ZARI.Application.Features.System.DocumentSequences.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class DocumentSequenceEndpoints
{
    public static void MapDocumentSequenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/document-sequences")
            .WithTags("DocumentSequences")
            .WithGroupName("System")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllDocumentSequences")
            .WithSummary("Get all document numbering sequences");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetDocumentSequenceById")
            .WithSummary("Get a document numbering sequence by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateDocumentSequenceCommand>>()
            .WithName("CreateDocumentSequence")
            .WithSummary("Create a new document numbering sequence");

        group.MapPut("/{id:guid}", Update)
            .AddEndpointFilter<ValidationFilter<UpdateDocumentSequenceCommand>>()
            .WithName("UpdateDocumentSequence")
            .WithSummary("Update an existing document numbering sequence");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteDocumentSequence")
            .WithSummary("Delete a document numbering sequence");

        group.MapPost("/next", GetNext)
            .WithName("GetNextDocumentNumber")
            .WithSummary("Atomically reserve and format the next document number for a branch/document type");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllDocumentSequencesQuery, Result<List<DocumentSequenceResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllDocumentSequencesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetDocumentSequenceQuery, Result<DocumentSequenceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDocumentSequenceQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateDocumentSequenceCommand command,
        ICommandHandler<CreateDocumentSequenceCommand, Result<DocumentSequenceResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetDocumentSequenceById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateDocumentSequenceRequest request,
        ICommandHandler<UpdateDocumentSequenceCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDocumentSequenceCommand(id, request.BranchId, request.DocType, request.Prefix, request.NextNumber, request.PaddingLength);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteDocumentSequenceCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteDocumentSequenceCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> GetNext(
        GetNextDocumentNumberCommand command,
        ICommandHandler<GetNextDocumentNumberCommand, Result<NextDocumentNumberResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

public sealed record UpdateDocumentSequenceRequest(string BranchId, string DocType, string Prefix, int NextNumber, int PaddingLength);
