using Microsoft.AspNetCore.Builder;
//using SIDCMMS.Application.DTOs.Swagger;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;
using ZARI.Application.DTOs.Swagger;

namespace ZARI.Api.Extensions;

    public static class ApplicationBuilderExtensions
    {
        public static void ConfigureSwagger(this IApplicationBuilder app, IConfiguration configuration)
        {
            app.UseSwagger();
            var swaggerOptions = new List<SwaggerOptions>();
            configuration.GetSection(nameof(SwaggerOptions)).Bind(swaggerOptions);

            app.UseSwaggerUI(options =>
            {
                options.EnableFilter();
                options.DocExpansion(DocExpansion.List);
                options.IndexStream = () => Assembly.GetExecutingAssembly().GetManifestResourceStream("ZARI.Api.Resources.swagger_index.html");
                options.DocumentTitle = "SIDC Item Verifier Core API";
                foreach (var swaggerOpt in swaggerOptions)                {
                    
                    options.SwaggerEndpoint($"/swagger/{swaggerOpt.Document.Name}/swagger.json", swaggerOpt.Document.Title);
                }                
                options.RoutePrefix = "swagger";
                options.DisplayRequestDuration();
            });
        }
    }

