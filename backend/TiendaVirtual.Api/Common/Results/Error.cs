namespace TiendaVirtual.Api.Common.Results;

/// <summary>Categoría del error. Cada una se traduce a un código HTTP en un único lugar (ApiControllerBase).</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Unavailable
}

/// <summary>
/// Error esperado de negocio (no una excepción): "la categoría no existe", "no hay stock"…
/// El mensaje va directo al cliente, así que debe ser claro y en español.
/// </summary>
public sealed record Error(ErrorType Type, string? Message = null)
{
    public static Error Validation(string message) => new(ErrorType.Validation, message);
    public static Error NotFound(string? message = null) => new(ErrorType.NotFound, message);
    public static Error Conflict(string message) => new(ErrorType.Conflict, message);
    public static Error Unauthorized(string message) => new(ErrorType.Unauthorized, message);
    public static Error Forbidden(string message) => new(ErrorType.Forbidden, message);
    public static Error Unavailable(string message) => new(ErrorType.Unavailable, message);
}
