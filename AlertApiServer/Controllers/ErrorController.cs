namespace AlertApiServer.Controllers
{
    using Microsoft.AspNetCore.Diagnostics;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/error")]
    public class ErrorController : Controller
    {
        [HttpGet]
        public IActionResult HandleError()
        {
            // Pobierz szczegóły błędu z kontekstu
            var exceptionHandlerFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = exceptionHandlerFeature?.Error;

            // Możesz logować szczegóły błędu (opcjonalnie)
            Console.WriteLine($"Błąd: {exception?.Message}");

            // Zwróć generyczną odpowiedź błędu
            return Problem(
                detail: exception?.Message ?? "Wystąpił nieznany błąd",
                statusCode: 500
            );
        }
    }
}
