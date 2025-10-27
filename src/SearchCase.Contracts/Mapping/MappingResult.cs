namespace SearchCase.Contracts.Mapping;

/// <summary>
/// Result pattern for mapping operations
/// Supports partial success scenarios
/// </summary>
/// <typeparam name="T">The type of the mapped data</typeparam>
public sealed class MappingResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public List<string> Errors { get; init; } = new();

    public static MappingResult<T> Ok(T data) => new()
    {
        Success = true,
        Data = data,
        Errors = new List<string>()
    };

    public static MappingResult<T> Fail(params string[] errors) => new()
    {
        Success = false,
        Data = default,
        Errors = errors.ToList()
    };

    public static MappingResult<T> Fail(List<string> errors) => new()
    {
        Success = false,
        Data = default,
        Errors = errors
    };
}
