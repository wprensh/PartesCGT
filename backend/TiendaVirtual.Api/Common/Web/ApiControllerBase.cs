using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using TiendaVirtual.Api.Common.Results;
using TiendaVirtual.Api.Common.Security;

namespace TiendaVirtual.Api.Common.Web;

/// <summary>
/// Base de los controladores. Traduce errores de negocio a respuestas ProblemDetails (RFC 9457).
/// Agregar un tipo de error nuevo solo toca este archivo, no los controladores.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id del usuario del panel que hace la petición (solo en acciones con [Authorize]/[HasPermission]).</summary>
    protected int CurrentUserId =>
        int.Parse(User.FindFirst(AuthClaims.Subject)?.Value
                  ?? throw new InvalidOperationException("La acción requiere un usuario autenticado."), CultureInfo.InvariantCulture);

    protected ActionResult Failure(Error error) =>
        error is { Type: ErrorType.NotFound, Message: null }
            ? NotFound()
            : Problem(detail: error.Message, statusCode: StatusCodeFor(error.Type));

    /// <summary>200 con el valor, o el error traducido.</summary>
    protected ActionResult<T> OkOrFailure<T>(Result<T> result)
    {
        if (result.IsSuccess) return result.Value!;
        return Failure(result.Error!);
    }

    /// <summary>204 sin contenido, o el error traducido.</summary>
    protected ActionResult NoContentOrFailure(Result result) =>
        result.IsSuccess ? NoContent() : Failure(result.Error!);

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };
}
