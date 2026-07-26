using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WorldCup_System.Controllers
{
    internal static class ApiErrorHelper
    {
        public static IActionResult FromException(Exception exception)
        {
            return exception switch
            {
                KeyNotFoundException => new NotFoundObjectResult(new { error = exception.Message }),
                UnauthorizedAccessException => new ObjectResult(new { error = exception.Message })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                },
                ArgumentNullException => new BadRequestObjectResult(new { error = exception.Message }),
                ArgumentException => new BadRequestObjectResult(new { error = exception.Message }),
                InvalidOperationException => new BadRequestObjectResult(new { error = exception.Message }),
                DbUpdateException dbUpdateException => new BadRequestObjectResult(new
                {
                    error = GetDatabaseErrorMessage(dbUpdateException)
                }),
                _ => new ObjectResult(new { error = $"Internal server error: {exception.Message}" })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                }
            };
        }

        private static string GetDatabaseErrorMessage(DbUpdateException exception)
        {
            Exception? innerException = exception.InnerException;
            while (innerException != null)
            {
                if (!string.IsNullOrWhiteSpace(innerException.Message))
                {
                    return innerException.Message;
                }

                innerException = innerException.InnerException;
            }

            return exception.Message;
        }
    }
}
