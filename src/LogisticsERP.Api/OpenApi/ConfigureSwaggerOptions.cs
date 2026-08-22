using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LogisticsERP.Api.OpenApi;

internal sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Al Bawaba Logistics ERP API",
            Version = "v1",
            Description = "API for البوابة للخدمات اللوجستية."
        });
        options.CustomSchemaIds(type => type.FullName?.Replace('+', '.') ?? type.Name);
    }
}
