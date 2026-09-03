using FluentValidation;
using ZARI.Api.Extensions;
using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosPromoSlides.Create;
using ZARI.Application.Features.Sales.PosPromoSlides.Delete;
using ZARI.Application.Features.Sales.PosPromoSlides.Get;
using ZARI.Application.Features.Sales.PosPromoSlides.GetAll;
using ZARI.Application.Features.Sales.PosPromoSlides.Update;
using ZARI.Domain.Common;

namespace ZARI.Api.Endpoints;

public static class PosPromoSlideEndpoints
{
    // File-upload constraints for the promo-slide image endpoint below — small, fixed allowlist;
    // no general-purpose upload feature exists elsewhere in this app, so this stays local to Sales.
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public static void MapPosPromoSlideEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pos-promo-slides")
            .WithTags("PosPromoSlides")
            .WithGroupName("Sales")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllPosPromoSlides")
            .WithSummary("Get all POS promo slides");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetPosPromoSlideById")
            .WithSummary("Get a POS promo slide by ID");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreatePosPromoSlideCommand>>()
            .WithName("CreatePosPromoSlide")
            .WithSummary("Create a new POS promo slide");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdatePosPromoSlide")
            .WithSummary("Update an existing POS promo slide");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeletePosPromoSlide")
            .WithSummary("Delete a POS promo slide");

        group.MapPost("/upload-image", UploadImage)
            .DisableAntiforgery()
            .WithName("UploadPosPromoSlideImage")
            .WithSummary("Upload an image file for a promo slide — returns a URL to store on the slide's ImageUrl field");
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetAllPosPromoSlidesQuery, Result<List<PosPromoSlideResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAllPosPromoSlidesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetPosPromoSlideQuery, Result<PosPromoSlideResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPosPromoSlideQuery(id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreatePosPromoSlideCommand command,
        ICommandHandler<CreatePosPromoSlideCommand, Result<PosPromoSlideResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.CreatedAtRoute(result.Value, "GetPosPromoSlideById", new { id = result.Value!.Id })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdatePosPromoSlideRequest request,
        IValidator<UpdatePosPromoSlideCommand> validator,
        ICommandHandler<UpdatePosPromoSlideCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePosPromoSlideCommand(id, request.Title, request.Subtitle, request.ImageUrl, request.DisplayOrder, request.Status);
        if (await validator.ValidateOrProblemAsync(command) is { } problem) return problem;

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeletePosPromoSlideCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeletePosPromoSlideCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Plain file-system upload — no general asset-storage abstraction exists elsewhere in ZARI, so
    /// this stays a small, local, single-purpose endpoint rather than inventing one. Saves under
    /// wwwroot/uploads/pos-promo-slides/{guid}.{ext}; Program.cs's app.UseStaticFiles() serves it
    /// back at the returned url.
    /// </summary>
    private static async Task<IResult> UploadImage(IFormFile? file, IWebHostEnvironment env)
    {
        if (file is null || file.Length == 0)
            return Results.Problem(title: "Validation Error", detail: "No file was uploaded.", statusCode: StatusCodes.Status400BadRequest);

        if (file.Length > MaxFileSizeBytes)
            return Results.Problem(title: "Validation Error", detail: "The image must be 5MB or smaller.", statusCode: StatusCodes.Status400BadRequest);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return Results.Problem(title: "Validation Error", detail: $"Only {string.Join(", ", AllowedExtensions)} files are allowed.", statusCode: StatusCodes.Status400BadRequest);

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", "pos-promo-slides");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);
        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        return TypedResults.Ok(new { url = $"/uploads/pos-promo-slides/{fileName}" });
    }
}

public sealed record UpdatePosPromoSlideRequest(string Title, string? Subtitle, string? ImageUrl, int DisplayOrder, string Status);
