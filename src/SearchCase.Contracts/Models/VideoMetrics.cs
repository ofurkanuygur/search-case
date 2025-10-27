namespace SearchCase.Contracts.Models;

public sealed class VideoMetrics
{
    public int Views { get; set; }
    public int Likes { get; set; }

    /// <summary>
    /// Duration in ISO 8601 format (e.g., PT22M45S for 22 minutes 45 seconds)
    /// </summary>
    public TimeSpan Duration { get; set; }
}
