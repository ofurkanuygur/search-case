using System.Xml.Serialization;

namespace XmlProviderMicroservice.Models;

public sealed class Meta
{
    [XmlElement("total_count")]
    public int TotalCount { get; set; }
    
    [XmlElement("current_page")]
    public int CurrentPage { get; set; }
    
    [XmlElement("items_per_page")]
    public int ItemsPerPage { get; set; }
}
