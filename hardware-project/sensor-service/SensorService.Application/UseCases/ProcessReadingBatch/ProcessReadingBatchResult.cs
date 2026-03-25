namespace SensorService.Application.UseCases.ProcessReadingBatch;

/// <summary>
/// Clase genérica que encapsula el resultado de una operación que puede ser exitosa o fallida.
/// Implementa el patrón Result para manejo explícito de errores sin excepciones.
/// </summary>
/// <typeparam name="T">Tipo del valor contenido en caso de éxito.</typeparam>
public class Result<T>
{
    /// <summary>Indica si la operación fue exitosa.</summary>
    public bool IsSuccess { get; private set; }
    /// <summary>Mensaje de error en caso de fallo.</summary>
    public string? Error { get; private set; }
    /// <summary>Valor resultante en caso de éxito.</summary>
    public T? Value { get; private set; }

    /// <summary>Indica si la operación falló.</summary>
    public bool IsFailure => !IsSuccess;

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Crea un resultado exitoso con el valor proporcionado.
    /// </summary>
    /// <param name="value">Valor del resultado.</param>
    /// <returns>Resultado exitoso.</returns>
    public static Result<T> Success(T value) => new(true, value, null);

    /// <summary>
    /// Crea un resultado fallido con el mensaje de error proporcionado.
    /// </summary>
    /// <param name="error">Mensaje de error descriptivo.</param>
    /// <returns>Resultado fallido.</returns>
    public static Result<T> Failure(string error) => new(false, default, error);
}
