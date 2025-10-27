using System.Xml.Serialization;

namespace XmlProviderMicroservice.Models;

[XmlRoot("stats")]
public sealed class ItemStats
{
    // Video stats
    [XmlElement("views")]
    public int? Views { get; set; }
    
    [XmlElement("likes")]
    public int? Likes { get; set; }
    
    [XmlElement("duration")]
    public string? Duration { get; set; }
    
    // Article stats
    [XmlElement("reading_time")]
    public int? ReadingTime { get; set; }
    
    [XmlElement("reactions")]
    public int? Reactions { get; set; }
    
    [XmlElement("comments")]
    public int? Comments { get; set; }
}
