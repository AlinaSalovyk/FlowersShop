using System.Net;
using System.Text.Json;
using FluentValidation;

namespace Api.Modules;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
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

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        HttpStatusCode code = HttpStatusCode.InternalServerError;
        object? errors = null;

        if (exception is ValidationException validationException)
        {
            code = HttpStatusCode.BadRequest;
            errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        if (errors != null)
        {
            var result = JsonSerializer.Serialize(new { errors });
            return context.Response.WriteAsync(result);
        }
        
        return Task.CompletedTask;
    }
}