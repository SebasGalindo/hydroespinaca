namespace SensorService.Api.Configurations;
public static class SwaggerAppBuilderExtensions
{
    public static IApplicationBuilder UseSwaggerDocs(this IApplicationBuilder app, string serviceName)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{serviceName} API");
            c.RoutePrefix = "docs";
        });
        return app;
    }
}
