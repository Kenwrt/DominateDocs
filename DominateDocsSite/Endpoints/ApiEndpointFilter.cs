using Microsoft.Extensions.Options;

namespace DominateDocsSite.Endpoints;

public class ApiEndpointFilter : IEndpointFilter
{
    private readonly IConfiguration config;

    private const string ApiKeyHeaderName = "X-Api-Key";
    private string ValidApiKey = "XXXXXX"; // Replace with config or secret store in production

    private readonly IOptionsMonitor<ApiConfigOptions> options;

    public ApiEndpointFilter(IConfiguration config, IOptionsMonitor<ApiConfigOptions> options)
    {
        this.config = config;
        this.options = options;

        ValidApiKey = options.CurrentValue.ApiKey;
    }

    public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        if (!httpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            return Results.Unauthorized();
        }

        if (!string.Equals(extractedApiKey, ValidApiKey, StringComparison.Ordinal))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}