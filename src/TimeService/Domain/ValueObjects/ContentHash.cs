using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TimeService.Domain.ValueObjects;

/// <summary>
/// Value Object representing a SHA256 hash of content data
/// Used for change detection by comparing hashes
/// Immutable by design
/// </summary>
public sealed record ContentHash
{
    /// <summary>
    /// The SHA256 hash value (64 character hex string)
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// Private constructor for controlled instantiation
    /// </summary>
    private ContentHash(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Factory method to create a ContentHash from a string value
    /// </summary>
    /// <param name="hashValue">The hash value (must be 64 character hex string)</param>
    /// <returns>A validated ContentHash instance</returns>
    /// <exception cref="ArgumentException">Thrown when hash format is invalid</exception>
    public static ContentHash FromValue(string hashValue)
    {
        if (string.IsNullOrWhiteSpace(hashValue))
        {
            throw new ArgumentException("Hash value cannot be null or empty", nameof(hashValue));
        }

        if (hashValue.Length != 64)
        {
            throw new ArgumentException("SHA256 hash must be 64 characters", nameof(hashValue));
        }

        // Validate hex format
        if (!System.Text.RegularExpressions.Regex.IsMatch(hashValue, "^[a-fA-F0-9]{64}$"))
        {
            throw new ArgumentException("Hash must be a valid hex string", nameof(hashValue));
        }

        return new ContentHash(hashValue.ToLowerInvariant());
    }

    /// <summary>
    /// Computes SHA256 hash from an object by serializing it to JSON
    /// </summary>
    /// <param name="data">The object to hash</param>
    /// <returns>A ContentHash instance</returns>
    public static ContentHash ComputeFrom<T>(T data) where T : notnull
    {
        // Serialize to JSON (deterministic)
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Compute SHA256 hash
        var bytes = Encoding.UTF8.GetBytes(json);
        var hashBytes = SHA256.HashData(bytes);

        // Convert to hex string
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return new ContentHash(hashString);
    }

    /// <summary>
    /// Implicit conversion to string for convenience
    /// </summary>
    public static implicit operator string(ContentHash hash) => hash.Value;

    /// <summary>
    /// String representation
    /// </summary>
    public override string ToString() => Value;
}
