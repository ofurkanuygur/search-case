using WriteService.Domain.Entities;
using WriteService.Domain.Enums;

namespace WriteService.Domain.Models;

/// <summary>
/// Result of change detection for a single content item
/// </summary>
public sealed class ChangeDetectionResult
{
    /// <summary>
    /// The content entity being evaluated
    /// </summary>
    public ContentEntity Content { get; }

    /// <summary>
    /// Type of change detected
    /// </summary>
    public ChangeType ChangeType { get; }

    /// <summary>
    /// Previous version of the content if it exists
    /// </summary>
    public ContentEntity? PreviousContent { get; }

    /// <summary>
    /// List of fields that changed
    /// </summary>
    public List<ChangedField> ChangedFields { get; }

    /// <summary>
    /// Whether this change should be persisted
    /// </summary>
    public bool ShouldPersist => ChangeType != ChangeType.None && ChangeType != ChangeType.Deleted;

    /// <summary>
    /// Confidence score for the change detection (0-1)
    /// </summary>
    public double ConfidenceScore { get; }

    public ChangeDetectionResult(
        ContentEntity content,
        ChangeType changeType,
        ContentEntity? previousContent = null,
        List<ChangedField>? changedFields = null,
        double confidenceScore = 1.0)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        ChangeType = changeType;
        PreviousContent = previousContent;
        ChangedFields = changedFields ?? new List<ChangedField>();
        ConfidenceScore = Math.Max(0, Math.Min(1, confidenceScore));
    }

    /// <summary>
    /// Creates a result for new content
    /// </summary>
    public static ChangeDetectionResult NewContent(ContentEntity content)
    {
        return new ChangeDetectionResult(content, ChangeType.Created);
    }

    /// <summary>
    /// Creates a result for unchanged content
    /// </summary>
    public static ChangeDetectionResult Unchanged(ContentEntity content, ContentEntity previousContent)
    {
        return new ChangeDetectionResult(content, ChangeType.None, previousContent);
    }

    /// <summary>
    /// Creates a result for updated content
    /// </summary>
    public static ChangeDetectionResult Updated(
        ContentEntity content,
        ContentEntity previousContent,
        List<ChangedField> changedFields)
    {
        return new ChangeDetectionResult(
            content,
            ChangeType.Updated,
            previousContent,
            changedFields);
    }

    /// <summary>
    /// Gets a summary of changes
    /// </summary>
    public string GetChangeSummary()
    {
        if (ChangeType == ChangeType.None)
            return $"No changes detected for {Content.Id}";

        if (ChangeType == ChangeType.Created)
            return $"New content created: {Content.Id}";

        if (ChangeType == ChangeType.Updated && ChangedFields.Any())
        {
            var fieldNames = string.Join(", ", ChangedFields.Select(f => f.FieldName));
            return $"Content {Content.Id} updated. Changed fields: {fieldNames}";
        }

        return $"Content {Content.Id} change type: {ChangeType}";
    }
}

/// <summary>
/// Represents a field that changed
/// </summary>
public sealed class ChangedField
{
    /// <summary>
    /// Name of the field that changed
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Previous value (serialized as string)
    /// </summary>
    public string? OldValue { get; }

    /// <summary>
    /// New value (serialized as string)
    /// </summary>
    public string? NewValue { get; }

    /// <summary>
    /// Whether this is a significant change
    /// </summary>
    public bool IsSignificant { get; }

    public ChangedField(
        string fieldName,
        string? oldValue,
        string? newValue,
        bool isSignificant = true)
    {
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        OldValue = oldValue;
        NewValue = newValue;
        IsSignificant = isSignificant;
    }
}