namespace MatchMaking.Service.Middlewares;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing the request.");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var response = new
            {
                StatusCode = 500,
                ErrorMessage = "An unexpected error occurred. Please try again later."
            };
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
