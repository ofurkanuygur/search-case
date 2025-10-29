namespace TimeService.Domain.ValueObjects;

/// <summary>
/// Value Object representing a content score
/// Enforces business rules: score must be non-negative
/// Immutable by design
/// </summary>
public sealed record Score
{
    /// <summary>
    /// The numeric score value
    /// </summary>
    public decimal Value { get; init; }

    /// <summary>
    /// Private constructor for controlled instantiation
    /// </summary>
    private Score(decimal value)
    {
        Value = value;
    }

    /// <summary>
    /// Factory method to create a Score with validation
    /// </summary>
    /// <param name="value">The score value (must be >= 0)</param>
    /// <returns>A validated Score instance</returns>
    /// <exception cref="ArgumentException">Thrown when value is negative</exception>
    public static Score Create(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentException("Score cannot be negative", nameof(value));
        }

        return new Score(value);
    }

    /// <summary>
    /// Creates a zero score (default)
    /// </summary>
    public static Score Zero => new(0m);

    /// <summary>
    /// Implicit conversion to decimal for convenience
    /// </summary>
    public static implicit operator decimal(Score score) => score.Value;

    /// <summary>
    /// String representation rounded to 2 decimal places
    /// </summary>
    public override string ToString() => Value.ToString("F2");
}
