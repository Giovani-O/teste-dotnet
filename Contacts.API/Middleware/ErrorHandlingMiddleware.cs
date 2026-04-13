using System.Text.Json;
using Contacts.API.Exceptions;

namespace Contacts.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
        int statusCode;
        string message;
        List<string> errors;

        switch (exception)
        {
            case ValidationException validationEx:
                statusCode = StatusCodes.Status400BadRequest;
                message = validationEx.Message;
                errors = validationEx.Errors;
                break;

            case NotFoundException notFoundEx:
                statusCode = StatusCodes.Status404NotFound;
                message = notFoundEx.Message;
                errors = [notFoundEx.Message];
                break;

            case ConflictException conflictEx:
                statusCode = StatusCodes.Status409Conflict;
                message = conflictEx.Message;
                errors = [conflictEx.Message];
                break;

            default:
                _logger.LogError(exception, "Unhandled exception");
                statusCode = StatusCodes.Status500InternalServerError;
                message = "An unexpected error occurred.";
                errors = ["An unexpected error occurred."];
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var body = JsonSerializer.Serialize(
            new { statusCode, message, errors },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        await context.Response.WriteAsync(body);
    }
}
