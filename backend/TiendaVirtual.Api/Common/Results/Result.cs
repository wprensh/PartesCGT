namespace TiendaVirtual.Api.Common.Results;

/// <summary>
/// Resultado de una operación que puede fallar por una regla de negocio.
/// Reemplaza las tuplas (valor, error) y los Problem(...) repartidos por los controladores.
/// </summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static implicit operator Result(Error error) => new(error);
}

/// <summary>Resultado con valor. Se crea implícitamente desde un <typeparamref name="T"/> o desde un <see cref="Results.Error"/>.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(null) => _value = value;
    private Result(Error error) : base(error) { }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede leer el valor de un resultado fallido.");

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);
}
