using Microsoft.AspNetCore.Mvc;

namespace WorldCup_System.Controllers
{
    internal static class ApiErrorHelper
    {
        public static IActionResult FromException(Exception exception)
        {
            return exception switch
            {
                KeyNotFoundException => new NotFoundObjectResult(new { error = exception.Message }),
                ArgumentNullException => new BadRequestObjectResult(new { error = exception.Message }),
                ArgumentException => new BadRequestObjectResult(new { error = exception.Message }),
                InvalidOperationException => new BadRequestObjectResult(new { error = exception.Message }),
                _ => new ObjectResult(new { error = $"Internal server error: {exception.Message}" })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                }
            };
        }
    }
}
