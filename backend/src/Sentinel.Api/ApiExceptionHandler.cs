using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Sentinel.Api;

// The services signal expected failures with exceptions (bad input, wrong state,
// duplicate row). Without this they all surface to the dashboard as bare 500s.
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ArgumentException ex => (StatusCodes.Status400BadRequest, "Invalid request", ex.Message),
            UnauthorizedAccessException ex => (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
            PostgresException { SqlState: "23505" } => (StatusCodes.Status409Conflict, "Already exists",
                "A record with these details already exists."),
            InvalidOperationException ex => (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            _ => (0, "", "")
        };

        if (status == 0)
            return false; // unexpected: let the default 500 problem-details path handle it

        logger.LogWarning(exception, "Request failed with {Status}: {Title}", status, title);

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title, Detail = detail },
            cancellationToken);
        return true;
    }
}
