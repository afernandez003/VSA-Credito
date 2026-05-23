using Creditos.Api.Endpoints;
using Creditos.Api.Health;
using Scalar.AspNetCore;

namespace Creditos.Api.Pipeline;

public static class PipelineExtensions
{
    public static WebApplication UseStandardPipeline(this WebApplication app)
    {
        _ = app.UseExceptionHandler();

        if (!app.Environment.IsDevelopment())
        {
            _ = app.UseHsts();
        }

        _ = app.UseHttpsRedirection();
        _ = app.UseCorrelationId();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi(pattern: "api/document.json");
        }
        else
        {
            app.MapGet("api/document.json", (IWebHostEnvironment env) =>
            {
                var path = Path.Combine(env.ContentRootPath, "openapi", "openapi.json");
                return TypedResults.PhysicalFile(path, "application/json");
            });
        }

        app.MapScalarApiReference(options =>
        {
            options.OpenApiRoutePattern = "api/document.json";
            options.Title = "Creditos API";
            options.Theme = ScalarTheme.Default;
            options.Layout = ScalarLayout.Modern;
            options.DarkMode = true;
        });

        _ = app.MapHealthEndpoints();
        _ = app.MapCreditoEndpoints();

        return app;
    }
}
