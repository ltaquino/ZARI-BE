using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using ZARI.Api.Endpoints;
using ZARI.Api.Extensions;
using ZARI.Application;
using ZARI.Application.DTOs.Identity;
using ZARI.Infrastructure;
using ZARI.Infrastructure.Persistence;
using ZARI.ServiceDefaults;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    IdentityModelEventSource.ShowPII = true;

    // Aspire service defaults (OpenTelemetry, health checks, service discovery)
    builder.AddServiceDefaults();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = false;

        options.ReportApiVersions = true;

        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";

        options.SubstituteApiVersionInUrl = true;
    });
    //builder.Services.AddSwaggerGen();
    builder.Services.AddCors();

    // Serilog
    builder.Host.UseSerilog((context, loggerConfiguration) =>
        loggerConfiguration.ReadFrom.Configuration(context.Configuration));

    // Aspire-managed PostgreSQL
    //builder.AddNpgsqlDbContext<AppDbContext>("cwm-db");

    builder.AddMySqlDbContext<AppDbContext>("cwm-db");

    // Aspire-managed Redis (for HybridCache L2)
    builder.AddRedisDistributedCache("cwm-cache");

    // Application & Infrastructure
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Global exception handling
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // ProblemDetails
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    // Global exception handler
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        //app.MapScalarApiReference(options =>
        //{
        //    options.WithTitle("CWM Clean Architecture API");
        //    options.WithTheme(ScalarTheme.BluePlanet);
        //    options.WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl);
        //});
    }

    var allowedOrigins = new List<string>();
    app.Configuration.GetSection(nameof(allowedOrigins)).Bind(allowedOrigins);
    app.UseCors(builder =>
    {
        builder.WithOrigins(allowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    app.ConfigureSwagger(app.Configuration);
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSerilogRequestLogging();

    // Map endpoints
    app.MapIdentityEndpoints();
    app.MapTodoEndpoints();
    app.MapUomEndpoints();
    app.MapItemCategoryEndpoints();
    app.MapWarehouseEndpoints();
    app.MapStorageLocationEndpoints();
    app.MapItemEndpoints();
    app.MapAdjustmentReasonEndpoints();
    app.MapItemBranchSettingEndpoints();
    app.MapStockReservationEndpoints();
    app.MapDocumentSequenceEndpoints();
    app.MapStockLedgerEndpoints();
    app.MapSerialNumberEndpoints();
    app.MapStockLocationBalanceEndpoints();
    app.MapGlAccountEndpoints();
    app.MapCostCenterEndpoints();
    app.MapGlJournalEndpoints();
    app.MapApprovalRequestEndpoints();
    app.MapNotificationEndpoints();
    app.MapGoodsReceiptEndpoints();
    app.MapGoodsIssueEndpoints();
    app.MapStockAdjustmentEndpoints();
    app.MapStockOpnameEndpoints();
    app.MapStockTransferRequestEndpoints();
    app.MapStockLocationTransferEndpoints();
    app.MapCustomerEndpoints();
    app.MapCompanyEndpoints();
    app.MapBranchEndpoints();
    app.MapFormEndpoints();
    app.MapUserEndpoints();
    app.MapRoleEndpoints();
    app.MapCurrencyEndpoints();
    app.MapTaxCodeEndpoints();
    app.MapFiscalYearEndpoints();
    app.MapExchangeRateEndpoints();
    app.MapBankAccountEndpoints();
    app.MapSupplierEndpoints();
    app.MapPurchaseOrderEndpoints();
    app.MapPurchaseRequestEndpoints();
    app.MapGoodsReceiptPoEndpoints();
    app.MapGoodsReturnEndpoints();
    app.MapApInvoiceEndpoints();
    app.MapPurchaseReturnReasonEndpoints();
    //app.MapGet("/debug-user", (HttpContext ctx) =>
    //{
    //    return new
    //    {
    //        ctx.User.Identity?.IsAuthenticated,
    //        Claims = ctx.User.Claims.Select(c => new { c.Type, c.Value })
    //    };
    //}).RequireAuthorization();
    //app.MapGet("/test-header", (HttpContext ctx) =>
    //{
    //    return ctx.Request.Headers.Authorization.ToString();
    //});

    // Aspire default endpoints (health, alive)
    app.MapDefaultEndpoints();

    // Seed database in development
    if (app.Environment.IsDevelopment())
    {
        await AppDbSeeder.SeedAsync(app.Services);
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
