using System.Text.Json;
using OrderFlow.Application.Common.Exceptions;

namespace OrderFlow.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve =>
                (StatusCodes.Status400BadRequest, ve.Message, (object?)ve.Errors),
            UnauthorizedException ue =>
                (StatusCodes.Status401Unauthorized, ue.Message, null),
            NotFoundException nfe =>
                (StatusCodes.Status404NotFound, nfe.Message, null),
            ConflictException ce =>
                (StatusCodes.Status409Conflict, ce.Message, null),
            _ =>
                (StatusCodes.Status500InternalServerError,
                 "Ocurrió un error interno en el servidor.", null)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Excepción no controlada: {Message}", exception.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        // Formato ProblemDetails (RFC 7807), estándar de APIs REST modernas
        var problem = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.com/{statusCode}",
            ["title"] = title,
            ["status"] = statusCode,
            ["traceId"] = context.TraceIdentifier
        };

        if (errors is not null)
            problem["errors"] = errors;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}