// Name: Ng Xuan Ya | Admin: 253125M | Tutorial: 04

using System.Net;
using System.Text.Json;

namespace ArcaneVault.API.Middleware
{
    /// <summary>
    /// Global error handling middleware that catches unhandled exceptions,
    /// logs them, and returns standardized error responses.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Processes incoming HTTP requests and handles exceptions.
        /// </summary>
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

        /// <summary>
        /// Handles exceptions by logging and returning standardized error response.
        /// </summary>
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log the exception
            var logger = context.RequestServices.GetRequiredService<ILogger<ErrorHandlingMiddleware>>();
            logger.LogError($"Unhandled exception: {exception.Message}\nStackTrace: {exception.StackTrace}");

            context.Response.ContentType = "application/json";

            var response = new ErrorResponse();

            switch (exception)
            {
                case ArgumentException argEx:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response.Message = argEx.Message;
                    response.StatusCode = 400;
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    response.Message = "Unauthorized access.";
                    response.StatusCode = 401;
                    break;

                case KeyNotFoundException:
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    response.Message = "Resource not found.";
                    response.StatusCode = 404;
                    break;

                default:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Message = "An unexpected error occurred. Please try again later.";
                    response.StatusCode = 500;
                    break;
            }

            return context.Response.WriteAsJsonAsync(response);
        }
    }

    /// <summary>
    /// Standard error response DTO.
    /// </summary>
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
