using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using QuestPDF.Infrastructure;
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

    // Report PDF export (Trial Balance, BIR books, etc.) — Community license: free under
    // $1M USD annual gross revenue per company, confirmed acceptable for this deployment.
    QuestPDF.Settings.License = LicenseType.Community;

    // BUG FIX (found during production-readiness UAT, 2026-10-03): this was unconditionally true,
    // including in Production. ShowPII makes IdentityModel include actual token/claim values (not
    // just type names) in exception messages and logs -- a real sensitive-data-leak risk once this
    // API is handling live JWTs. Restrict it to local debugging only.
    IdentityModelEventSource.ShowPII = builder.Environment.IsDevelopment();

    // BUG FIX (found during production-readiness UAT, 2026-10-03): appsettings.json's "Jwt:Secret"
    // is a hardcoded placeholder checked into source control. If a Production deployment is ever
    // started without overriding it via environment variable / secret manager, every JWT this API
    // issues is signed with a secret anyone with repo read access already has -- meaning they can
    // forge a valid token for ANY user, including Admin. Fail fast instead of starting up insecure.
    if (builder.Environment.IsProduction())
    {
        var jwtSecret = builder.Configuration["Jwt:Secret"];
        const string checkedInPlaceholder = "151W5Y5H3R35TY0RB5KT0SVP0RTUO1969";
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret == checkedInPlaceholder || jwtSecret.Length < 32)
        {
            throw new InvalidOperationException(
                "Refusing to start in Production with a missing, default, or weak Jwt:Secret. " +
                "Set a real secret (32+ random characters) via an environment variable or secret " +
                "manager -- never via the checked-in appsettings.json value.");
        }

        // Same reasoning as the Jwt:Secret guard above: appsettings.json's "cwm-db" connection
        // string ships with a trivial "User=root;Password=root" credential checked into source
        // control. Refuse to start in Production against it.
        var dbConnectionString = builder.Configuration.GetConnectionString("cwm-db") ?? string.Empty;
        if (dbConnectionString.Contains("Password=root", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Refusing to start in Production with the default 'root/root' database credential " +
                "checked into appsettings.json. Set a real connection string via an environment " +
                "variable or secret manager, and rotate the database credential.");
        }
    }

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

    // SECURITY FIX (found during production-readiness UAT, 2026-10-03): /api/identity/login had no
    // rate limiting or account lockout at all -- LoginCommandHandler calls UserManager.CheckPasswordAsync
    // directly, which bypasses ASP.NET Identity's lockout machinery entirely, leaving login fully open
    // to unthrottled brute-force/credential-stuffing. A per-IP fixed window is a minimal, low-risk fix;
    // real lockout (SignInManager + options.Lockout) is a bigger change better done as its own follow-up.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        // AddFixedWindowLimiter(name, ...) would create ONE shared counter for every caller --
        // partitioning by remote IP here instead, so one abusive client can't exhaust the budget
        // for everyone else trying to log in from a different address.
        options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
    });

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

    app.ConfigureSwagger(app.Configuration);
    app.UseHttpsRedirection();
    // SECURITY FIX (found during production-readiness UAT, 2026-10-03): UseHsts was never called --
    // without it, a client's first-ever plaintext connection (or any on-path MITM) is never told to
    // force HTTPS on subsequent visits. Guarded to non-Development since the dev cert isn't meant to
    // be trusted long-term by a browser.
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }
    // Serves uploaded promo-slide images back at /uploads/... — the only file-upload feature in
    // ZARI today (PosPromoSlideEndpoints.UploadImage saves under wwwroot/uploads/pos-promo-slides/).
    app.UseStaticFiles();
    app.UseRouting();

    // SECURITY FIX (found during production-readiness UAT, 2026-10-03): UseCors used to run before
    // UseRouting. It happened to still work (this is global middleware-based CORS, not per-endpoint
    // [EnableCors]), but Microsoft's documented required order is UseRouting -> UseCors ->
    // UseAuthentication -> UseAuthorization -- moved here to match and avoid relying on an
    // implementation detail.
    var allowedOrigins = new List<string>();
    app.Configuration.GetSection(nameof(allowedOrigins)).Bind(allowedOrigins);
    app.UseCors(builder =>
    {
        builder.WithOrigins(allowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSerilogRequestLogging();

    // Map endpoints
    app.MapIdentityEndpoints();
    app.MapUomEndpoints();
    app.MapItemCategoryEndpoints();
    app.MapWarehouseEndpoints();
    app.MapStorageLocationEndpoints();
    app.MapItemEndpoints();
    app.MapAdjustmentReasonEndpoints();
    app.MapItemBranchSettingEndpoints();
    app.MapItemBarcodeEndpoints();
    app.MapStockReservationEndpoints();
    app.MapDocumentSequenceEndpoints();
    app.MapStockLedgerEndpoints();
    app.MapSerialNumberEndpoints();
    app.MapStockLocationBalanceEndpoints();
    app.MapGlAccountEndpoints();
    app.MapCostCenterEndpoints();
    app.MapGlJournalEndpoints();
    app.MapManualJournalEntryEndpoints();
    app.MapApprovalRequestEndpoints();
    app.MapNotificationEndpoints();
    app.MapGoodsReceiptEndpoints();
    app.MapGoodsIssueEndpoints();
    app.MapStockAdjustmentEndpoints();
    app.MapStockOpnameEndpoints();
    app.MapStockTransferRequestEndpoints();
    app.MapStockLocationTransferEndpoints();
    app.MapCustomerEndpoints();
    app.MapCustomerCreditRecordEndpoints();
    app.MapDepartmentEndpoints();
    app.MapPositionEndpoints();
    app.MapEmployeeEndpoints();
    app.MapWorkScheduleEndpoints();
    app.MapEmployeeScheduleAssignmentEndpoints();
    app.MapDailyTimeRecordEndpoints();
    app.MapLeaveTypeEndpoints();
    app.MapLeaveBalanceEntryEndpoints();
    app.MapLeaveRequestEndpoints();
    app.MapOvertimeRateRuleEndpoints();
    app.MapOvertimeRequestEndpoints();
    app.MapEmployeeAllowanceEndpoints();
    app.MapEmployeeDeductionEndpoints();
    app.MapSssContributionBracketEndpoints();
    app.MapPagIbigContributionBracketEndpoints();
    app.MapGovernmentContributionRateEndpoints();
    app.MapWithholdingTaxBracketEndpoints();
    app.MapPayrollRunEndpoints();
    app.MapThirteenthMonthPayRunEndpoints();
    app.MapCompanyEndpoints();
    app.MapDeviceSettingsEndpoints();
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
    app.MapOutgoingPaymentEndpoints();
    app.MapDiscountRuleEndpoints();
    app.MapStatutoryDiscountTypeEndpoints();
    app.MapSalesOrderEndpoints();
    app.MapDeliveryOrderEndpoints();
    app.MapSalesInvoiceEndpoints();
    app.MapSalesReturnEndpoints();
    app.MapCustomerPaymentEndpoints();
    app.MapPosClosingEndpoints();
    app.MapPaymentMethodEndpoints();
    app.MapPosTerminalEndpoints();
    app.MapVehicleEndpoints();
    app.MapDeliveryRouteEndpoints();
    app.MapTripEndpoints();
    app.MapRouteSalesReportEndpoints();
    app.MapPosPromoSlideEndpoints();
    app.MapPosSaleEndpoints();
    app.MapAccountingReportsEndpoints();
    app.MapPurchasingReportsEndpoints();
    app.MapSalesReportsEndpoints();
    app.MapInventoryReportsEndpoints();
    app.MapReportingEndpoints();
    app.MapLoanProductEndpoints();
    app.MapLoanApplicationEndpoints();
    app.MapLoanAccountEndpoints();
    app.MapLoanDisbursementEndpoints();
    app.MapLoanPaymentEndpoints();
    app.MapLoanReportsEndpoints();
    app.MapLoanRestructuringEndpoints();
    app.MapLoanWriteOffEndpoints();
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
