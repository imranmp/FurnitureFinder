using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FurnitureFinder.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed.");
            var problemDetails = new ValidationProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Validation error",
                Detail = "One or more validation errors occurred.",
                Instance = context.Request.Path
            };
            foreach (var error in ex.Errors)
            {
                problemDetails.Errors.TryAdd(error.PropertyName, [error.ErrorMessage]);
            }
            context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access.");
            var problemDetails = new ProblemDetails
            {
                Status = (int)HttpStatusCode.Unauthorized,
                Title = "Unauthorized",
                Detail = ex.Message,
                Instance = context.Request.Path
            };
            context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception.");
            var problemDetails = new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Internal server error",
                Detail = ex.Message,
                Instance = context.Request.Path
            };
            context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}