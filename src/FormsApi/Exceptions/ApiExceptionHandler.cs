using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FormsApi.Exceptions;

// Global exception handler. 
// Unhandled exceptions are logged on server side before 
// returning the response.
public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    // Handle exceptions and map to appropriate HTTP status codes
    // and ProblemDetails responses
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            FormNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            FormConflictException => (StatusCodes.Status409Conflict, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;

        /* Example output for a 404 Not Found response:
            {
                "status": 404,
                "title": "Form '3fa85f64-5717-4562-b3fc-2c963f66afa6' not found.",
                "instance": "/api/forms/3fa85f64-5717-4562-b3fc-2c963f66afa6"
            }
        */
    }
}
