namespace SearchCase.Contracts.Models;

/// <summary>
/// Canonical representation of video content
/// </summary>
public sealed class CanonicalVideoContent : CanonicalContent
{
    public override ContentType Type => ContentType.Video;

    public VideoMetrics Metrics { get; set; } = new();
}
